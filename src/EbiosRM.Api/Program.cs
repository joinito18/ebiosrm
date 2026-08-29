using EbiosRM.Api.Infrastructure.Hebergement;
using EbiosRM.Api.Infrastructure.Persistence;
using EbiosRM.Api.Modules.Audit.Domain;
using EbiosRM.Api.Modules.Audit.Infrastructure;
using EbiosRM.Api.Modules.Bibliotheque.Domain;
using EbiosRM.Api.Modules.Bibliotheque.Infrastructure;
using EbiosRM.Api.Modules.Conformite;
using EbiosRM.Api.Modules.Suivi;
using EbiosRM.Api.Modules.Suivi.Domain;
using EbiosRM.Api.Modules.Suivi.Infrastructure;
using EbiosRM.Api.Modules.Collaboration.Domain;
using EbiosRM.Api.Modules.Collaboration.Infrastructure;
using EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;
using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;
using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;
using EbiosRM.Api.Modules.CoreEngine.Infrastructure;
using EbiosRM.Api.Modules.Identity.Domain;
using EbiosRM.Api.Modules.Identity.Infrastructure;
using EbiosRM.Api.Modules.Reporting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

QuestPDF.Settings.License = LicenseType.Community;

// Sans ça, le handler JWT renomme silencieusement le claim "sub" vers l'URI XML
// historique (ClaimTypes.NameIdentifier) -- surprise classique .NET, garde les
// noms de claims tels qu'émis par ServiceAuthentification.GenererJeton.
System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

// Polices des rapports PDF : embarquees dans l'assembly (EmbeddedResource) donc
// disponibles quel que soit le mode de publication -- y compris l'executable
// fichier unique de l'application de bureau, qui ne recopie pas les fichiers
// loose a cote de l'exe.
var assemblyCourant = typeof(Program).Assembly;
foreach (var nomRessource in assemblyCourant.GetManifestResourceNames())
{
    if (!nomRessource.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase))
        continue;
    using var fluxRessource = assemblyCourant.GetManifestResourceStream(nomRessource);
    if (fluxRessource is null)
        continue;
    var memoirePolice = new MemoryStream();
    fluxRessource.CopyTo(memoirePolice);
    memoirePolice.Position = 0;
    QuestPDF.Drawing.FontManager.RegisterFont(memoirePolice);
}

var builder = WebApplication.CreateBuilder(args);

// Mode d'execution : serveur PostgreSQL (deploiement heberge / docker) ou
// bureau SQLite (le .exe double-clique). Cf. ConfigurationExecution.
var execution = ConfigurationExecution.Determiner(builder.Configuration);
builder.Services.AddSingleton(execution);

// Secret JWT resolu (auto-genere et persiste en mode bureau) reinjecte dans la
// configuration : ServiceAuthentification et le handler JwtBearer le lisent
// tous deux via Configuration["Jwt:Secret"].
builder.Configuration["Jwt:Secret"] = execution.ResoudreSecretJwt(builder.Configuration);

if (execution.ModeBureau)
{
    // Port fixe et connu pour l'ouverture du navigateur.
    builder.WebHost.UseUrls("http://localhost:5000");
}

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddDbContext<EbiosDbContext>(options =>
{
    if (execution.Fournisseur == FournisseurBaseDeDonnees.Sqlite)
        options.UseSqlite(execution.ChaineConnexion);
    else
        options.UseNpgsql(execution.ChaineConnexion);
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// Render est un reverse proxy -- sans ceci, Connection.RemoteIpAddress vaut
// toujours l'IP du proxy (la meme pour tout le monde), ce qui ferait
// partager un seul quota de rate limiting a tous les utilisateurs au lieu
// d'un quota par IP reelle. KnownNetworks/KnownProxies vides = on fait
// confiance a l'en-tete X-Forwarded-For tel quel (l'IP du proxy Render
// n'est pas fixe/connaissable a l'avance, pratique courante sur ce type
// de plateforme).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Par defaut ForwardLimit vaut 1 : un seul maillon de la chaine
    // X-Forwarded-For est traite, ce qui peut resoudre un hop de proxy
    // interne a Render (potentiellement instable d'une requete a l'autre)
    // plutot que l'IP publique reelle du client si plusieurs proxys
    // s'enchainent avant d'atteindre le conteneur -- constate en prod, ou
    // le rate limiter ne se declenchait jamais (contrairement au test local
    // sans proxy). Illimite = on remonte toute la chaine jusqu'au client.
    options.ForwardLimit = null;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Anti-abus sur l'inscription : n'importe qui pouvait creer des comptes en
// masse sans aucune verification (pas de captcha, pas d'email a confirmer).
// 5 inscriptions/heure par IP est large pour un usage legitime (personne ne
// cree 5 comptes en une heure depuis la meme IP) mais bloque le bourrage.
// En environnement de test, les WebApplicationFactory partagent une seule IP
// (null -> "inconnu") : des dizaines d'inscriptions de test se retrouveraient
// dans un unique quota. On desserre alors la limite.
var quotaInscription = builder.Environment.IsEnvironment("Testing") ? 100_000 : 5;
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("inscription", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "inconnu",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = quotaInscription,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0,
            }));
});

// Authentification par jeton JWT (pas de cookies de session) : le frontend
// (Vercel) et l'API (Render) sont sur deux origines différentes, un cookie
// cross-origin exigerait SameSite=None/Secure + credentials CORS, plus
// fragile qu'un en-tête Authorization porté explicitement par le client.
// Mur d'entrée uniquement (FallbackPolicy) : un seul niveau d'accès, pas de
// rôles/permissions différenciées (décision actée).
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Lecture différée (pas au moment de la construction du builder) :
        // AddJwtBearer(configureOptions) n'exécute ce lambda qu'à la
        // résolution de JwtBearerOptions (première requête entrante), bien
        // après que WebApplicationFactory (tests d'intégration) ait fusionné
        // sa configuration in-memory dans builder.Configuration -- même
        // raisonnement que builder.Configuration.GetConnectionString("EbiosDb")
        // lu paresseusement dans AddDbContext.
        var jwtSecret = builder.Configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret doit être configuré (appsettings ou variable d'environnement Jwt__Secret).");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser().Build();
});

builder.Services.AddScoped<IUtilisateurRepository, UtilisateurRepository>();
builder.Services.AddScoped<IEntreeJournalRepository, EntreeJournalRepository>();
builder.Services.AddScoped<IEtudeMembreRepository, EtudeMembreRepository>();
builder.Services.AddScoped<IBibliothequeRepository, BibliothequeRepository>();
builder.Services.AddScoped<EbiosRM.Api.Modules.Conformite.ServiceConformite>();
builder.Services.AddScoped<RapportConformitePdfGenerator>();
builder.Services.AddScoped<IIndicateurSuiviRepository, IndicateurSuiviRepository>();
builder.Services.AddScoped<ServiceMetriquesEtude>();
builder.Services.AddScoped<ServicePortefeuille>();
builder.Services.AddScoped<ServiceIndicateursAuto>();
builder.Services.AddScoped<ServiceEvolutionEtude>();
builder.Services.AddScoped<EbiosRM.Api.Modules.Reporting.Exports.RegistreRisquesExcelGenerator>();
builder.Services.AddScoped<EbiosRM.Api.Modules.Reporting.Exports.PortefeuilleExcelGenerator>();
builder.Services.AddScoped<EbiosRM.Api.Modules.Reporting.Exports.SyntheseWordGenerator>();
builder.Services.AddScoped<ServiceAuthentification>();
builder.Services.AddScoped<IEtudeRepository, EtudeRepository>();
builder.Services.AddScoped<ServiceSuppressionEtude>();
builder.Services.AddScoped<ServiceDuplicationEtude>();
builder.Services.AddScoped<ServiceImportEtude>();
builder.Services.AddScoped<IValeurMetierRepository, ValeurMetierRepository>();
builder.Services.AddScoped<IBienSupportRepository, BienSupportRepository>();
builder.Services.AddScoped<IEvenementRedouteRepository, EvenementRedouteRepository>();
builder.Services.AddScoped<ISocleSecuriteRepository, SocleSecuriteRepository>();
builder.Services.AddScoped<ISnapshotAtelierRepository, SnapshotAtelierRepository>();
builder.Services.AddScoped<ICoupleSourceRisqueObjectifViseRepository, CoupleSourceRisqueObjectifViseRepository>();
builder.Services.AddScoped<IPartiePrenanteRepository, PartiePrenanteRepository>();
builder.Services.AddScoped<IScenarioStrategiqueRepository, ScenarioStrategiqueRepository>();
builder.Services.AddScoped<ICheminAttaqueRepository, CheminAttaqueRepository>();
builder.Services.AddScoped<IScenarioOperationnelRepository, ScenarioOperationnelRepository>();
builder.Services.AddScoped<IScenarioDeRisqueRepository, ScenarioDeRisqueRepository>();
builder.Services.AddScoped<IPlanTraitementRisqueRepository, PlanTraitementRisqueRepository>();
builder.Services.AddScoped<ServiceValidationCompletudeAtelier1>();
builder.Services.AddScoped<ServiceCreationSnapshotAtelier1>();
builder.Services.AddScoped<ServiceCreationSnapshotAtelier2>();
builder.Services.AddScoped<ServiceCreationSnapshotAtelier3>();
builder.Services.AddScoped<ServiceCreationSnapshotAtelier4>();
builder.Services.AddScoped<ServiceCreationSnapshotAtelier5>();
builder.Services.AddScoped<ServiceValidationCompletudeAtelier2>();
builder.Services.AddScoped<ServiceValidationCompletudeAtelier3>();
builder.Services.AddScoped<ServiceValidationCompletudeAtelier4>();
builder.Services.AddScoped<ServiceValidationCompletudeAtelier5>();
builder.Services.AddScoped<ServiceAssemblageScenariosDeRisque>();
builder.Services.AddScoped<RapportAtelier1Service>();
builder.Services.AddScoped<RapportAtelier1PdfGenerator>();
builder.Services.AddScoped<RapportAtelier2Service>();
builder.Services.AddScoped<RapportAtelier2PdfGenerator>();
builder.Services.AddScoped<RapportAtelier3Service>();
builder.Services.AddScoped<RapportAtelier3PdfGenerator>();
builder.Services.AddScoped<RapportAtelier4Service>();
builder.Services.AddScoped<RapportAtelier4PdfGenerator>();
builder.Services.AddScoped<RapportAtelier5Service>();
builder.Services.AddScoped<RapportAtelier5PdfGenerator>();
builder.Services.AddScoped<RapportSyntheseGlobaleService>();
builder.Services.AddScoped<RapportSyntheseGlobalePdfGenerator>();
builder.Services.AddScoped<RapportCadreDeSuiviService>();
builder.Services.AddScoped<RapportCadreDeSuiviPdfGenerator>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Mise en place du schema de la base au demarrage :
//  - SQLite (mode bureau, le .exe) : cree la base directement depuis le modele
//    au premier lancement (les migrations EF de ce projet sont PostgreSQL).
//  - PostgreSQL : migrations EF, uniquement si ApplyMigrationsOnStartup=true
//    (docker compose selfhost). En SaaS c'est le pipeline ci.yml qui migre.
{
    // Mode bureau, 1er lancement : deposer l'etude d'exemple embarquee (si presente).
    execution.DeposerBaseExempleSiPremierLancement(app.Configuration);

    using var scopeMigration = app.Services.CreateScope();
    var dbDemarrage = scopeMigration.ServiceProvider.GetRequiredService<EbiosDbContext>();
    if (execution.Fournisseur == FournisseurBaseDeDonnees.Sqlite)
        await dbDemarrage.Database.EnsureCreatedAsync();
    else if (app.Configuration.GetValue<bool>("ApplyMigrationsOnStartup"))
        await dbDemarrage.Database.MigrateAsync();
}

// En tout premier : sans ca, tout ce qui lit Connection.RemoteIpAddress
// plus loin dans le pipeline (le rate limiter, ici) voit l'IP du proxy
// Render au lieu de celle du client reel.
app.UseForwardedHeaders();

// Gestion d'erreurs centralisée : toute exception non prévue (ex. violation
// de contrainte SQL, timeout...) renvoie un ProblemDetails générique au lieu
// de laisser fuiter une erreur 500 non standardisée. Les erreurs métier
// prévues (ArgumentException, InvalidOperationException...) continuent
// d'être traitées explicitement dans chaque endpoint, qui reste prioritaire.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseRateLimiter();

// Frontend embarque (build .exe / image Docker "web") : sert les fichiers
// statiques et fait tomber toute route non-API sur index.html (routage React).
// Ignore si wwwroot/index.html est absent -> API hebergee et conteneur "api"
// (dont le frontend est servi par nginx) inchanges.
//
// On resout wwwroot depuis AppContext.BaseDirectory (dossier de l'exe), PAS
// depuis app.Environment.WebRootPath : lance depuis le menu des applications,
// le repertoire courant est le HOME de l'utilisateur, WebRootPath pointerait
// alors sur ~/wwwroot (inexistant) et toute l'appli renverrait 401.
var dossierWwwroot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
var indexHtml = Path.Combine(dossierWwwroot, "index.html");
var frontendEmbarque = File.Exists(indexHtml);
if (frontendEmbarque)
{
    var fichiersWwwroot = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(dossierWwwroot);
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fichiersWwwroot });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = fichiersWwwroot });
}

app.UseAuthentication();
app.UseAuthorization();

// Controle d'acces aux etudes, centralise : toute route /api/v1/etudes/{id|etudeId}
// passe ici plutot que de repeter le controle dans chacun des ~80 handlers.
//  - Etude de demonstration (ProprietaireId null) : visible en lecture par tous,
//    non modifiable par personne.
//  - Sinon : il faut etre membre (table etude_membres). Le role requis pour
//    ecrire depend de l'action :
//      * gestion des membres (.../membres) ou suppression de l'etude : Proprietaire
//      * tout le reste (contenu des ateliers, valider/rouvrir, acceptation) : Editeur+
//    Un Lecteur ne peut qu'afficher et telecharger les rapports.
app.Use(async (context, next) =>
{
    var routeValue = context.GetRouteValue("etudeId") ?? context.GetRouteValue("id");
    if (routeValue is string idBrut && Guid.TryParse(idBrut, out var etudeId) && context.User.Identity?.IsAuthenticated == true)
    {
        var utilisateurId = ObtenirUtilisateurId(context.User);
        if (utilisateurId is not null)
        {
            var etude = await context.RequestServices.GetRequiredService<IEtudeRepository>()
                .ObtenirParIdAsync(etudeId, context.RequestAborted);
            if (etude is not null)
            {
                // La duplication est un POST sur l'etude *source* mais ne fait
                // que la lire (le contenu recopie va dans une nouvelle etude
                // dont l'appelant devient proprietaire) : accessible a tout
                // membre, y compris Lecteur, et sur l'etude de demonstration.
                var estDuplication = (context.Request.Path.Value ?? "").EndsWith("/dupliquer", StringComparison.OrdinalIgnoreCase);
                var estEcriture = !HttpMethods.IsGet(context.Request.Method)
                    && !HttpMethods.IsHead(context.Request.Method)
                    && !estDuplication;

                if (etude.ProprietaireId is null)
                {
                    if (estEcriture)
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsJsonAsync(new { error = "Cette etude de demonstration est en lecture seule -- creez votre propre etude pour la modifier." });
                        return;
                    }
                }
                else
                {
                    var membreRepo = context.RequestServices.GetRequiredService<IEtudeMembreRepository>();
                    var membre = await membreRepo.ObtenirAsync(etudeId, utilisateurId.Value, context.RequestAborted);

                    // Filet de securite : si le createur d'origine n'a pas de ligne
                    // etude_membres (derive de donnees, transaction interrompue...),
                    // on la recree plutot que de le verrouiller hors de son etude.
                    if (membre is null && etude.ProprietaireId == utilisateurId.Value)
                    {
                        membre = EtudeMembre.Creer(etudeId, utilisateurId.Value, RoleEtude.Proprietaire, utilisateurId.Value);
                        await membreRepo.AjouterAsync(membre, context.RequestAborted);
                    }
                    if (membre is null)
                    {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }
                    if (estEcriture)
                    {
                        var chemin = context.Request.Path.Value ?? "";
                        var actionProprietaire = chemin.Contains("/membres")
                            || (HttpMethods.IsDelete(context.Request.Method) && System.Text.RegularExpressions.Regex.IsMatch(chemin, @"^/api/v1/etudes/[0-9a-fA-F-]{36}/?$"));
                        var roleRequis = actionProprietaire ? RoleEtude.Proprietaire : RoleEtude.Editeur;
                        if (membre.Role < roleRequis)
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            await context.Response.WriteAsJsonAsync(new
                            {
                                error = roleRequis == RoleEtude.Proprietaire
                                    ? "Reserve au proprietaire de l'etude."
                                    : "Votre role (Lecteur) ne permet pas de modifier cette etude.",
                            });
                            return;
                        }
                    }
                }
            }
        }
    }
    await next(context);
});

// Journal d'audit : apres chaque ecriture reussie sur une route d'etude, on
// consigne qui / quoi / quand. Centralise ici plutot que dans chaque handler.
// Append-only : aucune interface ne modifie ni ne supprime une entree. Les
// echecs de journalisation ne font jamais echouer la requete de l'utilisateur.
app.Use(async (context, next) =>
{
    await next(context);

    var methode = context.Request.Method;
    if (HttpMethods.IsGet(methode) || HttpMethods.IsHead(methode) || HttpMethods.IsOptions(methode))
        return;
    if (context.Response.StatusCode is < 200 or >= 300)
        return;

    var routeValue = context.GetRouteValue("etudeId") ?? context.GetRouteValue("id");
    if (routeValue is not string idBrut || !Guid.TryParse(idBrut, out var etudeIdJournal))
        return;

    var auteurId = ObtenirUtilisateurId(context.User);
    if (auteurId is null)
        return;

    try
    {
        var repoJournal = context.RequestServices.GetRequiredService<IEntreeJournalRepository>();
        var repoUtil = context.RequestServices.GetRequiredService<IUtilisateurRepository>();
        var auteur = await repoUtil.ObtenirParIdAsync(auteurId.Value, CancellationToken.None);
        var chemin = context.Request.Path.Value ?? "";
        await repoJournal.AjouterAsync(
            EntreeJournal.Creer(
                etudeIdJournal, auteurId, auteur?.NomAffiche ?? auteur?.Email ?? "inconnu",
                DescriptionAction.Deriver(methode, chemin), methode, chemin, context.Response.StatusCode),
            CancellationToken.None);
    }
    catch (Exception ex)
    {
        context.RequestServices.GetService<ILoggerFactory>()?
            .CreateLogger("JournalAudit").LogWarning(ex, "Echec d'ecriture du journal d'audit");
    }
});

Guid? ObtenirUtilisateurId(System.Security.Claims.ClaimsPrincipal principal)
{
    var idClaim = principal.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
    return Guid.TryParse(idClaim, out var id) ? id : null;
}

// Projette une etude + le role de l'appelant ("monRole" : Proprietaire /
// Editeur / Lecteur, ou null pour l'etude de demonstration publique).
static object AvecRole(Etude e, string? monRole) => new
{
    e.Id, e.Nom, e.Perimetre, e.Mission, e.VersionReferentielId,
    Statut = e.Statut.ToString(),
    StatutAtelier2 = e.StatutAtelier2.ToString(),
    StatutAtelier3 = e.StatutAtelier3.ToString(),
    StatutAtelier4 = e.StatutAtelier4.ToString(),
    StatutAtelier5 = e.StatutAtelier5.ToString(),
    e.CreeLeUtc, e.ProprietaireId,
    monRole,
};

app.MapGet("/api/v1/health", async (EbiosDbContext db) =>
{
    var databaseConnected = await db.Database.CanConnectAsync();
    return Results.Ok(new
    {
        status = "ok",
        application = "EbiosRM.Api",
        database = databaseConnected ? "connected" : "disconnected",
        timestampUtc = DateTime.UtcNow
    });
}).AllowAnonymous();

// --- Authentification ---

app.MapPost("/api/v1/auth/inscription", async (
    InscriptionRequest request, ServiceAuthentification service, CancellationToken ct) =>
{
    try
    {
        var (token, utilisateur) = await service.InscrireAsync(request.Email, request.MotDePasse, request.NomAffiche, ct);
        return Results.Created($"/api/v1/auth/moi", new { token, utilisateur = new { utilisateur.Id, utilisateur.Email, utilisateur.NomAffiche } });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).AllowAnonymous().RequireRateLimiting("inscription");

app.MapPost("/api/v1/auth/connexion", async (
    ConnexionRequest request, ServiceAuthentification service, CancellationToken ct) =>
{
    var resultat = await service.ConnecterAsync(request.Email, request.MotDePasse, ct);
    if (resultat is null)
        return Results.Unauthorized();

    var (token, utilisateur) = resultat.Value;
    return Results.Ok(new { token, utilisateur = new { utilisateur.Id, utilisateur.Email, utilisateur.NomAffiche } });
}).AllowAnonymous();

app.MapGet("/api/v1/auth/moi", async (
    System.Security.Claims.ClaimsPrincipal principal, IUtilisateurRepository repo, CancellationToken ct) =>
{
    var idClaim = principal.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
    if (idClaim is null || !Guid.TryParse(idClaim, out var id))
        return Results.Unauthorized();

    var utilisateur = await repo.ObtenirParIdAsync(id, ct);
    if (utilisateur is null)
        return Results.Unauthorized();

    return Results.Ok(new { utilisateur.Id, utilisateur.Email, utilisateur.NomAffiche });
});

// --- Etudes ---

app.MapPost("/api/v1/etudes", async (
    CreerEtudeRequest request, IEtudeRepository repo, IEtudeMembreRepository membreRepo,
    System.Security.Claims.ClaimsPrincipal principal, CancellationToken ct) =>
{
    try
    {
        // Le createur devient Proprietaire (table etude_membres) ; ProprietaireId
        // reste le marqueur du createur d'origine. Seule l'etude de demonstration
        // (deja en base avant ce chantier) reste publique (ProprietaireId null).
        var proprietaireId = ObtenirUtilisateurId(principal);
        var etude = Etude.Creer(request.Nom, request.Perimetre, request.Mission, proprietaireId);
        await repo.AjouterAsync(etude, ct);
        if (proprietaireId is not null)
            await membreRepo.AjouterAsync(EtudeMembre.Creer(etude.Id, proprietaireId.Value, RoleEtude.Proprietaire, proprietaireId), ct);
        return Results.Created($"/api/v1/etudes/{etude.Id}", etude);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/v1/etudes/{id:guid}", async (
    Guid id, IEtudeRepository repo, IEtudeMembreRepository membreRepo,
    System.Security.Claims.ClaimsPrincipal principal, CancellationToken ct) =>
{
    // Visibilite deja verifiee par le middleware -- si on arrive ici, l'appelant
    // a le droit de voir l'etude. On joint son role pour que le frontend adapte
    // ses controles (edition / gestion des membres).
    var etude = await repo.ObtenirParIdAsync(id, ct);
    if (etude is null) return Results.NotFound();

    var moiId = ObtenirUtilisateurId(principal);
    var monRole = moiId is null ? null : (await membreRepo.ObtenirAsync(id, moiId.Value, ct))?.Role.ToString();
    return Results.Ok(AvecRole(etude, monRole));
});

// Export/sauvegarde complete d'une etude en JSON -- couvre le contenu editable
// des 5 ateliers (pas les snapshots figes, deja des rapports PDF derives, pas
// des donnees sources). Beneficie automatiquement du middleware de visibilite
// des etudes (etudeId dans la route) : 404 si non visible, aucune restriction
// d'ecriture puisque c'est une simple lecture.
app.MapGet("/api/v1/etudes/{etudeId:guid}/journal", async (
    Guid etudeId, IEntreeJournalRepository journalRepo, int? limite, CancellationToken ct) =>
{
    var entrees = await journalRepo.ListerParEtudeAsync(etudeId, Math.Clamp(limite ?? 200, 1, 1000), ct);
    return Results.Ok(entrees.Select(e => new
    {
        e.Id, e.DateUtc, e.NomUtilisateur, e.Action, e.Methode, e.Chemin, e.StatutHttp,
    }));
});

// --- Membres d'une etude (partage) ---

app.MapGet("/api/v1/etudes/{etudeId:guid}/membres", async (
    Guid etudeId, IEtudeMembreRepository membreRepo, IUtilisateurRepository utilRepo,
    System.Security.Claims.ClaimsPrincipal principal, CancellationToken ct) =>
{
    var moiId = ObtenirUtilisateurId(principal);
    var membres = await membreRepo.ListerParEtudeAsync(etudeId, ct);
    var resultat = new List<object>();
    foreach (var m in membres)
    {
        var u = await utilRepo.ObtenirParIdAsync(m.UtilisateurId, ct);
        resultat.Add(new
        {
            m.UtilisateurId,
            nomAffiche = u?.NomAffiche ?? "(compte supprime)",
            email = u?.Email ?? "",
            role = m.Role.ToString(),
            m.AjouteLeUtc,
            estMoi = m.UtilisateurId == moiId,
        });
    }
    return Results.Ok(resultat);
});

app.MapPost("/api/v1/etudes/{etudeId:guid}/membres", async (
    Guid etudeId, AjouterMembreRequest request, IEtudeMembreRepository membreRepo,
    IUtilisateurRepository utilRepo, System.Security.Claims.ClaimsPrincipal principal, CancellationToken ct) =>
{
    if (!Enum.TryParse<RoleEtude>(request.Role, ignoreCase: true, out var role))
        return Results.BadRequest(new { error = $"Role invalide. Valeurs : {string.Join(", ", Enum.GetNames<RoleEtude>())}." });

    var utilisateur = await utilRepo.ObtenirParEmailAsync((request.Email ?? "").Trim().ToLowerInvariant(), ct);
    if (utilisateur is null)
        return Results.NotFound(new { error = "Aucun compte avec cet email. La personne doit d'abord creer un compte." });

    if (await membreRepo.ObtenirAsync(etudeId, utilisateur.Id, ct) is not null)
        return Results.Conflict(new { error = "Cette personne est deja membre de l'etude." });

    await membreRepo.AjouterAsync(EtudeMembre.Creer(etudeId, utilisateur.Id, role, ObtenirUtilisateurId(principal)), ct);
    return Results.Created($"/api/v1/etudes/{etudeId}/membres/{utilisateur.Id}", new { utilisateur.Id, utilisateur.NomAffiche, utilisateur.Email, role = role.ToString() });
});

app.MapPut("/api/v1/etudes/{etudeId:guid}/membres/{utilisateurId:guid}", async (
    Guid etudeId, Guid utilisateurId, ChangerRoleMembreRequest request, IEtudeMembreRepository membreRepo, CancellationToken ct) =>
{
    if (!Enum.TryParse<RoleEtude>(request.Role, ignoreCase: true, out var role))
        return Results.BadRequest(new { error = "Role invalide." });

    var membre = await membreRepo.ObtenirAsync(etudeId, utilisateurId, ct);
    if (membre is null) return Results.NotFound();

    if (membre.Role == RoleEtude.Proprietaire && role != RoleEtude.Proprietaire
        && await membreRepo.CompterProprietairesAsync(etudeId, ct) <= 1)
        return Results.Conflict(new { error = "L'etude doit garder au moins un proprietaire." });

    membre.ChangerRole(role);
    await membreRepo.MettreAJourAsync(membre, ct);
    return Results.Ok(new { utilisateurId, role = role.ToString() });
});

app.MapDelete("/api/v1/etudes/{etudeId:guid}/membres/{utilisateurId:guid}", async (
    Guid etudeId, Guid utilisateurId, IEtudeMembreRepository membreRepo, CancellationToken ct) =>
{
    var membre = await membreRepo.ObtenirAsync(etudeId, utilisateurId, ct);
    if (membre is null) return Results.NotFound();

    if (membre.Role == RoleEtude.Proprietaire && await membreRepo.CompterProprietairesAsync(etudeId, ct) <= 1)
        return Results.Conflict(new { error = "L'etude doit garder au moins un proprietaire." });

    await membreRepo.SupprimerAsync(membre, ct);
    return Results.NoContent();
});

app.MapGet("/api/v1/etudes/{etudeId:guid}/export", async (
    Guid etudeId,
    IEtudeRepository etudeRepo,
    IValeurMetierRepository valeurMetierRepo,
    IBienSupportRepository bienSupportRepo,
    IEvenementRedouteRepository evenementRedouteRepo,
    ISocleSecuriteRepository socleRepo,
    ICoupleSourceRisqueObjectifViseRepository coupleRepo,
    IPartiePrenanteRepository partiePrenanteRepo,
    IScenarioStrategiqueRepository scenarioStrategiqueRepo,
    ICheminAttaqueRepository cheminAttaqueRepo,
    IScenarioOperationnelRepository scenarioOperationnelRepo,
    IScenarioDeRisqueRepository scenarioDeRisqueRepo,
    IPlanTraitementRisqueRepository planTraitementRepo,
    CancellationToken ct) =>
{
    var etude = await etudeRepo.ObtenirParIdAsync(etudeId, ct);
    if (etude is null) return Results.NotFound();

    var export = new
    {
        formatVersion = 1,
        exporteLeUtc = DateTime.UtcNow,
        etude,
        valeursMetier = await valeurMetierRepo.ListerParEtudeAsync(etudeId, ct),
        biensSupport = await bienSupportRepo.ListerParEtudeAsync(etudeId, ct),
        evenementsRedoutes = await evenementRedouteRepo.ListerParEtudeAsync(etudeId, ct),
        socleSecurite = await socleRepo.ObtenirParEtudeAsync(etudeId, ct),
        couplesSourceRisqueObjectifVise = await coupleRepo.ListerParEtudeAsync(etudeId, ct),
        partiesPrenantes = await partiePrenanteRepo.ListerParEtudeAsync(etudeId, ct),
        scenariosStrategiques = await scenarioStrategiqueRepo.ListerParEtudeAsync(etudeId, ct),
        cheminsAttaque = await cheminAttaqueRepo.ListerParEtudeAsync(etudeId, ct),
        scenariosOperationnels = await scenarioOperationnelRepo.ListerParEtudeAsync(etudeId, ct),
        scenariosDeRisque = await scenarioDeRisqueRepo.ListerParEtudeAsync(etudeId, ct),
        planTraitementRisque = await planTraitementRepo.ObtenirParEtudeAsync(etudeId, ct),
    };

    return Results.Ok(export);
});

// Duplication d'une etude (base de "modeles") : recopie tout le contenu
// editable des 5 ateliers dans une nouvelle etude dont l'appelant devient
// proprietaire. Snapshots figes, journal et membres non copies ; les 5
// ateliers repartent en brouillon. Accessible a tout membre de la source
// (y compris Lecteur) et a l'etude de demonstration -- cf. middleware.
app.MapPost("/api/v1/etudes/{etudeId:guid}/dupliquer", async (
    Guid etudeId, DupliquerEtudeRequest? request, ServiceDuplicationEtude service,
    IEtudeMembreRepository membreRepo, System.Security.Claims.ClaimsPrincipal principal,
    CancellationToken ct) =>
{
    var proprietaireId = ObtenirUtilisateurId(principal);
    if (proprietaireId is null)
        return Results.Unauthorized();

    var nouvelleId = await service.DupliquerAsync(etudeId, request?.Nom, proprietaireId, ct);
    if (nouvelleId is null)
        return Results.NotFound(new { error = "Étude introuvable." });

    await membreRepo.AjouterAsync(
        EtudeMembre.Creer(nouvelleId.Value, proprietaireId.Value, RoleEtude.Proprietaire, proprietaireId), ct);

    return Results.Created($"/api/v1/etudes/{nouvelleId}", new { id = nouvelleId });
});

// Import d'une etude depuis un fichier JSON produit par .../export (autre
// installation, sauvegarde, transfert entre comptes non partages). Le corps
// de la requete EST le fichier. L'appelant devient proprietaire de l'etude
// creee. Route sans etudeId -> le middleware d'isolation ne s'applique pas ;
// seule l'authentification est requise.
app.MapPost("/api/v1/etudes/importer", async (
    HttpRequest request, ServiceImportEtude service, IEtudeMembreRepository membreRepo,
    System.Security.Claims.ClaimsPrincipal principal, CancellationToken ct) =>
{
    var proprietaireId = ObtenirUtilisateurId(principal);
    if (proprietaireId is null)
        return Results.Unauthorized();

    // Un export d'etude complete fait ~300-400 Ko. Kestrel plafonne deja le
    // corps a 30 Mo ; on refuse plus tot et proprement au-dela de 15 Mo.
    const long tailleMax = 15L * 1024 * 1024;
    if (request.ContentLength > tailleMax)
        return Results.BadRequest(new { error = "Fichier trop volumineux (max 15 Mo pour un export d'etude)." });

    var resultat = await service.ImporterAsync(request.Body, proprietaireId.Value, ct);
    if (resultat.Erreur is not null)
        return Results.BadRequest(new { error = resultat.Erreur });

    await membreRepo.AjouterAsync(
        EtudeMembre.Creer(resultat.EtudeId!.Value, proprietaireId.Value, RoleEtude.Proprietaire, proprietaireId), ct);

    return Results.Created($"/api/v1/etudes/{resultat.EtudeId}", new { id = resultat.EtudeId });
});

// --- Bibliotheque : elements reutilisables d'une etude a l'autre ------------
// Deux origines fusionnees a la lecture : le catalogue systeme (CatalogueSysteme,
// dans le code, jamais en base -- ISO 27002 + hygiene ANSSI) et les entrees
// personnelles de l'appelant (persistees). Routes sans etudeId -> hors des
// middlewares d'isolation et de journal.

static object VueMesureBiblio(MesureBibliotheque m) => new
{
    m.Id, systeme = m.EstSysteme, referentiel = m.Referentiel.ToString(),
    m.Code, m.Titre, m.Description, m.Categorie,
};

static bool Contient(string? valeur, string terme)
    => valeur is not null && valeur.Contains(terme, StringComparison.OrdinalIgnoreCase);

app.MapGet("/api/v1/bibliotheque/mesures", async (
    string? referentiel, string? q, IBibliothequeRepository repo,
    System.Security.Claims.ClaimsPrincipal principal, CancellationToken ct) =>
{
    var moiId = ObtenirUtilisateurId(principal);
    if (moiId is null) return Results.Unauthorized();

    IEnumerable<MesureBibliotheque> mesures = CatalogueSysteme.Mesures
        .Concat(await repo.ListerMesuresAsync(moiId.Value, ct));

    if (Enum.TryParse<ReferentielMesure>(referentiel, ignoreCase: true, out var r))
        mesures = mesures.Where(m => m.Referentiel == r);

    if (!string.IsNullOrWhiteSpace(q))
    {
        var terme = q.Trim();
        mesures = mesures.Where(m => Contient(m.Titre, terme) || Contient(m.Code, terme) || Contient(m.Categorie, terme));
    }

    return Results.Ok(mesures
        .OrderBy(m => m.EstSysteme ? 1 : 0)
        .ThenBy(m => m.Referentiel).ThenBy(m => m.Code, StringComparer.OrdinalIgnoreCase)
        .Select(VueMesureBiblio));
});

app.MapPost("/api/v1/bibliotheque/mesures", async (
    AjouterMesureBiblioRequest request, IBibliothequeRepository repo,
    System.Security.Claims.ClaimsPrincipal principal, CancellationToken ct) =>
{
    var moiId = ObtenirUtilisateurId(principal);
    if (moiId is null) return Results.Unauthorized();

    if (!Enum.TryParse<ReferentielMesure>(request.Referentiel, ignoreCase: true, out var referentiel))
        referentiel = ReferentielMesure.Libre;

    try
    {
        var mesure = MesureBibliotheque.Creer(moiId.Value, referentiel, request.Code, request.Titre, request.Description, request.Categorie);
        await repo.AjouterMesureAsync(mesure, ct);
        return Results.Created($"/api/v1/bibliotheque/mesures/{mesure.Id}", VueMesureBiblio(mesure));
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapDelete("/api/v1/bibliotheque/mesures/{id:guid}", async (
    Guid id, IBibliothequeRepository repo, System.Security.Claims.ClaimsPrincipal principal, CancellationToken ct) =>
{
    var moiId = ObtenirUtilisateurId(principal);
    if (moiId is null) return Results.Unauthorized();

    var mesure = await repo.ObtenirMesureAsync(id, ct);
    if (mesure is null || mesure.ProprietaireId != moiId)
        return Results.NotFound(new { error = "Mesure introuvable dans votre bibliotheque (le catalogue systeme n'est pas modifiable)." });

    await repo.SupprimerMesureAsync(mesure, ct);
    return Results.NoContent();
});

static object VueSourceRisqueBiblio(SourceRisqueBibliotheque s) => new
{
    s.Id, systeme = s.EstSysteme,
    sourceRisque = s.SourceRisque.ToString(), s.DescriptionSourceRisque,
    objectifVise = s.ObjectifVise.ToString(), s.DescriptionObjectifVise,
    s.Theme, s.MotivationTypique, s.RessourcesTypiques,
};

app.MapGet("/api/v1/bibliotheque/sources-risque", async (
    string? q, IBibliothequeRepository repo, System.Security.Claims.ClaimsPrincipal principal, CancellationToken ct) =>
{
    var moiId = ObtenirUtilisateurId(principal);
    if (moiId is null) return Results.Unauthorized();

    IEnumerable<SourceRisqueBibliotheque> sources = CatalogueSysteme.SourcesRisque
        .Concat(await repo.ListerSourcesRisqueAsync(moiId.Value, ct));

    if (!string.IsNullOrWhiteSpace(q))
    {
        var terme = q.Trim();
        sources = sources.Where(s =>
            Contient(s.DescriptionSourceRisque, terme) || Contient(s.DescriptionObjectifVise, terme)
            || Contient(s.SourceRisque.ToString(), terme) || Contient(s.ObjectifVise.ToString(), terme)
            || Contient(s.Theme, terme));
    }

    return Results.Ok(sources.OrderBy(s => s.EstSysteme ? 1 : 0).ThenBy(s => s.DescriptionSourceRisque).Select(VueSourceRisqueBiblio));
});

app.MapPost("/api/v1/bibliotheque/sources-risque", async (
    AjouterSourceRisqueBiblioRequest request, IBibliothequeRepository repo,
    System.Security.Claims.ClaimsPrincipal principal, CancellationToken ct) =>
{
    var moiId = ObtenirUtilisateurId(principal);
    if (moiId is null) return Results.Unauthorized();

    if (!Enum.TryParse<CategorieSourceRisque>(request.SourceRisque, ignoreCase: true, out var sr)
        || !Enum.TryParse<CategorieObjectifVise>(request.ObjectifVise, ignoreCase: true, out var ov))
        return Results.BadRequest(new { error = "Categorie de source de risque ou d'objectif vise invalide." });

    try
    {
        var source = SourceRisqueBibliotheque.Creer(
            moiId.Value, sr, request.DescriptionSourceRisque, ov, request.DescriptionObjectifVise,
            request.Theme, request.MotivationTypique, request.RessourcesTypiques);
        await repo.AjouterSourceRisqueAsync(source, ct);
        return Results.Created($"/api/v1/bibliotheque/sources-risque/{source.Id}", VueSourceRisqueBiblio(source));
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapDelete("/api/v1/bibliotheque/sources-risque/{id:guid}", async (
    Guid id, IBibliothequeRepository repo, System.Security.Claims.ClaimsPrincipal principal, CancellationToken ct) =>
{
    var moiId = ObtenirUtilisateurId(principal);
    if (moiId is null) return Results.Unauthorized();

    var source = await repo.ObtenirSourceRisqueAsync(id, ct);
    if (source is null || source.ProprietaireId != moiId)
        return Results.NotFound(new { error = "Source de risque introuvable dans votre bibliotheque (le catalogue systeme n'est pas modifiable)." });

    await repo.SupprimerSourceRisqueAsync(source, ct);
    return Results.NoContent();
});

// Catalogue MITRE ATT&CK Enterprise (techniques de 1er niveau), embarque dans
// le code. Filtre optionnel par phase EBIOS RM (Connaitre/Rentrer/Trouver/
// Exploiter) et recherche plein texte sur l'identifiant / le nom / la tactique.
app.MapGet("/api/v1/referentiels/mitre", (string? phase, string? q) =>
{
    IEnumerable<CatalogueMitre.Technique> techniques = CatalogueMitre.Techniques;

    if (!string.IsNullOrWhiteSpace(phase))
        techniques = techniques.Where(t => string.Equals(t.PhaseEbios, phase, StringComparison.OrdinalIgnoreCase));

    if (!string.IsNullOrWhiteSpace(q))
    {
        var terme = q.Trim();
        techniques = techniques.Where(t =>
            t.Id.Contains(terme, StringComparison.OrdinalIgnoreCase)
            || t.Nom.Contains(terme, StringComparison.OrdinalIgnoreCase)
            || t.Tactique.Contains(terme, StringComparison.OrdinalIgnoreCase));
    }

    return Results.Ok(techniques.OrderBy(t => t.Id, StringComparer.OrdinalIgnoreCase));
});

// Catalogue des exigences de conformite (ISO 27001 Annexe A + NIS2 art. 21),
// embarque dans le code. Sert au selecteur cote frontend.
app.MapGet("/api/v1/referentiels/conformite", (string? referentiel) =>
{
    IEnumerable<EbiosRM.Api.Modules.Conformite.Domain.ExigenceConformite> exigences =
        EbiosRM.Api.Modules.Conformite.Domain.CatalogueConformite.Iso27001
            .Concat(EbiosRM.Api.Modules.Conformite.Domain.CatalogueConformite.Nis2);

    if (Enum.TryParse<EbiosRM.Api.Modules.Conformite.Domain.ReferentielConformite>(referentiel, ignoreCase: true, out var r))
        exigences = EbiosRM.Api.Modules.Conformite.Domain.CatalogueConformite.Pour(r);

    return Results.Ok(exigences.Select(e => new
    {
        referentiel = e.Referentiel.ToString(), e.Code, e.Titre, e.Categorie,
    }));
});

// Tableau de couverture de conformite d'une etude (ISO 27001 ou NIS2) :
// croise le socle de securite (A1) et le plan de traitement (A5) avec les
// exigences du referentiel. GET -> lecture, visible par tout membre.
app.MapGet("/api/v1/etudes/{etudeId:guid}/conformite", async (
    Guid etudeId, string? referentiel, ServiceConformite service, CancellationToken ct) =>
{
    if (!Enum.TryParse<EbiosRM.Api.Modules.Conformite.Domain.ReferentielConformite>(referentiel, ignoreCase: true, out var r))
        r = EbiosRM.Api.Modules.Conformite.Domain.ReferentielConformite.Iso27001;

    var rapport = await service.ConstruireAsync(etudeId, r, ct);
    return rapport is null ? Results.NotFound() : Results.Ok(rapport);
});

app.MapGet("/api/v1/etudes/{etudeId:guid}/rapports/conformite", async (
    Guid etudeId, ServiceConformite service, RapportConformitePdfGenerator pdf,
    IEtudeRepository etudeRepo, CancellationToken ct) =>
{
    var etude = await etudeRepo.ObtenirParIdAsync(etudeId, ct);
    if (etude is null) return Results.NotFound();

    var rapports = new List<ServiceConformite.RapportConformite>();
    foreach (var r in new[] { EbiosRM.Api.Modules.Conformite.Domain.ReferentielConformite.Iso27001, EbiosRM.Api.Modules.Conformite.Domain.ReferentielConformite.Nis2 })
    {
        var rapport = await service.ConstruireAsync(etudeId, r, ct);
        if (rapport is not null) rapports.Add(rapport);
    }

    return Results.File(pdf.Generer(etude.Nom, rapports), "application/pdf", $"conformite-{etudeId}.pdf");
});

// --- Suivi : vue portefeuille, evolution N/N-1, indicateurs (KRI) ---

app.MapGet("/api/v1/portefeuille", async (
    ServicePortefeuille service, System.Security.Claims.ClaimsPrincipal principal, CancellationToken ct) =>
{
    var moiId = ObtenirUtilisateurId(principal);
    if (moiId is null) return Results.Unauthorized();
    return Results.Ok(await service.ConstruireAsync(moiId.Value, ct));
});

app.MapGet("/api/v1/portefeuille/export.xlsx", async (
    EbiosRM.Api.Modules.Reporting.Exports.PortefeuilleExcelGenerator generateur,
    System.Security.Claims.ClaimsPrincipal principal, CancellationToken ct) =>
{
    var moiId = ObtenirUtilisateurId(principal);
    if (moiId is null) return Results.Unauthorized();
    var octets = await generateur.GenererAsync(moiId.Value, ct);
    return Results.File(octets, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "portefeuille.xlsx");
});

// --- Exports bureautiques d'une etude (Excel / Word) ---

app.MapGet("/api/v1/etudes/{etudeId:guid}/exports/registre.xlsx", async (
    Guid etudeId, EbiosRM.Api.Modules.Reporting.Exports.RegistreRisquesExcelGenerator generateur, CancellationToken ct) =>
{
    var octets = await generateur.GenererAsync(etudeId, ct);
    return octets is null
        ? Results.NotFound()
        : Results.File(octets, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"registre-risques-{etudeId}.xlsx");
});

app.MapGet("/api/v1/etudes/{etudeId:guid}/exports/synthese.docx", async (
    Guid etudeId, EbiosRM.Api.Modules.Reporting.Exports.SyntheseWordGenerator generateur, CancellationToken ct) =>
{
    var octets = await generateur.GenererAsync(etudeId, ct);
    return octets is null
        ? Results.NotFound()
        : Results.File(octets, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"synthese-{etudeId}.docx");
});

app.MapGet("/api/v1/etudes/{etudeId:guid}/evolution", async (
    Guid etudeId, ServiceEvolutionEtude service, CancellationToken ct) =>
{
    var evolution = await service.ConstruireAsync(etudeId, ct);
    return evolution is null ? Results.NotFound() : Results.Ok(evolution);
});

static object VueIndicateur(IndicateurSuivi i) => new
{
    i.Id, i.Nom, i.Categorie, i.Unite, i.Cible, i.SeuilAlerte,
    sens = i.Sens.ToString(),
    points = i.Points.OrderBy(p => p.Date).Select(p => new { p.Id, date = p.Date.ToString("yyyy-MM-dd"), p.Valeur, p.Commentaire }),
};

app.MapGet("/api/v1/etudes/{etudeId:guid}/indicateurs", async (
    Guid etudeId, IIndicateurSuiviRepository repo, ServiceIndicateursAuto auto, CancellationToken ct) =>
{
    var manuels = await repo.ListerParEtudeAsync(etudeId, ct);
    return Results.Ok(new
    {
        automatiques = await auto.ConstruireAsync(etudeId, ct),
        manuels = manuels.Select(VueIndicateur),
    });
});

app.MapPost("/api/v1/etudes/{etudeId:guid}/indicateurs", async (
    Guid etudeId, IndicateurRequest request, IIndicateurSuiviRepository repo, CancellationToken ct) =>
{
    if (!Enum.TryParse<SensAmelioration>(request.Sens, ignoreCase: true, out var sens))
        return Results.BadRequest(new { error = "Sens invalide (Baisse ou Hausse)." });
    try
    {
        var indicateur = IndicateurSuivi.Creer(etudeId, request.Nom, request.Categorie, request.Unite, request.Cible, request.SeuilAlerte, sens);
        await repo.AjouterAsync(indicateur, ct);
        return Results.Created($"/api/v1/etudes/{etudeId}/indicateurs/{indicateur.Id}", VueIndicateur(indicateur));
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/v1/etudes/{etudeId:guid}/indicateurs/{indicId:guid}", async (
    Guid etudeId, Guid indicId, IndicateurRequest request, IIndicateurSuiviRepository repo, CancellationToken ct) =>
{
    var indicateur = await repo.ObtenirAsync(indicId, ct);
    if (indicateur is null || indicateur.EtudeId != etudeId) return Results.NotFound();
    if (!Enum.TryParse<SensAmelioration>(request.Sens, ignoreCase: true, out var sens))
        return Results.BadRequest(new { error = "Sens invalide (Baisse ou Hausse)." });
    try
    {
        indicateur.Modifier(request.Nom, request.Categorie, request.Unite, request.Cible, request.SeuilAlerte, sens);
        await repo.MettreAJourAsync(indicateur, ct);
        return Results.Ok(VueIndicateur(indicateur));
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapDelete("/api/v1/etudes/{etudeId:guid}/indicateurs/{indicId:guid}", async (
    Guid etudeId, Guid indicId, IIndicateurSuiviRepository repo, CancellationToken ct) =>
{
    var indicateur = await repo.ObtenirAsync(indicId, ct);
    if (indicateur is null || indicateur.EtudeId != etudeId) return Results.NotFound();
    await repo.SupprimerAsync(indicateur, ct);
    return Results.NoContent();
});

app.MapPost("/api/v1/etudes/{etudeId:guid}/indicateurs/{indicId:guid}/points", async (
    Guid etudeId, Guid indicId, PointMesureRequest request, IIndicateurSuiviRepository repo, CancellationToken ct) =>
{
    var indicateur = await repo.ObtenirAsync(indicId, ct);
    if (indicateur is null || indicateur.EtudeId != etudeId) return Results.NotFound();
    if (!DateOnly.TryParse(request.Date, out var date))
        return Results.BadRequest(new { error = "Date invalide (format attendu : AAAA-MM-JJ)." });

    indicateur.AjouterPoint(date, request.Valeur, request.Commentaire);
    await repo.MettreAJourAsync(indicateur, ct);
    return Results.Ok(VueIndicateur(indicateur));
});

app.MapDelete("/api/v1/etudes/{etudeId:guid}/indicateurs/{indicId:guid}/points/{pointId:guid}", async (
    Guid etudeId, Guid indicId, Guid pointId, IIndicateurSuiviRepository repo, CancellationToken ct) =>
{
    var indicateur = await repo.ObtenirAsync(indicId, ct);
    if (indicateur is null || indicateur.EtudeId != etudeId) return Results.NotFound();
    try
    {
        indicateur.SupprimerPoint(pointId);
        await repo.MettreAJourAsync(indicateur, ct);
        return Results.NoContent();
    }
    catch (ArgumentException)
    {
        return Results.NotFound();
    }
});

app.MapGet("/api/v1/etudes", async (
    IEtudeRepository repo, IEtudeMembreRepository membreRepo,
    System.Security.Claims.ClaimsPrincipal principal, CancellationToken ct) =>
{
    var utilisateurId = ObtenirUtilisateurId(principal);
    if (utilisateurId is null)
        return Results.Ok(Array.Empty<object>());

    var etudes = await repo.ListerVisiblesAsync(utilisateurId.Value, ct);
    var roles = (await membreRepo.ListerParUtilisateurAsync(utilisateurId.Value, ct))
        .ToDictionary(m => m.EtudeId, m => m.Role.ToString());
    return Results.Ok(etudes.Select(e => AvecRole(e, roles.GetValueOrDefault(e.Id))));
});

app.MapDelete("/api/v1/etudes/{id:guid}", async (
    Guid id, ServiceSuppressionEtude service, CancellationToken ct) =>
{
    var supprimee = await service.SupprimerAsync(id, ct);
    if (!supprimee)
        return Results.NotFound(new { error = "Étude introuvable." });
    return Results.NoContent();
});

// --- Workflow Engine minimal ---

app.MapPost("/api/v1/etudes/{id:guid}/demarrer-atelier1", async (
    Guid id, IEtudeRepository repo, CancellationToken ct) =>
{
    var etude = await repo.ObtenirParIdAsync(id, ct);
    if (etude is null)
        return Results.NotFound(new { error = "Étude introuvable." });

    try
    {
        etude.DemarrerAtelier1();
        await repo.MettreAJourAsync(etude, ct);
        return Results.Ok(etude);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/v1/etudes/{id:guid}/valider-atelier1", async (
    Guid id,
    IEtudeRepository repo,
    ServiceValidationCompletudeAtelier1 serviceValidation,
    ServiceCreationSnapshotAtelier1 serviceSnapshot,
    EbiosDbContext db,
    CancellationToken ct) =>
{
    var etude = await repo.ObtenirParIdAsync(id, ct);
    if (etude is null)
        return Results.NotFound(new { error = "Étude introuvable." });

    var resultat = await serviceValidation.VerifierAsync(id, ct);
    if (!resultat.EstComplet)
    {
        return Results.BadRequest(new
        {
            error = "L'atelier 1 n'est pas complet.",
            elementsManquants = resultat.ElementsManquants
        });
    }

    // Transaction unique : la transition de statut et la création du snapshot
    // doivent réussir ou échouer ensemble (P13 -- jamais d'étude "Validee"
    // sans son snapshot). Voir audit architectural, constat critique.
    await using var transaction = await db.Database.BeginTransactionAsync(ct);
    try
    {
        etude.ValiderAtelier1();
        await repo.MettreAJourAsync(etude, ct);

        var snapshot = await serviceSnapshot.CreerAsync(id, ct);

        await transaction.CommitAsync(ct);

        return Results.Ok(new { etude, snapshotVersion = snapshot.Version });
    }
    catch (InvalidOperationException ex)
    {
        await transaction.RollbackAsync(ct);
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/v1/etudes/{id:guid}/rouvrir-atelier1", async (
    Guid id, IEtudeRepository repo, CancellationToken ct) =>
{
    var etude = await repo.ObtenirParIdAsync(id, ct);
    if (etude is null)
        return Results.NotFound(new { error = "Étude introuvable." });

    try
    {
        etude.RouvrirAtelier1();
        await repo.MettreAJourAsync(etude, ct);
        return Results.Ok(etude);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/v1/etudes/{id:guid}/demarrer-atelier2", async (
    Guid id, IEtudeRepository repo, CancellationToken ct) =>
{
    var etude = await repo.ObtenirParIdAsync(id, ct);
    if (etude is null)
        return Results.NotFound(new { error = "Étude introuvable." });

    try
    {
        etude.DemarrerAtelier2();
        await repo.MettreAJourAsync(etude, ct);
        return Results.Ok(etude);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/v1/etudes/{id:guid}/valider-atelier2", async (
    Guid id,
    IEtudeRepository repo,
    ServiceValidationCompletudeAtelier2 serviceValidation,
    ServiceCreationSnapshotAtelier2 serviceSnapshot,
    EbiosDbContext db,
    CancellationToken ct) =>
{
    var etude = await repo.ObtenirParIdAsync(id, ct);
    if (etude is null)
        return Results.NotFound(new { error = "Étude introuvable." });

    var resultat = await serviceValidation.VerifierAsync(id, ct);
    if (!resultat.EstComplet)
    {
        return Results.BadRequest(new
        {
            error = "L'atelier 2 n'est pas complet.",
            elementsManquants = resultat.ElementsManquants
        });
    }

    // Transaction unique : la transition de statut et la création du snapshot
    // doivent réussir ou échouer ensemble (P13 -- jamais d'atelier "Validee"
    // sans son snapshot), même patron que valider-atelier1.
    await using var transaction = await db.Database.BeginTransactionAsync(ct);
    try
    {
        etude.ValiderAtelier2();
        await repo.MettreAJourAsync(etude, ct);

        var snapshot = await serviceSnapshot.CreerAsync(id, ct);

        await transaction.CommitAsync(ct);

        return Results.Ok(new { etude, snapshotVersion = snapshot.Version });
    }
    catch (InvalidOperationException ex)
    {
        await transaction.RollbackAsync(ct);
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/v1/etudes/{id:guid}/rouvrir-atelier2", async (
    Guid id, IEtudeRepository repo, CancellationToken ct) =>
{
    var etude = await repo.ObtenirParIdAsync(id, ct);
    if (etude is null)
        return Results.NotFound(new { error = "Étude introuvable." });

    try
    {
        etude.RouvrirAtelier2();
        await repo.MettreAJourAsync(etude, ct);
        return Results.Ok(etude);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/v1/etudes/{id:guid}/demarrer-atelier3", async (
    Guid id, IEtudeRepository repo, CancellationToken ct) =>
{
    var etude = await repo.ObtenirParIdAsync(id, ct);
    if (etude is null)
        return Results.NotFound(new { error = "Étude introuvable." });

    try
    {
        etude.DemarrerAtelier3();
        await repo.MettreAJourAsync(etude, ct);
        return Results.Ok(etude);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/v1/etudes/{id:guid}/valider-atelier3", async (
    Guid id,
    IEtudeRepository repo,
    ServiceValidationCompletudeAtelier3 serviceValidation,
    ServiceCreationSnapshotAtelier3 serviceSnapshot,
    EbiosDbContext db,
    CancellationToken ct) =>
{
    var etude = await repo.ObtenirParIdAsync(id, ct);
    if (etude is null)
        return Results.NotFound(new { error = "Étude introuvable." });

    var resultat = await serviceValidation.VerifierAsync(id, ct);
    if (!resultat.EstComplet)
    {
        return Results.BadRequest(new
        {
            error = "L'atelier 3 n'est pas complet.",
            elementsManquants = resultat.ElementsManquants
        });
    }

    await using var transaction = await db.Database.BeginTransactionAsync(ct);
    try
    {
        etude.ValiderAtelier3();
        await repo.MettreAJourAsync(etude, ct);

        var snapshot = await serviceSnapshot.CreerAsync(id, ct);

        await transaction.CommitAsync(ct);

        return Results.Ok(new { etude, snapshotVersion = snapshot.Version });
    }
    catch (InvalidOperationException ex)
    {
        await transaction.RollbackAsync(ct);
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/v1/etudes/{id:guid}/rouvrir-atelier3", async (
    Guid id, IEtudeRepository repo, CancellationToken ct) =>
{
    var etude = await repo.ObtenirParIdAsync(id, ct);
    if (etude is null)
        return Results.NotFound(new { error = "Étude introuvable." });

    try
    {
        etude.RouvrirAtelier3();
        await repo.MettreAJourAsync(etude, ct);
        return Results.Ok(etude);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/v1/etudes/{id:guid}/demarrer-atelier4", async (
    Guid id, IEtudeRepository repo, CancellationToken ct) =>
{
    var etude = await repo.ObtenirParIdAsync(id, ct);
    if (etude is null)
        return Results.NotFound(new { error = "Étude introuvable." });

    try
    {
        etude.DemarrerAtelier4();
        await repo.MettreAJourAsync(etude, ct);
        return Results.Ok(etude);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/v1/etudes/{id:guid}/valider-atelier4", async (
    Guid id,
    IEtudeRepository repo,
    ServiceValidationCompletudeAtelier4 serviceValidation,
    ServiceCreationSnapshotAtelier4 serviceSnapshot,
    EbiosDbContext db,
    CancellationToken ct) =>
{
    var etude = await repo.ObtenirParIdAsync(id, ct);
    if (etude is null)
        return Results.NotFound(new { error = "Étude introuvable." });

    var resultat = await serviceValidation.VerifierAsync(id, ct);
    if (!resultat.EstComplet)
    {
        return Results.BadRequest(new
        {
            error = "L'atelier 4 n'est pas complet.",
            elementsManquants = resultat.ElementsManquants
        });
    }

    await using var transaction = await db.Database.BeginTransactionAsync(ct);
    try
    {
        etude.ValiderAtelier4();
        await repo.MettreAJourAsync(etude, ct);

        var snapshot = await serviceSnapshot.CreerAsync(id, ct);

        await transaction.CommitAsync(ct);

        return Results.Ok(new { etude, snapshotVersion = snapshot.Version });
    }
    catch (InvalidOperationException ex)
    {
        await transaction.RollbackAsync(ct);
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/v1/etudes/{id:guid}/rouvrir-atelier4", async (
    Guid id, IEtudeRepository repo, CancellationToken ct) =>
{
    var etude = await repo.ObtenirParIdAsync(id, ct);
    if (etude is null)
        return Results.NotFound(new { error = "Étude introuvable." });

    try
    {
        etude.RouvrirAtelier4();
        await repo.MettreAJourAsync(etude, ct);
        return Results.Ok(etude);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/v1/etudes/{id:guid}/demarrer-atelier5", async (
    Guid id, IEtudeRepository repo, CancellationToken ct) =>
{
    var etude = await repo.ObtenirParIdAsync(id, ct);
    if (etude is null)
        return Results.NotFound(new { error = "Étude introuvable." });

    try
    {
        etude.DemarrerAtelier5();
        await repo.MettreAJourAsync(etude, ct);
        return Results.Ok(etude);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/v1/etudes/{id:guid}/valider-atelier5", async (
    Guid id,
    ValiderAtelier5Request? request,
    IEtudeRepository repo,
    ServiceValidationCompletudeAtelier5 serviceValidation,
    ServiceCreationSnapshotAtelier5 serviceSnapshot,
    EbiosDbContext db,
    CancellationToken ct) =>
{
    var etude = await repo.ObtenirParIdAsync(id, ct);
    if (etude is null)
        return Results.NotFound(new { error = "Étude introuvable." });

    var resultat = await serviceValidation.VerifierAsync(id, ct);
    if (!resultat.EstComplet)
    {
        return Results.BadRequest(new
        {
            error = "L'atelier 5 n'est pas complet.",
            elementsManquants = resultat.ElementsManquants
        });
    }

    await using var transaction = await db.Database.BeginTransactionAsync(ct);
    try
    {
        etude.ValiderAtelier5();
        await repo.MettreAJourAsync(etude, ct);

        var snapshot = await serviceSnapshot.CreerAsync(id, ct, request?.Libelle);

        await transaction.CommitAsync(ct);

        return Results.Ok(new { etude, snapshotVersion = snapshot.Version });
    }
    catch (InvalidOperationException ex)
    {
        await transaction.RollbackAsync(ct);
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/v1/etudes/{id:guid}/rouvrir-atelier5", async (
    Guid id, IEtudeRepository repo, CancellationToken ct) =>
{
    var etude = await repo.ObtenirParIdAsync(id, ct);
    if (etude is null)
        return Results.NotFound(new { error = "Étude introuvable." });

    try
    {
        etude.RouvrirAtelier5();
        await repo.MettreAJourAsync(etude, ct);
        return Results.Ok(etude);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// --- Valeurs métier ---

app.MapPost("/api/v1/etudes/{etudeId:guid}/valeurs-metier", async (
    Guid etudeId, CreerValeurMetierRequest request,
    IEtudeRepository etudeRepo, IValeurMetierRepository valeurRepo, CancellationToken ct) =>
{
    var etude = await etudeRepo.ObtenirParIdAsync(etudeId, ct);
    if (etude is null)
        return Results.NotFound(new { error = "Étude introuvable." });

    try
    {
        var valeurMetier = ValeurMetier.Creer(etudeId, request.Description, request.EntiteProprietaire);
        await valeurRepo.AjouterAsync(valeurMetier, ct);
        return Results.Created($"/api/v1/etudes/{etudeId}/valeurs-metier/{valeurMetier.Id}", valeurMetier);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/v1/etudes/{etudeId:guid}/valeurs-metier", async (
    Guid etudeId, IValeurMetierRepository valeurRepo, CancellationToken ct) =>
{
    var valeurs = await valeurRepo.ListerParEtudeAsync(etudeId, ct);
    return Results.Ok(valeurs);
});

app.MapPut("/api/v1/etudes/{etudeId:guid}/valeurs-metier/{id:guid}", async (
    Guid etudeId, Guid id, CreerValeurMetierRequest request,
    IValeurMetierRepository valeurRepo, CancellationToken ct) =>
{
    var valeurMetier = await valeurRepo.ObtenirParIdAsync(id, ct);
    if (valeurMetier is null || valeurMetier.EtudeId != etudeId)
        return Results.NotFound(new { error = "Valeur métier introuvable pour cette étude." });

    try
    {
        valeurMetier.Modifier(request.Description, request.EntiteProprietaire);
        await valeurRepo.MettreAJourAsync(valeurMetier, ct);
        return Results.Ok(valeurMetier);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapDelete("/api/v1/etudes/{etudeId:guid}/valeurs-metier/{id:guid}", async (
    Guid etudeId, Guid id, IValeurMetierRepository valeurRepo, CancellationToken ct) =>
{
    var valeurMetier = await valeurRepo.ObtenirParIdAsync(id, ct);
    if (valeurMetier is null || valeurMetier.EtudeId != etudeId)
        return Results.NotFound(new { error = "Valeur métier introuvable pour cette étude." });

    await valeurRepo.SupprimerAsync(valeurMetier, ct);
    return Results.NoContent();
});

// --- Biens support ---

app.MapPost("/api/v1/etudes/{etudeId:guid}/valeurs-metier/{valeurMetierId:guid}/biens-support", async (
    Guid etudeId, Guid valeurMetierId, CreerBienSupportRequest request,
    IEtudeRepository etudeRepo, IValeurMetierRepository valeurRepo, IBienSupportRepository bienRepo, CancellationToken ct) =>
{
    var etude = await etudeRepo.ObtenirParIdAsync(etudeId, ct);
    if (etude is null)
        return Results.NotFound(new { error = "Étude introuvable." });

    var valeursExistantes = await valeurRepo.ListerParEtudeAsync(etudeId, ct);
    if (!valeursExistantes.Any(v => v.Id == valeurMetierId))
        return Results.NotFound(new { error = "Valeur métier introuvable pour cette étude." });

    if (!Enum.TryParse<TypeBienSupport>(request.Type, ignoreCase: true, out var type))
        return Results.BadRequest(new { error = $"Type de bien support invalide. Valeurs autorisées : {string.Join(", ", Enum.GetNames<TypeBienSupport>())}" });

    try
    {
        var bienSupport = BienSupport.Creer(etudeId, valeurMetierId, request.Description, type, request.EntiteProprietaire);
        await bienRepo.AjouterAsync(bienSupport, ct);
        return Results.Created($"/api/v1/etudes/{etudeId}/biens-support/{bienSupport.Id}", bienSupport);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/v1/etudes/{etudeId:guid}/biens-support", async (
    Guid etudeId, IBienSupportRepository bienRepo, CancellationToken ct) =>
{
    var biens = await bienRepo.ListerParEtudeAsync(etudeId, ct);
    return Results.Ok(biens);
});

app.MapPut("/api/v1/etudes/{etudeId:guid}/biens-support/{id:guid}", async (
    Guid etudeId, Guid id, CreerBienSupportRequest request,
    IBienSupportRepository bienRepo, CancellationToken ct) =>
{
    var bienSupport = await bienRepo.ObtenirParIdAsync(id, ct);
    if (bienSupport is null || bienSupport.EtudeId != etudeId)
        return Results.NotFound(new { error = "Bien support introuvable pour cette étude." });

    if (!Enum.TryParse<TypeBienSupport>(request.Type, ignoreCase: true, out var type))
        return Results.BadRequest(new { error = $"Type de bien support invalide. Valeurs autorisées : {string.Join(", ", Enum.GetNames<TypeBienSupport>())}" });

    try
    {
        bienSupport.Modifier(request.Description, type, request.EntiteProprietaire);
        await bienRepo.MettreAJourAsync(bienSupport, ct);
        return Results.Ok(bienSupport);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapDelete("/api/v1/etudes/{etudeId:guid}/biens-support/{id:guid}", async (
    Guid etudeId, Guid id, IBienSupportRepository bienRepo, CancellationToken ct) =>
{
    var bienSupport = await bienRepo.ObtenirParIdAsync(id, ct);
    if (bienSupport is null || bienSupport.EtudeId != etudeId)
        return Results.NotFound(new { error = "Bien support introuvable pour cette étude." });

    await bienRepo.SupprimerAsync(bienSupport, ct);
    return Results.NoContent();
});

// --- Événements redoutés ---

app.MapPost("/api/v1/etudes/{etudeId:guid}/valeurs-metier/{valeurMetierId:guid}/evenements-redoutes", async (
    Guid etudeId, Guid valeurMetierId, CreerEvenementRedouteRequest request,
    IEtudeRepository etudeRepo, IValeurMetierRepository valeurRepo, IEvenementRedouteRepository erRepo, CancellationToken ct) =>
{
    var etude = await etudeRepo.ObtenirParIdAsync(etudeId, ct);
    if (etude is null)
        return Results.NotFound(new { error = "Étude introuvable." });

    var valeursExistantes = await valeurRepo.ListerParEtudeAsync(etudeId, ct);
    if (!valeursExistantes.Any(v => v.Id == valeurMetierId))
        return Results.NotFound(new { error = "Valeur métier introuvable pour cette étude." });

    try
    {
        var er = EvenementRedoute.Creer(etudeId, valeurMetierId, request.Description, request.Gravite);
        await erRepo.AjouterAsync(er, ct);
        return Results.Created($"/api/v1/etudes/{etudeId}/evenements-redoutes/{er.Id}", er);
    }
    catch (ArgumentOutOfRangeException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/v1/etudes/{etudeId:guid}/evenements-redoutes", async (
    Guid etudeId, IEvenementRedouteRepository erRepo, CancellationToken ct) =>
{
    var evenements = await erRepo.ListerParEtudeAsync(etudeId, ct);
    return Results.Ok(evenements);
});

app.MapPut("/api/v1/etudes/{etudeId:guid}/evenements-redoutes/{erId:guid}/gravite", async (
    Guid etudeId, Guid erId, RecoterGraviteRequest request,
    IEvenementRedouteRepository erRepo, CancellationToken ct) =>
{
    var er = await erRepo.ObtenirParIdAsync(erId, ct);
    if (er is null || er.EtudeId != etudeId)
        return Results.NotFound(new { error = "Événement redouté introuvable pour cette étude." });

    try
    {
        er.RecoterGravite(request.NouvelleGravite);
        await erRepo.MettreAJourAsync(er, ct);
        return Results.Ok(er);
    }
    catch (ArgumentOutOfRangeException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/v1/etudes/{etudeId:guid}/evenements-redoutes/{erId:guid}", async (
    Guid etudeId, Guid erId, CreerEvenementRedouteRequest request,
    IEvenementRedouteRepository erRepo, CancellationToken ct) =>
{
    var er = await erRepo.ObtenirParIdAsync(erId, ct);
    if (er is null || er.EtudeId != etudeId)
        return Results.NotFound(new { error = "Événement redouté introuvable pour cette étude." });

    try
    {
        er.ModifierDescription(request.Description);
        if (request.Gravite != er.Gravite)
            er.RecoterGravite(request.Gravite);
        await erRepo.MettreAJourAsync(er, ct);
        return Results.Ok(er);
    }
    catch (ArgumentOutOfRangeException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapDelete("/api/v1/etudes/{etudeId:guid}/evenements-redoutes/{erId:guid}", async (
    Guid etudeId, Guid erId, IEvenementRedouteRepository erRepo, CancellationToken ct) =>
{
    var er = await erRepo.ObtenirParIdAsync(erId, ct);
    if (er is null || er.EtudeId != etudeId)
        return Results.NotFound(new { error = "Événement redouté introuvable pour cette étude." });

    await erRepo.SupprimerAsync(er, ct);
    return Results.NoContent();
});

// --- Socle de sécurité ---

app.MapPost("/api/v1/etudes/{etudeId:guid}/socle-securite", async (
    Guid etudeId, IEtudeRepository etudeRepo, ISocleSecuriteRepository socleRepo, CancellationToken ct) =>
{
    var etude = await etudeRepo.ObtenirParIdAsync(etudeId, ct);
    if (etude is null)
        return Results.NotFound(new { error = "Étude introuvable." });

    var existant = await socleRepo.ObtenirParEtudeAsync(etudeId, ct);
    if (existant is not null)
        return Results.BadRequest(new { error = "Un socle de sécurité existe déjà pour cette étude." });

    try
    {
        var socle = SocleSecurite.Creer(etudeId);
        await socleRepo.AjouterAsync(socle, ct);
        return Results.Created($"/api/v1/etudes/{etudeId}/socle-securite", socle);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/v1/etudes/{etudeId:guid}/socle-securite/referentiels", async (
    Guid etudeId, AjouterReferentielRequest request,
    ISocleSecuriteRepository socleRepo, CancellationToken ct) =>
{
    var socle = await socleRepo.ObtenirParEtudeAsync(etudeId, ct);
    if (socle is null)
        return Results.NotFound(new { error = "Socle de sécurité introuvable pour cette étude. Créez-le d'abord." });

    if (!Enum.TryParse<EtatConformite>(request.Etat, ignoreCase: true, out var etat))
        return Results.BadRequest(new { error = $"État de conformité invalide. Valeurs autorisées : {string.Join(", ", Enum.GetNames<EtatConformite>())}" });

    try
    {
        socle.AjouterReferentiel(request.Nom, etat, request.Theme, request.CodeControle, request.EtatActuel);
        await socleRepo.MettreAJourAsync(socle, ct);
        return Results.Ok(socle);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/v1/etudes/{etudeId:guid}/socle-securite/referentiels/{referentielId:guid}", async (
    Guid etudeId, Guid referentielId, AjouterReferentielRequest request,
    ISocleSecuriteRepository socleRepo, CancellationToken ct) =>
{
    var socle = await socleRepo.ObtenirParEtudeAsync(etudeId, ct);
    if (socle is null)
        return Results.NotFound(new { error = "Socle de sécurité introuvable pour cette étude." });

    if (!Enum.TryParse<EtatConformite>(request.Etat, ignoreCase: true, out var etat))
        return Results.BadRequest(new { error = $"État de conformité invalide. Valeurs autorisées : {string.Join(", ", Enum.GetNames<EtatConformite>())}" });

    try
    {
        socle.ModifierReferentiel(referentielId, request.Nom, etat, request.Theme, request.CodeControle, request.EtatActuel);
        await socleRepo.MettreAJourAsync(socle, ct);
        return Results.Ok(socle);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapDelete("/api/v1/etudes/{etudeId:guid}/socle-securite/referentiels/{referentielId:guid}", async (
    Guid etudeId, Guid referentielId, ISocleSecuriteRepository socleRepo, CancellationToken ct) =>
{
    var socle = await socleRepo.ObtenirParEtudeAsync(etudeId, ct);
    if (socle is null)
        return Results.NotFound(new { error = "Socle de sécurité introuvable pour cette étude." });

    try
    {
        socle.SupprimerReferentiel(referentielId);
        await socleRepo.MettreAJourAsync(socle, ct);
        return Results.NoContent();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/v1/etudes/{etudeId:guid}/socle-securite", async (
    Guid etudeId, ISocleSecuriteRepository socleRepo, CancellationToken ct) =>
{
    var socle = await socleRepo.ObtenirParEtudeAsync(etudeId, ct);
    return socle is null ? Results.NotFound() : Results.Ok(socle);
});

// --- Reporting ---

app.MapGet("/api/v1/etudes/{etudeId:guid}/rapports/atelier1", async (
    Guid etudeId, RapportAtelier1Service rapportService, RapportAtelier1PdfGenerator pdfGenerator, CancellationToken ct) =>
{
    var data = await rapportService.ConstruireAsync(etudeId, ct);
    if (data is null)
        return Results.Conflict(new
        {
            error = "Aucun snapshot disponible pour l'atelier 1 de cette étude. L'atelier 1 doit être validé au moins une fois avant de générer un rapport."
        });

    var pdfBytes = pdfGenerator.Generer(data);
    return Results.File(pdfBytes, "application/pdf", $"rapport-atelier1-{etudeId}.pdf");
});



// --- Couples Source de Risque / Objectif Vise (Atelier 2) ---

app.MapPost("/api/v1/etudes/{etudeId:guid}/couples-sr-ov", async (
    Guid etudeId, CreerCoupleSrOvRequest request,
    IEtudeRepository etudeRepo, ICoupleSourceRisqueObjectifViseRepository coupleRepo, CancellationToken ct) =>
{
    var etude = await etudeRepo.ObtenirParIdAsync(etudeId, ct);
    if (etude is null)
        return Results.NotFound(new { error = "Étude introuvable." });

    if (!Enum.TryParse<CategorieSourceRisque>(request.SourceRisque, ignoreCase: true, out var sr))
        return Results.BadRequest(new { error = $"Catégorie de source de risque invalide. Valeurs autorisées : {string.Join(", ", Enum.GetNames<CategorieSourceRisque>())}" });

    if (!Enum.TryParse<CategorieObjectifVise>(request.ObjectifVise, ignoreCase: true, out var ov))
        return Results.BadRequest(new { error = $"Catégorie d objectif visé invalide. Valeurs autorisées : {string.Join(", ", Enum.GetNames<CategorieObjectifVise>())}" });

    try
    {
        var pertinenceCalculee = ServiceCalculPertinence.Calculer(request.Motivation, request.Ressources);
        var couple = CoupleSourceRisqueObjectifVise.Creer(
            etudeId, sr, request.DescriptionSourceRisque, ov, request.DescriptionObjectifVise,
            request.ContexteVulnerabilite, request.Theme, request.Motivation, request.Ressources, pertinenceCalculee);
        await coupleRepo.AjouterAsync(couple, ct);
        return Results.Created($"/api/v1/etudes/{etudeId}/couples-sr-ov/{couple.Id}", couple);
    }
    catch (ArgumentOutOfRangeException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/v1/etudes/{etudeId:guid}/couples-sr-ov", async (
    Guid etudeId, ICoupleSourceRisqueObjectifViseRepository coupleRepo, CancellationToken ct) =>
{
    var couples = await coupleRepo.ListerParEtudeAsync(etudeId, ct);
    return Results.Ok(couples);
});

app.MapPut("/api/v1/etudes/{etudeId:guid}/couples-sr-ov/{id:guid}", async (
    Guid etudeId, Guid id, CreerCoupleSrOvRequest request,
    ICoupleSourceRisqueObjectifViseRepository coupleRepo, CancellationToken ct) =>
{
    var couple = await coupleRepo.ObtenirParIdAsync(id, ct);
    if (couple is null || couple.EtudeId != etudeId)
        return Results.NotFound(new { error = "Couple SR/OV introuvable pour cette étude." });

    if (!Enum.TryParse<CategorieSourceRisque>(request.SourceRisque, ignoreCase: true, out var sr))
        return Results.BadRequest(new { error = $"Catégorie de source de risque invalide. Valeurs autorisées : {string.Join(", ", Enum.GetNames<CategorieSourceRisque>())}" });

    if (!Enum.TryParse<CategorieObjectifVise>(request.ObjectifVise, ignoreCase: true, out var ov))
        return Results.BadRequest(new { error = $"Catégorie d objectif visé invalide. Valeurs autorisées : {string.Join(", ", Enum.GetNames<CategorieObjectifVise>())}" });

    try
    {
        var pertinenceCalculee = ServiceCalculPertinence.Calculer(request.Motivation, request.Ressources);
        couple.Modifier(
            sr, request.DescriptionSourceRisque, ov, request.DescriptionObjectifVise,
            request.ContexteVulnerabilite, request.Theme, request.Motivation, request.Ressources, pertinenceCalculee);
        await coupleRepo.MettreAJourAsync(couple, ct);
        return Results.Ok(couple);
    }
    catch (ArgumentOutOfRangeException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/v1/etudes/{etudeId:guid}/couples-sr-ov/{id:guid}/pertinence-retenue", async (
    Guid etudeId, Guid id, DefinirPertinenceRetenueRequest request,
    ICoupleSourceRisqueObjectifViseRepository coupleRepo, CancellationToken ct) =>
{
    var couple = await coupleRepo.ObtenirParIdAsync(id, ct);
    if (couple is null || couple.EtudeId != etudeId)
        return Results.NotFound(new { error = "Couple SR/OV introuvable pour cette étude." });

    if (!Enum.TryParse<NiveauPertinence>(request.PertinenceRetenue, ignoreCase: true, out var pertinenceRetenue))
        return Results.BadRequest(new { error = $"Pertinence invalide. Valeurs autorisées : {string.Join(", ", Enum.GetNames<NiveauPertinence>())}" });

    try
    {
        couple.DefinirPertinenceRetenue(pertinenceRetenue, request.Justification);
        await coupleRepo.MettreAJourAsync(couple, ct);
        return Results.Ok(couple);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapDelete("/api/v1/etudes/{etudeId:guid}/couples-sr-ov/{id:guid}/pertinence-retenue", async (
    Guid etudeId, Guid id, ICoupleSourceRisqueObjectifViseRepository coupleRepo, CancellationToken ct) =>
{
    var couple = await coupleRepo.ObtenirParIdAsync(id, ct);
    if (couple is null || couple.EtudeId != etudeId)
        return Results.NotFound(new { error = "Couple SR/OV introuvable pour cette étude." });

    couple.ReinitialiserPertinence();
    await coupleRepo.MettreAJourAsync(couple, ct);
    return Results.Ok(couple);
});

app.MapDelete("/api/v1/etudes/{etudeId:guid}/couples-sr-ov/{id:guid}", async (
    Guid etudeId, Guid id, ICoupleSourceRisqueObjectifViseRepository coupleRepo,
    IScenarioStrategiqueRepository scenarioRepo, ICheminAttaqueRepository cheminRepo,
    IScenarioOperationnelRepository scenarioOpRepo, IScenarioDeRisqueRepository sdrRepo,
    IPlanTraitementRisqueRepository planRepo, CancellationToken ct) =>
{
    var couple = await coupleRepo.ObtenirParIdAsync(id, ct);
    if (couple is null || couple.EtudeId != etudeId)
        return Results.NotFound(new { error = "Couple SR/OV introuvable pour cette étude." });

    // Nettoyage en cascade manuel (pas de FK réelle entre agrégats, cf. suppression
    // de scénario stratégique ci-dessus) : un couple supprimé emporte son éventuel
    // scénario stratégique (relation 1:1), les chemins d'attaque de ce dernier, et
    // les scenarios operationnels et scenarios de risque 1:1 de chaque chemin.
    var scenario = await scenarioRepo.ObtenirParCoupleIdAsync(id, ct);
    if (scenario is not null)
    {
        var chemins = await cheminRepo.ListerParScenarioAsync(scenario.Id, ct);
        foreach (var chemin in chemins)
        {
            var scenarioOp = await scenarioOpRepo.ObtenirParCheminIdAsync(chemin.Id, ct);
            if (scenarioOp is not null)
                await scenarioOpRepo.SupprimerAsync(scenarioOp, ct);
            await SupprimerScenarioDeRisqueEtReferencesAsync(chemin.Id, etudeId, sdrRepo, planRepo, ct);
            await cheminRepo.SupprimerAsync(chemin, ct);
        }
        await scenarioRepo.SupprimerAsync(scenario, ct);
    }

    await coupleRepo.SupprimerAsync(couple, ct);
    return Results.NoContent();
});

// --- Reporting Atelier 2 ---

app.MapGet("/api/v1/etudes/{etudeId:guid}/rapports/atelier2", async (
    Guid etudeId, RapportAtelier2Service rapportService, RapportAtelier2PdfGenerator pdfGenerator, CancellationToken ct) =>
{
    var data = await rapportService.ConstruireAsync(etudeId, ct);
    if (data is null)
        return Results.NotFound(new { error = "Étude introuvable." });

    var pdfBytes = pdfGenerator.Generer(data);
    return Results.File(pdfBytes, "application/pdf", $"rapport-atelier2-{etudeId}.pdf");
});

// --- Reporting Atelier 3 ---

app.MapGet("/api/v1/etudes/{etudeId:guid}/rapports/atelier3", async (
    Guid etudeId, RapportAtelier3Service rapportService, RapportAtelier3PdfGenerator pdfGenerator, CancellationToken ct) =>
{
    var data = await rapportService.ConstruireAsync(etudeId, ct);
    if (data is null)
        return Results.NotFound(new { error = "Étude introuvable." });

    var pdfBytes = pdfGenerator.Generer(data);
    return Results.File(pdfBytes, "application/pdf", $"rapport-atelier3-{etudeId}.pdf");
});

// --- Cartographie graphique de l'Atelier 3 (radar ecosysteme + arbre des
// chemins d'attaque). Meme geometrie que le rapport PDF (CartographieSvg),
// exposee en SVG pour un rendu direct dans l'app. GET -> lecture, visible
// par tout membre.
app.MapGet("/api/v1/etudes/{etudeId:guid}/cartographie/ecosysteme.svg", async (
    Guid etudeId, bool? residuel, RapportAtelier3Service rapportService, CancellationToken ct) =>
{
    var data = await rapportService.ConstruireAsync(etudeId, ct);
    if (data is null) return Results.NotFound();

    var estResiduel = residuel == true;
    var parties = data.PartiesPrenantes
        .Select(p => new CartographieSvg.PartieRadar(
            p.Nom, p.LibelleCategorie,
            estResiduel ? p.NiveauDangerositeResiduel ?? p.NiveauDangerosite : p.NiveauDangerosite,
            estResiduel ? p.ZoneResiduelle ?? p.Zone : p.Zone))
        .ToList();

    return Results.Text(
        CartographieSvg.RadarEcosysteme(parties, estResiduel ? "après mesures (résiduelle)" : "initiale"),
        "image/svg+xml");
});

app.MapGet("/api/v1/etudes/{etudeId:guid}/cartographie/chemins-attaque.svg", async (
    Guid etudeId, RapportAtelier3Service rapportService, CancellationToken ct) =>
{
    var data = await rapportService.ConstruireAsync(etudeId, ct);
    if (data is null) return Results.NotFound();

    var scenarios = data.ScenariosStrategiques
        .Select(s => new CartographieSvg.ScenarioArbre(
            s.LibelleSourceRisque, s.LibelleObjectifVise, s.Description, s.Pertinence,
            s.LibelleEvenementRedoute, s.Gravite,
            s.CheminsAttaque
                .Select(c => new CartographieSvg.CheminArbre(
                    c.Description,
                    c.EvenementsIntermediaires.Select(e => e.LibellePartiePrenante).ToList()))
                .ToList()))
        .ToList();

    return Results.Text(CartographieSvg.ArbreCheminsAttaque(scenarios), "image/svg+xml");
});

// --- Reporting Atelier 4 ---

app.MapGet("/api/v1/etudes/{etudeId:guid}/rapports/atelier4", async (
    Guid etudeId, RapportAtelier4Service rapportService, RapportAtelier4PdfGenerator pdfGenerator, CancellationToken ct) =>
{
    var data = await rapportService.ConstruireAsync(etudeId, ct);
    if (data is null)
        return Results.NotFound(new { error = "Étude introuvable." });

    var pdfBytes = pdfGenerator.Generer(data);
    return Results.File(pdfBytes, "application/pdf", $"rapport-atelier4-{etudeId}.pdf");
});

// --- Reporting Atelier 5 ---

app.MapGet("/api/v1/etudes/{etudeId:guid}/rapports/atelier5", async (
    Guid etudeId, RapportAtelier5Service rapportService, RapportAtelier5PdfGenerator pdfGenerator, CancellationToken ct) =>
{
    var data = await rapportService.ConstruireAsync(etudeId, ct);
    if (data is null)
        return Results.NotFound(new { error = "Étude introuvable." });

    var pdfBytes = pdfGenerator.Generer(data);
    return Results.File(pdfBytes, "application/pdf", $"rapport-atelier5-{etudeId}.pdf");
});

// --- Reporting Synthese globale ---

app.MapGet("/api/v1/etudes/{etudeId:guid}/rapports/synthese", async (
    Guid etudeId, IEtudeRepository etudeRepo, RapportSyntheseGlobaleService rapportService, RapportSyntheseGlobalePdfGenerator pdfGenerator, CancellationToken ct) =>
{
    var etude = await etudeRepo.ObtenirParIdAsync(etudeId, ct);
    if (etude is null)
        return Results.NotFound(new { error = "Étude introuvable." });
    if (etude.StatutAtelier5 != StatutEtude.Validee)
        return Results.BadRequest(new { error = "La synthèse globale n'est disponible qu'une fois l'atelier 5 validé." });

    var data = await rapportService.ConstruireAsync(etudeId, ct);
    if (data is null)
        return Results.NotFound(new { error = "Un ou plusieurs snapshots d'atelier sont manquants pour cette étude." });

    var pdfBytes = pdfGenerator.Generer(data);
    return Results.File(pdfBytes, "application/pdf", $"rapport-synthese-{etudeId}.pdf");
});

app.MapGet("/api/v1/etudes/{etudeId:guid}/rapports/cadre-de-suivi", async (
    Guid etudeId, IEtudeRepository etudeRepo, RapportCadreDeSuiviService rapportService, RapportCadreDeSuiviPdfGenerator pdfGenerator, CancellationToken ct) =>
{
    var etude = await etudeRepo.ObtenirParIdAsync(etudeId, ct);
    if (etude is null)
        return Results.NotFound(new { error = "Étude introuvable." });
    if (etude.StatutAtelier5 == StatutEtude.Brouillon)
        return Results.BadRequest(new { error = "Le cadre de suivi n'est disponible qu'une fois l'atelier 5 démarré et un plan de traitement créé." });

    var data = await rapportService.ConstruireAsync(etudeId, ct);
    if (data is null)
        return Results.NotFound(new { error = "Aucun plan de traitement du risque n'existe pour cette étude." });

    var pdfBytes = pdfGenerator.Generer(data);
    return Results.File(pdfBytes, "application/pdf", $"cadre-de-suivi-{etudeId}.pdf");
});

// --- Parties Prenantes (Atelier 2) ---

app.MapPost("/api/v1/etudes/{etudeId:guid}/parties-prenantes", async (
    Guid etudeId, CreerPartiePrenanteRequest request,
    IEtudeRepository etudeRepo, IPartiePrenanteRepository ppRepo, CancellationToken ct) =>
{
    var etude = await etudeRepo.ObtenirParIdAsync(etudeId, ct);
    if (etude is null)
        return Results.NotFound(new { error = "Étude introuvable." });

    if (!Enum.TryParse<CategoriePartiePrenante>(request.Categorie, ignoreCase: true, out var categorie))
        return Results.BadRequest(new { error = $"Catégorie de partie prenante invalide. Valeurs autorisées : {string.Join(", ", Enum.GetNames<CategoriePartiePrenante>())}" });

    try
    {
        var pp = PartiePrenante.Creer(etudeId, request.Nom, request.RolesEtAttentes, request.Representant, categorie, request.DescriptionCategorie);
        await ppRepo.AjouterAsync(pp, ct);
        return Results.Created($"/api/v1/etudes/{etudeId}/parties-prenantes/{pp.Id}", pp);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/v1/etudes/{etudeId:guid}/parties-prenantes", async (
    Guid etudeId, IPartiePrenanteRepository ppRepo, CancellationToken ct) =>
{
    var parties = await ppRepo.ListerParEtudeAsync(etudeId, ct);
    return Results.Ok(parties);
});

app.MapPut("/api/v1/etudes/{etudeId:guid}/parties-prenantes/{id:guid}", async (
    Guid etudeId, Guid id, CreerPartiePrenanteRequest request,
    IPartiePrenanteRepository ppRepo, CancellationToken ct) =>
{
    var pp = await ppRepo.ObtenirParIdAsync(id, ct);
    if (pp is null || pp.EtudeId != etudeId)
        return Results.NotFound(new { error = "Partie prenante introuvable pour cette étude." });

    if (!Enum.TryParse<CategoriePartiePrenante>(request.Categorie, ignoreCase: true, out var categorie))
        return Results.BadRequest(new { error = $"Catégorie de partie prenante invalide. Valeurs autorisées : {string.Join(", ", Enum.GetNames<CategoriePartiePrenante>())}" });

    try
    {
        pp.Modifier(request.Nom, request.RolesEtAttentes, request.Representant, categorie, request.DescriptionCategorie);
        await ppRepo.MettreAJourAsync(pp, ct);
        return Results.Ok(pp);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapDelete("/api/v1/etudes/{etudeId:guid}/parties-prenantes/{id:guid}", async (
    Guid etudeId, Guid id, IPartiePrenanteRepository ppRepo, CancellationToken ct) =>
{
    var pp = await ppRepo.ObtenirParIdAsync(id, ct);
    if (pp is null || pp.EtudeId != etudeId)
        return Results.NotFound(new { error = "Partie prenante introuvable pour cette étude." });

    await ppRepo.SupprimerAsync(pp, ct);
    return Results.NoContent();
});

// --- Evaluation de la dangerosite (Atelier 3) ---
// "Dangerosite" est le terme officiel depuis EBIOS RM 1.5 (mars 2024,
// conformite ISO/CEI 27005:2022) -- remplace "Menace" partout (routes,
// records, methodes de domaine).

app.MapPut("/api/v1/etudes/{etudeId:guid}/parties-prenantes/{id:guid}/dangerosite", async (
    Guid etudeId, Guid id, EvaluerDangerositeRequest request,
    IPartiePrenanteRepository ppRepo, CancellationToken ct) =>
{
    var pp = await ppRepo.ObtenirParIdAsync(id, ct);
    if (pp is null || pp.EtudeId != etudeId)
        return Results.NotFound(new { error = "Partie prenante introuvable pour cette étude." });

    try
    {
        var niveauDangerositeCalculee = ServiceCalculNiveauDangerosite.Calculer(
            request.Dependance, request.Penetration, request.MaturiteCyber, request.Confiance);
        pp.EvaluerDangerosite(request.Dependance, request.Penetration, request.MaturiteCyber, request.Confiance, niveauDangerositeCalculee);
        await ppRepo.MettreAJourAsync(pp, ct);
        return Results.Ok(pp);
    }
    catch (ArgumentOutOfRangeException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/v1/etudes/{etudeId:guid}/parties-prenantes/{id:guid}/dangerosite-residuelle", async (
    Guid etudeId, Guid id, EvaluerDangerositeRequest request,
    IPartiePrenanteRepository ppRepo, CancellationToken ct) =>
{
    var pp = await ppRepo.ObtenirParIdAsync(id, ct);
    if (pp is null || pp.EtudeId != etudeId)
        return Results.NotFound(new { error = "Partie prenante introuvable pour cette étude." });

    try
    {
        var niveauDangerositeCalculee = ServiceCalculNiveauDangerosite.Calculer(
            request.Dependance, request.Penetration, request.MaturiteCyber, request.Confiance);
        pp.EvaluerDangerositeResiduelle(request.Dependance, request.Penetration, request.MaturiteCyber, request.Confiance, niveauDangerositeCalculee);
        await ppRepo.MettreAJourAsync(pp, ct);
        return Results.Ok(pp);
    }
    catch (ArgumentOutOfRangeException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/v1/etudes/{etudeId:guid}/parties-prenantes/{id:guid}/dangerosite-retenue", async (
    Guid etudeId, Guid id, DefinirDangerositeRetenueRequest request,
    IPartiePrenanteRepository ppRepo, CancellationToken ct) =>
{
    var pp = await ppRepo.ObtenirParIdAsync(id, ct);
    if (pp is null || pp.EtudeId != etudeId)
        return Results.NotFound(new { error = "Partie prenante introuvable pour cette étude." });

    try
    {
        pp.DefinirDangerositeRetenue(request.NiveauRetenu, request.Justification);
        await ppRepo.MettreAJourAsync(pp, ct);
        return Results.Ok(pp);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapDelete("/api/v1/etudes/{etudeId:guid}/parties-prenantes/{id:guid}/dangerosite-retenue", async (
    Guid etudeId, Guid id, IPartiePrenanteRepository ppRepo, CancellationToken ct) =>
{
    var pp = await ppRepo.ObtenirParIdAsync(id, ct);
    if (pp is null || pp.EtudeId != etudeId)
        return Results.NotFound(new { error = "Partie prenante introuvable pour cette étude." });

    pp.ReinitialiserDangerosite();
    await ppRepo.MettreAJourAsync(pp, ct);
    return Results.Ok(pp);
});

app.MapPut("/api/v1/etudes/{etudeId:guid}/parties-prenantes/{id:guid}/dangerosite-residuelle-retenue", async (
    Guid etudeId, Guid id, DefinirDangerositeRetenueRequest request,
    IPartiePrenanteRepository ppRepo, CancellationToken ct) =>
{
    var pp = await ppRepo.ObtenirParIdAsync(id, ct);
    if (pp is null || pp.EtudeId != etudeId)
        return Results.NotFound(new { error = "Partie prenante introuvable pour cette étude." });

    try
    {
        pp.DefinirDangerositeResiduelleRetenue(request.NiveauRetenu, request.Justification);
        await ppRepo.MettreAJourAsync(pp, ct);
        return Results.Ok(pp);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapDelete("/api/v1/etudes/{etudeId:guid}/parties-prenantes/{id:guid}/dangerosite-residuelle-retenue", async (
    Guid etudeId, Guid id, IPartiePrenanteRepository ppRepo, CancellationToken ct) =>
{
    var pp = await ppRepo.ObtenirParIdAsync(id, ct);
    if (pp is null || pp.EtudeId != etudeId)
        return Results.NotFound(new { error = "Partie prenante introuvable pour cette étude." });

    pp.ReinitialiserDangerositeResiduelle();
    await ppRepo.MettreAJourAsync(pp, ct);
    return Results.Ok(pp);
});

app.MapPost("/api/v1/etudes/{etudeId:guid}/parties-prenantes/{id:guid}/mesures", async (
    Guid etudeId, Guid id, MesureEcosystemeRequest request,
    IPartiePrenanteRepository ppRepo, CancellationToken ct) =>
{
    var pp = await ppRepo.ObtenirParIdAsync(id, ct);
    if (pp is null || pp.EtudeId != etudeId)
        return Results.NotFound(new { error = "Partie prenante introuvable pour cette étude." });

    try
    {
        pp.AjouterMesure(request.Description);
        await ppRepo.MettreAJourAsync(pp, ct);
        return Results.Created($"/api/v1/etudes/{etudeId}/parties-prenantes/{id}/mesures", pp);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/v1/etudes/{etudeId:guid}/parties-prenantes/{id:guid}/mesures/{mesureId:guid}", async (
    Guid etudeId, Guid id, Guid mesureId, MesureEcosystemeRequest request,
    IPartiePrenanteRepository ppRepo, CancellationToken ct) =>
{
    var pp = await ppRepo.ObtenirParIdAsync(id, ct);
    if (pp is null || pp.EtudeId != etudeId)
        return Results.NotFound(new { error = "Partie prenante introuvable pour cette étude." });

    try
    {
        pp.ModifierMesure(mesureId, request.Description);
        await ppRepo.MettreAJourAsync(pp, ct);
        return Results.Ok(pp);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapDelete("/api/v1/etudes/{etudeId:guid}/parties-prenantes/{id:guid}/mesures/{mesureId:guid}", async (
    Guid etudeId, Guid id, Guid mesureId,
    IPartiePrenanteRepository ppRepo, CancellationToken ct) =>
{
    var pp = await ppRepo.ObtenirParIdAsync(id, ct);
    if (pp is null || pp.EtudeId != etudeId)
        return Results.NotFound(new { error = "Partie prenante introuvable pour cette étude." });

    try
    {
        pp.SupprimerMesure(mesureId);
        await ppRepo.MettreAJourAsync(pp, ct);
        return Results.NoContent();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// --- Scenarios strategiques (Atelier 3) ---

app.MapPost("/api/v1/etudes/{etudeId:guid}/couples-sr-ov/{coupleId:guid}/scenario-strategique", async (
    Guid etudeId, Guid coupleId, CreerScenarioStrategiqueRequest request,
    ICoupleSourceRisqueObjectifViseRepository coupleRepo, IScenarioStrategiqueRepository scenarioRepo,
    IEvenementRedouteRepository erRepo, CancellationToken ct) =>
{
    var couple = await coupleRepo.ObtenirParIdAsync(coupleId, ct);
    if (couple is null || couple.EtudeId != etudeId)
        return Results.NotFound(new { error = "Couple source de risque / objectif visé introuvable pour cette étude." });

    if (couple.Pertinence is not (NiveauPertinence.TresPertinent or NiveauPertinence.PlutotPertinent))
        return Results.BadRequest(new { error = "Seul un couple retenu (pertinence 'Très pertinent' ou 'Plutôt pertinent') peut donner lieu à un scénario stratégique." });

    var existant = await scenarioRepo.ObtenirParCoupleIdAsync(coupleId, ct);
    if (existant is not null)
        return Results.BadRequest(new { error = "Ce couple a déjà un scénario stratégique (relation 1:1)." });

    var er = await erRepo.ObtenirParIdAsync(request.EvenementRedouteId, ct);
    if (er is null || er.EtudeId != etudeId)
        return Results.BadRequest(new { error = "Événement redouté introuvable pour cette étude." });

    try
    {
        var scenario = ScenarioStrategique.Creer(etudeId, coupleId, request.EvenementRedouteId, request.Description);
        await scenarioRepo.AjouterAsync(scenario, ct);
        return Results.Created($"/api/v1/etudes/{etudeId}/scenarios-strategiques/{scenario.Id}", scenario);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/v1/etudes/{etudeId:guid}/scenarios-strategiques", async (
    Guid etudeId, IScenarioStrategiqueRepository scenarioRepo, CancellationToken ct) =>
{
    var scenarios = await scenarioRepo.ListerParEtudeAsync(etudeId, ct);
    return Results.Ok(scenarios);
});

app.MapPut("/api/v1/etudes/{etudeId:guid}/scenarios-strategiques/{id:guid}", async (
    Guid etudeId, Guid id, ModifierScenarioStrategiqueRequest request,
    IScenarioStrategiqueRepository scenarioRepo, IEvenementRedouteRepository erRepo, CancellationToken ct) =>
{
    var scenario = await scenarioRepo.ObtenirParIdAsync(id, ct);
    if (scenario is null || scenario.EtudeId != etudeId)
        return Results.NotFound(new { error = "Scénario stratégique introuvable pour cette étude." });

    var er = await erRepo.ObtenirParIdAsync(request.EvenementRedouteId, ct);
    if (er is null || er.EtudeId != etudeId)
        return Results.BadRequest(new { error = "Événement redouté introuvable pour cette étude." });

    try
    {
        scenario.Modifier(request.EvenementRedouteId, request.Description);
        await scenarioRepo.MettreAJourAsync(scenario, ct);
        return Results.Ok(scenario);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapDelete("/api/v1/etudes/{etudeId:guid}/scenarios-strategiques/{id:guid}", async (
    Guid etudeId, Guid id, IScenarioStrategiqueRepository scenarioRepo, ICheminAttaqueRepository cheminRepo,
    IScenarioOperationnelRepository scenarioOpRepo, IScenarioDeRisqueRepository sdrRepo,
    IPlanTraitementRisqueRepository planRepo, CancellationToken ct) =>
{
    var scenario = await scenarioRepo.ObtenirParIdAsync(id, ct);
    if (scenario is null || scenario.EtudeId != etudeId)
        return Results.NotFound(new { error = "Scénario stratégique introuvable pour cette étude." });

    // Pas de contrainte FK entre ScenarioStrategique et CheminAttaque (agrégats
    // séparés, référencés par Id seulement -- cohérent avec le reste du projet).
    // Nettoyage explicite requis ici pour éviter des chemins d'attaque, scenarios
    // operationnels et scenarios de risque orphelins.
    var cheminsOrphelins = await cheminRepo.ListerParScenarioAsync(id, ct);
    foreach (var chemin in cheminsOrphelins)
    {
        var scenarioOp = await scenarioOpRepo.ObtenirParCheminIdAsync(chemin.Id, ct);
        if (scenarioOp is not null)
            await scenarioOpRepo.SupprimerAsync(scenarioOp, ct);
        await SupprimerScenarioDeRisqueEtReferencesAsync(chemin.Id, etudeId, sdrRepo, planRepo, ct);
        await cheminRepo.SupprimerAsync(chemin, ct);
    }

    await scenarioRepo.SupprimerAsync(scenario, ct);
    return Results.NoContent();
});

// --- Chemins d'attaque et evenements intermediaires (Atelier 3) ---

app.MapPost("/api/v1/etudes/{etudeId:guid}/scenarios-strategiques/{scenarioId:guid}/chemins-attaque", async (
    Guid etudeId, Guid scenarioId, CreerCheminAttaqueRequest request,
    IScenarioStrategiqueRepository scenarioRepo, ICheminAttaqueRepository cheminRepo, CancellationToken ct) =>
{
    var scenario = await scenarioRepo.ObtenirParIdAsync(scenarioId, ct);
    if (scenario is null || scenario.EtudeId != etudeId)
        return Results.NotFound(new { error = "Scénario stratégique introuvable pour cette étude." });

    try
    {
        var chemin = CheminAttaque.Creer(etudeId, scenarioId, request.Description);
        await cheminRepo.AjouterAsync(chemin, ct);
        return Results.Created($"/api/v1/etudes/{etudeId}/chemins-attaque/{chemin.Id}", chemin);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/v1/etudes/{etudeId:guid}/chemins-attaque", async (
    Guid etudeId, ICheminAttaqueRepository cheminRepo, CancellationToken ct) =>
{
    var chemins = await cheminRepo.ListerParEtudeAsync(etudeId, ct);
    return Results.Ok(chemins);
});

app.MapPut("/api/v1/etudes/{etudeId:guid}/chemins-attaque/{id:guid}", async (
    Guid etudeId, Guid id, ModifierCheminAttaqueRequest request,
    ICheminAttaqueRepository cheminRepo, CancellationToken ct) =>
{
    var chemin = await cheminRepo.ObtenirParIdAsync(id, ct);
    if (chemin is null || chemin.EtudeId != etudeId)
        return Results.NotFound(new { error = "Chemin d'attaque introuvable pour cette étude." });

    try
    {
        chemin.Modifier(request.Description);
        await cheminRepo.MettreAJourAsync(chemin, ct);
        return Results.Ok(chemin);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapDelete("/api/v1/etudes/{etudeId:guid}/chemins-attaque/{id:guid}", async (
    Guid etudeId, Guid id, ICheminAttaqueRepository cheminRepo, IScenarioOperationnelRepository scenarioOpRepo,
    IScenarioDeRisqueRepository sdrRepo, IPlanTraitementRisqueRepository planRepo, CancellationToken ct) =>
{
    var chemin = await cheminRepo.ObtenirParIdAsync(id, ct);
    if (chemin is null || chemin.EtudeId != etudeId)
        return Results.NotFound(new { error = "Chemin d'attaque introuvable pour cette étude." });

    // Cascade manuelle (pas de FK reelle entre agregats) : le scenario
    // operationnel et le scenario de risque 1:1 de ce chemin doivent
    // disparaitre avec lui.
    var scenarioOp = await scenarioOpRepo.ObtenirParCheminIdAsync(id, ct);
    if (scenarioOp is not null)
        await scenarioOpRepo.SupprimerAsync(scenarioOp, ct);
    await SupprimerScenarioDeRisqueEtReferencesAsync(id, etudeId, sdrRepo, planRepo, ct);

    await cheminRepo.SupprimerAsync(chemin, ct);
    return Results.NoContent();
});

app.MapPost("/api/v1/etudes/{etudeId:guid}/chemins-attaque/{cheminId:guid}/evenements-intermediaires", async (
    Guid etudeId, Guid cheminId, CreerEvenementIntermediaireRequest request,
    ICheminAttaqueRepository cheminRepo, IPartiePrenanteRepository ppRepo, CancellationToken ct) =>
{
    var chemin = await cheminRepo.ObtenirParIdAsync(cheminId, ct);
    if (chemin is null || chemin.EtudeId != etudeId)
        return Results.NotFound(new { error = "Chemin d'attaque introuvable pour cette étude." });

    var pp = await ppRepo.ObtenirParIdAsync(request.PartiePrenanteId, ct);
    if (pp is null || pp.EtudeId != etudeId)
        return Results.BadRequest(new { error = "Partie prenante introuvable pour cette étude." });

    try
    {
        chemin.AjouterEvenementIntermediaire(request.PartiePrenanteId, request.Description);
        await cheminRepo.MettreAJourAsync(chemin, ct);
        return Results.Created($"/api/v1/etudes/{etudeId}/chemins-attaque/{cheminId}", chemin);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/v1/etudes/{etudeId:guid}/chemins-attaque/{cheminId:guid}/evenements-intermediaires/{eiId:guid}", async (
    Guid etudeId, Guid cheminId, Guid eiId, ModifierEvenementIntermediaireRequest request,
    ICheminAttaqueRepository cheminRepo, CancellationToken ct) =>
{
    var chemin = await cheminRepo.ObtenirParIdAsync(cheminId, ct);
    if (chemin is null || chemin.EtudeId != etudeId)
        return Results.NotFound(new { error = "Chemin d'attaque introuvable pour cette étude." });

    try
    {
        chemin.ModifierEvenementIntermediaire(eiId, request.Description);
        await cheminRepo.MettreAJourAsync(chemin, ct);
        return Results.Ok(chemin);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapDelete("/api/v1/etudes/{etudeId:guid}/chemins-attaque/{cheminId:guid}/evenements-intermediaires/{eiId:guid}", async (
    Guid etudeId, Guid cheminId, Guid eiId, ICheminAttaqueRepository cheminRepo, CancellationToken ct) =>
{
    var chemin = await cheminRepo.ObtenirParIdAsync(cheminId, ct);
    if (chemin is null || chemin.EtudeId != etudeId)
        return Results.NotFound(new { error = "Chemin d'attaque introuvable pour cette étude." });

    try
    {
        chemin.SupprimerEvenementIntermediaire(eiId);
        await cheminRepo.MettreAJourAsync(chemin, ct);
        return Results.NoContent();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// --- Scenarios operationnels et modes operatoires (Atelier 4) ---

app.MapPost("/api/v1/etudes/{etudeId:guid}/chemins-attaque/{cheminId:guid}/scenario-operationnel", async (
    Guid etudeId, Guid cheminId, ICheminAttaqueRepository cheminRepo, IScenarioOperationnelRepository scenarioOpRepo, CancellationToken ct) =>
{
    var chemin = await cheminRepo.ObtenirParIdAsync(cheminId, ct);
    if (chemin is null || chemin.EtudeId != etudeId)
        return Results.NotFound(new { error = "Chemin d'attaque introuvable pour cette étude." });

    var existant = await scenarioOpRepo.ObtenirParCheminIdAsync(cheminId, ct);
    if (existant is not null)
        return Results.BadRequest(new { error = "Ce chemin d'attaque a déjà un scénario opérationnel (relation 1:1)." });

    try
    {
        var scenarioOp = ScenarioOperationnel.Creer(etudeId, cheminId);
        await scenarioOpRepo.AjouterAsync(scenarioOp, ct);
        return Results.Created($"/api/v1/etudes/{etudeId}/scenarios-operationnels/{scenarioOp.Id}", scenarioOp);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/v1/etudes/{etudeId:guid}/scenarios-operationnels", async (
    Guid etudeId, IScenarioOperationnelRepository scenarioOpRepo, CancellationToken ct) =>
{
    var scenarios = await scenarioOpRepo.ListerParEtudeAsync(etudeId, ct);
    return Results.Ok(scenarios);
});

app.MapDelete("/api/v1/etudes/{etudeId:guid}/scenarios-operationnels/{id:guid}", async (
    Guid etudeId, Guid id, IScenarioOperationnelRepository scenarioOpRepo,
    IScenarioDeRisqueRepository sdrRepo, IPlanTraitementRisqueRepository planRepo, CancellationToken ct) =>
{
    var scenarioOp = await scenarioOpRepo.ObtenirParIdAsync(id, ct);
    if (scenarioOp is null || scenarioOp.EtudeId != etudeId)
        return Results.NotFound(new { error = "Scénario opérationnel introuvable pour cette étude." });

    // Sans ce scénario opérationnel, plus de Vraisemblance disponible : le
    // scénario de risque 1:1 de ce chemin devient incalculable, donc invalide.
    await SupprimerScenarioDeRisqueEtReferencesAsync(scenarioOp.CheminAttaqueId, etudeId, sdrRepo, planRepo, ct);

    await scenarioOpRepo.SupprimerAsync(scenarioOp, ct);
    return Results.NoContent();
});

app.MapPost("/api/v1/etudes/{etudeId:guid}/scenarios-operationnels/{id:guid}/modes-operatoires", async (
    Guid etudeId, Guid id, ModeOperatoireRequest request, IScenarioOperationnelRepository scenarioOpRepo,
    IBienSupportRepository bienRepo, CancellationToken ct) =>
{
    var scenarioOp = await scenarioOpRepo.ObtenirParIdAsync(id, ct);
    if (scenarioOp is null || scenarioOp.EtudeId != etudeId)
        return Results.NotFound(new { error = "Scénario opérationnel introuvable pour cette étude." });

    var (actions, erreurActions) = await ParseActionsElementairesAsync(etudeId, request.Actions, bienRepo, ct);
    if (erreurActions is not null)
        return erreurActions;

    try
    {
        scenarioOp.AjouterModeOperatoire(request.Description, actions!, request.ProbabiliteSucces, request.DifficulteTechnique);
        await scenarioOpRepo.MettreAJourAsync(scenarioOp, ct);
        return Results.Created($"/api/v1/etudes/{etudeId}/scenarios-operationnels/{id}/modes-operatoires", scenarioOp);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/v1/etudes/{etudeId:guid}/scenarios-operationnels/{id:guid}/modes-operatoires/{modeId:guid}", async (
    Guid etudeId, Guid id, Guid modeId, ModeOperatoireRequest request, IScenarioOperationnelRepository scenarioOpRepo,
    IBienSupportRepository bienRepo, CancellationToken ct) =>
{
    var scenarioOp = await scenarioOpRepo.ObtenirParIdAsync(id, ct);
    if (scenarioOp is null || scenarioOp.EtudeId != etudeId)
        return Results.NotFound(new { error = "Scénario opérationnel introuvable pour cette étude." });

    var (actions, erreurActions) = await ParseActionsElementairesAsync(etudeId, request.Actions, bienRepo, ct);
    if (erreurActions is not null)
        return erreurActions;

    try
    {
        scenarioOp.ModifierModeOperatoire(modeId, request.Description, actions!, request.ProbabiliteSucces, request.DifficulteTechnique);
        await scenarioOpRepo.MettreAJourAsync(scenarioOp, ct);
        return Results.Ok(scenarioOp);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapDelete("/api/v1/etudes/{etudeId:guid}/scenarios-operationnels/{id:guid}/modes-operatoires/{modeId:guid}", async (
    Guid etudeId, Guid id, Guid modeId, IScenarioOperationnelRepository scenarioOpRepo, CancellationToken ct) =>
{
    var scenarioOp = await scenarioOpRepo.ObtenirParIdAsync(id, ct);
    if (scenarioOp is null || scenarioOp.EtudeId != etudeId)
        return Results.NotFound(new { error = "Scénario opérationnel introuvable pour cette étude." });

    try
    {
        scenarioOp.SupprimerModeOperatoire(modeId);
        await scenarioOpRepo.MettreAJourAsync(scenarioOp, ct);
        return Results.NoContent();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/v1/etudes/{etudeId:guid}/scenarios-operationnels/{id:guid}/modes-operatoires/{modeId:guid}/vraisemblance-retenue", async (
    Guid etudeId, Guid id, Guid modeId, DefinirVraisemblanceRetenueRequest request,
    IScenarioOperationnelRepository scenarioOpRepo, CancellationToken ct) =>
{
    var scenarioOp = await scenarioOpRepo.ObtenirParIdAsync(id, ct);
    if (scenarioOp is null || scenarioOp.EtudeId != etudeId)
        return Results.NotFound(new { error = "Scénario opérationnel introuvable pour cette étude." });

    if (!Enum.TryParse<NiveauVraisemblance>(request.VraisemblanceRetenue, ignoreCase: true, out var vraisemblanceRetenue))
        return Results.BadRequest(new { error = $"Vraisemblance invalide. Valeurs autorisées : {string.Join(", ", Enum.GetNames<NiveauVraisemblance>())}" });

    try
    {
        scenarioOp.DefinirVraisemblanceRetenueModeOperatoire(modeId, vraisemblanceRetenue, request.Justification);
        await scenarioOpRepo.MettreAJourAsync(scenarioOp, ct);
        return Results.Ok(scenarioOp);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapDelete("/api/v1/etudes/{etudeId:guid}/scenarios-operationnels/{id:guid}/modes-operatoires/{modeId:guid}/vraisemblance-retenue", async (
    Guid etudeId, Guid id, Guid modeId, IScenarioOperationnelRepository scenarioOpRepo, CancellationToken ct) =>
{
    var scenarioOp = await scenarioOpRepo.ObtenirParIdAsync(id, ct);
    if (scenarioOp is null || scenarioOp.EtudeId != etudeId)
        return Results.NotFound(new { error = "Scénario opérationnel introuvable pour cette étude." });

    try
    {
        scenarioOp.ReinitialiserVraisemblanceModeOperatoire(modeId);
        await scenarioOpRepo.MettreAJourAsync(scenarioOp, ct);
        return Results.Ok(scenarioOp);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// --- Scenarios de risque (Atelier 5) ---

app.MapPost("/api/v1/etudes/{etudeId:guid}/chemins-attaque/{cheminId:guid}/scenario-de-risque", async (
    Guid etudeId, Guid cheminId, ICheminAttaqueRepository cheminRepo, IScenarioOperationnelRepository scenarioOpRepo,
    IScenarioDeRisqueRepository sdrRepo, CancellationToken ct) =>
{
    var chemin = await cheminRepo.ObtenirParIdAsync(cheminId, ct);
    if (chemin is null || chemin.EtudeId != etudeId)
        return Results.NotFound(new { error = "Chemin d'attaque introuvable pour cette étude." });

    var scenarioOp = await scenarioOpRepo.ObtenirParCheminIdAsync(cheminId, ct);
    if (scenarioOp is null)
        return Results.BadRequest(new { error = "Ce chemin d'attaque doit avoir un scénario opérationnel avant de matérialiser son scénario de risque." });

    var existant = await sdrRepo.ObtenirParCheminIdAsync(cheminId, ct);
    if (existant is not null)
        return Results.BadRequest(new { error = "Ce chemin d'attaque a déjà un scénario de risque (relation 1:1)." });

    try
    {
        var scenarioDeRisque = ScenarioDeRisque.Creer(etudeId, cheminId);
        await sdrRepo.AjouterAsync(scenarioDeRisque, ct);
        return Results.Created($"/api/v1/etudes/{etudeId}/scenarios-de-risque/{scenarioDeRisque.Id}", scenarioDeRisque);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/v1/etudes/{etudeId:guid}/scenarios-de-risque", async (
    Guid etudeId, ServiceAssemblageScenariosDeRisque serviceAssemblage, CancellationToken ct) =>
{
    var vues = await serviceAssemblage.ListerAsync(etudeId, ct);
    return Results.Ok(vues);
});

app.MapDelete("/api/v1/etudes/{etudeId:guid}/scenarios-de-risque/{id:guid}", async (
    Guid etudeId, Guid id, IScenarioDeRisqueRepository sdrRepo, IPlanTraitementRisqueRepository planRepo, CancellationToken ct) =>
{
    var sdr = await sdrRepo.ObtenirParIdAsync(id, ct);
    if (sdr is null || sdr.EtudeId != etudeId)
        return Results.NotFound(new { error = "Scénario de risque introuvable pour cette étude." });

    var plan = await planRepo.ObtenirParEtudeAsync(etudeId, ct);
    if (plan is not null)
    {
        plan.RetirerReferenceScenario(sdr.Id);
        await planRepo.MettreAJourAsync(plan, ct);
    }

    await sdrRepo.SupprimerAsync(sdr, ct);
    return Results.NoContent();
});

app.MapPut("/api/v1/etudes/{etudeId:guid}/scenarios-de-risque/{id:guid}/niveau-risque-initial-retenue", async (
    Guid etudeId, Guid id, DefinirNiveauRisqueRetenuRequest request, IScenarioDeRisqueRepository sdrRepo, CancellationToken ct) =>
{
    var sdr = await sdrRepo.ObtenirParIdAsync(id, ct);
    if (sdr is null || sdr.EtudeId != etudeId)
        return Results.NotFound(new { error = "Scénario de risque introuvable pour cette étude." });

    if (!Enum.TryParse<NiveauRisque>(request.NiveauRetenu, ignoreCase: true, out var niveauRetenu))
        return Results.BadRequest(new { error = $"Niveau de risque invalide. Valeurs autorisées : {string.Join(", ", Enum.GetNames<NiveauRisque>())}" });

    try
    {
        sdr.DefinirNiveauRisqueInitialRetenu(niveauRetenu, request.Justification);
        await sdrRepo.MettreAJourAsync(sdr, ct);
        return Results.Ok(sdr);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapDelete("/api/v1/etudes/{etudeId:guid}/scenarios-de-risque/{id:guid}/niveau-risque-initial-retenue", async (
    Guid etudeId, Guid id, IScenarioDeRisqueRepository sdrRepo, CancellationToken ct) =>
{
    var sdr = await sdrRepo.ObtenirParIdAsync(id, ct);
    if (sdr is null || sdr.EtudeId != etudeId)
        return Results.NotFound(new { error = "Scénario de risque introuvable pour cette étude." });

    sdr.ReinitialiserNiveauRisqueInitial();
    await sdrRepo.MettreAJourAsync(sdr, ct);
    return Results.Ok(sdr);
});

app.MapPut("/api/v1/etudes/{etudeId:guid}/scenarios-de-risque/{id:guid}/risque-residuel", async (
    Guid etudeId, Guid id, EvaluerRisqueResiduelRequest request, IScenarioDeRisqueRepository sdrRepo, CancellationToken ct) =>
{
    var sdr = await sdrRepo.ObtenirParIdAsync(id, ct);
    if (sdr is null || sdr.EtudeId != etudeId)
        return Results.NotFound(new { error = "Scénario de risque introuvable pour cette étude." });

    if (!Enum.TryParse<NiveauVraisemblance>(request.VraisemblanceResiduelle, ignoreCase: true, out var vraisemblanceResiduelle))
        return Results.BadRequest(new { error = $"Vraisemblance invalide. Valeurs autorisées : {string.Join(", ", Enum.GetNames<NiveauVraisemblance>())}" });

    try
    {
        var niveauCalcule = ServiceCalculNiveauRisque.Calculer(request.GraviteResiduelle, vraisemblanceResiduelle);
        sdr.EvaluerRisqueResiduel(request.GraviteResiduelle, vraisemblanceResiduelle, niveauCalcule);
        await sdrRepo.MettreAJourAsync(sdr, ct);
        return Results.Ok(sdr);
    }
    catch (ArgumentOutOfRangeException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/v1/etudes/{etudeId:guid}/scenarios-de-risque/{id:guid}/niveau-risque-residuel-retenue", async (
    Guid etudeId, Guid id, DefinirNiveauRisqueRetenuRequest request, IScenarioDeRisqueRepository sdrRepo, CancellationToken ct) =>
{
    var sdr = await sdrRepo.ObtenirParIdAsync(id, ct);
    if (sdr is null || sdr.EtudeId != etudeId)
        return Results.NotFound(new { error = "Scénario de risque introuvable pour cette étude." });

    if (!Enum.TryParse<NiveauRisque>(request.NiveauRetenu, ignoreCase: true, out var niveauRetenu))
        return Results.BadRequest(new { error = $"Niveau de risque invalide. Valeurs autorisées : {string.Join(", ", Enum.GetNames<NiveauRisque>())}" });

    try
    {
        sdr.DefinirNiveauRisqueResiduelRetenu(niveauRetenu, request.Justification);
        await sdrRepo.MettreAJourAsync(sdr, ct);
        return Results.Ok(sdr);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapDelete("/api/v1/etudes/{etudeId:guid}/scenarios-de-risque/{id:guid}/niveau-risque-residuel-retenue", async (
    Guid etudeId, Guid id, IScenarioDeRisqueRepository sdrRepo, CancellationToken ct) =>
{
    var sdr = await sdrRepo.ObtenirParIdAsync(id, ct);
    if (sdr is null || sdr.EtudeId != etudeId)
        return Results.NotFound(new { error = "Scénario de risque introuvable pour cette étude." });

    sdr.ReinitialiserNiveauRisqueResiduel();
    await sdrRepo.MettreAJourAsync(sdr, ct);
    return Results.Ok(sdr);
});

app.MapPost("/api/v1/etudes/{etudeId:guid}/scenarios-de-risque/{id:guid}/acceptation", async (
    Guid etudeId, Guid id, AccepterRisqueResiduelRequest request, IScenarioDeRisqueRepository sdrRepo, CancellationToken ct) =>
{
    var sdr = await sdrRepo.ObtenirParIdAsync(id, ct);
    if (sdr is null || sdr.EtudeId != etudeId)
        return Results.NotFound(new { error = "Scénario de risque introuvable pour cette étude." });

    try
    {
        sdr.AccepterRisqueResiduel(request.NomProprietaireRisque, request.NomValidateurSecurite, request.NomSponsorExecutif, request.Justification);
        await sdrRepo.MettreAJourAsync(sdr, ct);
        return Results.Ok(sdr);
    }
    catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapDelete("/api/v1/etudes/{etudeId:guid}/scenarios-de-risque/{id:guid}/acceptation", async (
    Guid etudeId, Guid id, IScenarioDeRisqueRepository sdrRepo, CancellationToken ct) =>
{
    var sdr = await sdrRepo.ObtenirParIdAsync(id, ct);
    if (sdr is null || sdr.EtudeId != etudeId)
        return Results.NotFound(new { error = "Scénario de risque introuvable pour cette étude." });

    sdr.RetirerAcceptation();
    await sdrRepo.MettreAJourAsync(sdr, ct);
    return Results.Ok(sdr);
});

// --- Plan de traitement du risque (Atelier 5) ---

app.MapPost("/api/v1/etudes/{etudeId:guid}/plan-traitement-risque", async (
    Guid etudeId, IEtudeRepository etudeRepo, IPlanTraitementRisqueRepository planRepo, CancellationToken ct) =>
{
    var etude = await etudeRepo.ObtenirParIdAsync(etudeId, ct);
    if (etude is null)
        return Results.NotFound(new { error = "Étude introuvable." });

    var existant = await planRepo.ObtenirParEtudeAsync(etudeId, ct);
    if (existant is not null)
        return Results.BadRequest(new { error = "Cette étude a déjà un plan de traitement du risque." });

    var plan = PlanTraitementRisque.Creer(etudeId);
    await planRepo.AjouterAsync(plan, ct);
    return Results.Created($"/api/v1/etudes/{etudeId}/plan-traitement-risque", plan);
});

app.MapGet("/api/v1/etudes/{etudeId:guid}/plan-traitement-risque", async (
    Guid etudeId, IPlanTraitementRisqueRepository planRepo, CancellationToken ct) =>
{
    var plan = await planRepo.ObtenirParEtudeAsync(etudeId, ct);
    if (plan is null)
        return Results.NotFound(new { error = "Aucun plan de traitement du risque pour cette étude." });
    return Results.Ok(plan);
});

app.MapPost("/api/v1/etudes/{etudeId:guid}/plan-traitement-risque/mesures", async (
    Guid etudeId, MesureTraitementRisqueRequest request, IPlanTraitementRisqueRepository planRepo,
    IScenarioDeRisqueRepository sdrRepo, CancellationToken ct) =>
{
    var plan = await planRepo.ObtenirParEtudeAsync(etudeId, ct);
    if (plan is null)
        return Results.NotFound(new { error = "Aucun plan de traitement du risque pour cette étude." });

    var (erreurValidation, axe, coutComplexite, statut) = await ValiderMesureTraitementRisqueAsync(etudeId, request, sdrRepo, ct);
    if (erreurValidation is not null)
        return erreurValidation;

    try
    {
        plan.AjouterMesure(request.Description, axe, request.ScenariosDeRisqueIds, request.Responsable, request.FreinsEtDifficultes, coutComplexite, request.Echeance, statut, request.CodesConformite);
        await planRepo.MettreAJourAsync(plan, ct);
        return Results.Created($"/api/v1/etudes/{etudeId}/plan-traitement-risque", plan);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/v1/etudes/{etudeId:guid}/plan-traitement-risque/mesures/{mesureId:guid}", async (
    Guid etudeId, Guid mesureId, MesureTraitementRisqueRequest request, IPlanTraitementRisqueRepository planRepo,
    IScenarioDeRisqueRepository sdrRepo, CancellationToken ct) =>
{
    var plan = await planRepo.ObtenirParEtudeAsync(etudeId, ct);
    if (plan is null)
        return Results.NotFound(new { error = "Aucun plan de traitement du risque pour cette étude." });

    var (erreurValidation, axe, coutComplexite, statut) = await ValiderMesureTraitementRisqueAsync(etudeId, request, sdrRepo, ct);
    if (erreurValidation is not null)
        return erreurValidation;

    try
    {
        plan.ModifierMesure(mesureId, request.Description, axe, request.ScenariosDeRisqueIds, request.Responsable, request.FreinsEtDifficultes, coutComplexite, request.Echeance, statut, request.CodesConformite);
        await planRepo.MettreAJourAsync(plan, ct);
        return Results.Ok(plan);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapDelete("/api/v1/etudes/{etudeId:guid}/plan-traitement-risque/mesures/{mesureId:guid}", async (
    Guid etudeId, Guid mesureId, IPlanTraitementRisqueRepository planRepo, CancellationToken ct) =>
{
    var plan = await planRepo.ObtenirParEtudeAsync(etudeId, ct);
    if (plan is null)
        return Results.NotFound(new { error = "Aucun plan de traitement du risque pour cette étude." });

    try
    {
        plan.SupprimerMesure(mesureId);
        await planRepo.MettreAJourAsync(plan, ct);
        return Results.NoContent();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// Toute route non reconnue (et non /api/...) -> index.html : le routeur React
// prend le relais (liens profonds, rechargement de page). Anonyme : la page
// se charge, puis le frontend redirige vers /connexion si pas de jeton.
if (frontendEmbarque)
{
    app.MapFallback((HttpContext ctx) =>
        ctx.Request.Path.StartsWithSegments("/api")
            ? Results.NotFound()
            : Results.File(indexHtml, "text/html")).AllowAnonymous();
}

// Mode bureau : ouvrir le navigateur une fois Kestrel a l'ecoute
// (App:OuvrirNavigateur=false pour un lancement en arriere-plan / sans interface).
if (execution.ModeBureau && app.Configuration.GetValue("App:OuvrirNavigateur", true))
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var url = app.Urls.FirstOrDefault() ?? "http://localhost:5000";
        Console.WriteLine($"\nEBIOS RM est demarre. Ouvrez {url} si le navigateur ne s'ouvre pas.\n");
        LanceurNavigateur.Ouvrir(url, execution.DossierDonnees);
    });
}

app.Run();

static async Task SupprimerScenarioDeRisqueEtReferencesAsync(
    Guid cheminAttaqueId, Guid etudeId, IScenarioDeRisqueRepository sdrRepo, IPlanTraitementRisqueRepository planRepo, CancellationToken ct)
{
    var sdr = await sdrRepo.ObtenirParCheminIdAsync(cheminAttaqueId, ct);
    if (sdr is null)
        return;

    var plan = await planRepo.ObtenirParEtudeAsync(etudeId, ct);
    if (plan is not null)
    {
        plan.RetirerReferenceScenario(sdr.Id);
        await planRepo.MettreAJourAsync(plan, ct);
    }

    await sdrRepo.SupprimerAsync(sdr, ct);
}

static async Task<(IResult? Erreur, AxeMesure Axe, NiveauCoutComplexite CoutComplexite, StatutMesure Statut)> ValiderMesureTraitementRisqueAsync(
    Guid etudeId, MesureTraitementRisqueRequest request, IScenarioDeRisqueRepository sdrRepo, CancellationToken ct)
{
    if (!Enum.TryParse<AxeMesure>(request.Axe, ignoreCase: true, out var axe))
        return (Results.BadRequest(new { error = $"Axe invalide. Valeurs autorisées : {string.Join(", ", Enum.GetNames<AxeMesure>())}" }), default, default, default);
    if (!Enum.TryParse<NiveauCoutComplexite>(request.CoutComplexite, ignoreCase: true, out var coutComplexite))
        return (Results.BadRequest(new { error = $"Coût/complexité invalide. Valeurs autorisées : {string.Join(", ", Enum.GetNames<NiveauCoutComplexite>())}" }), default, default, default);
    if (!Enum.TryParse<StatutMesure>(request.Statut, ignoreCase: true, out var statut))
        return (Results.BadRequest(new { error = $"Statut invalide. Valeurs autorisées : {string.Join(", ", Enum.GetNames<StatutMesure>())}" }), default, default, default);

    foreach (var scenarioId in request.ScenariosDeRisqueIds)
    {
        var scenario = await sdrRepo.ObtenirParIdAsync(scenarioId, ct);
        if (scenario is null || scenario.EtudeId != etudeId)
            return (Results.BadRequest(new { error = $"Scénario de risque introuvable pour cette étude : {scenarioId}." }), default, default, default);
    }

    return (null, axe, coutComplexite, statut);
}

static async Task<(List<ActionElementaireEntree>? Actions, IResult? Erreur)> ParseActionsElementairesAsync(
    Guid etudeId, List<ActionElementaireInput> actionsInput, IBienSupportRepository bienRepo, CancellationToken ct)
{
    var actions = new List<ActionElementaireEntree>();
    foreach (var a in actionsInput)
    {
        if (!Enum.TryParse<PhaseActionElementaire>(a.Phase, out var phase))
            return (null, Results.BadRequest(new
            {
                error = $"Phase invalide : '{a.Phase}'. Valeurs autorisées : {string.Join(", ", Enum.GetNames<PhaseActionElementaire>())}."
            }));

        var bien = await bienRepo.ObtenirParIdAsync(a.BienSupportId, ct);
        if (bien is null || bien.EtudeId != etudeId)
            return (null, Results.BadRequest(new { error = $"Bien support introuvable pour cette étude : {a.BienSupportId}." }));

        actions.Add(new ActionElementaireEntree(a.Description, phase, a.BienSupportId, a.TechniqueMitre));
    }
    return (actions, null);
}

record InscriptionRequest(string Email, string MotDePasse, string NomAffiche);
record ConnexionRequest(string Email, string MotDePasse);
record CreerEtudeRequest(string Nom, string Perimetre, string Mission);
record DupliquerEtudeRequest(string? Nom);
record ValiderAtelier5Request(string? Libelle);
record IndicateurRequest(string Nom, string? Categorie, string? Unite, double? Cible, double? SeuilAlerte, string Sens);
record PointMesureRequest(string Date, double Valeur, string? Commentaire);
record AjouterMesureBiblioRequest(string? Referentiel, string? Code, string Titre, string? Description, string? Categorie);
record AjouterSourceRisqueBiblioRequest(
    string SourceRisque, string DescriptionSourceRisque, string ObjectifVise, string DescriptionObjectifVise,
    string? Theme, int? MotivationTypique, int? RessourcesTypiques);
record AjouterMembreRequest(string Email, string Role);
record ChangerRoleMembreRequest(string Role);
record CreerValeurMetierRequest(string Description, string EntiteProprietaire);
record CreerBienSupportRequest(string Description, string Type, string EntiteProprietaire);
record CreerEvenementRedouteRequest(string Description, int Gravite);
record RecoterGraviteRequest(int NouvelleGravite);
record AjouterReferentielRequest(string Nom, string Etat, string? Theme = null, string? CodeControle = null, string? EtatActuel = null);
record CreerCoupleSrOvRequest(string SourceRisque, string DescriptionSourceRisque, string ObjectifVise, string DescriptionObjectifVise, string ContexteVulnerabilite, string Theme, int Motivation, int Ressources);
record DefinirPertinenceRetenueRequest(string PertinenceRetenue, string Justification);
record CreerPartiePrenanteRequest(string Nom, string RolesEtAttentes, string Representant, string Categorie, string? DescriptionCategorie = null);
record EvaluerDangerositeRequest(int Dependance, int Penetration, int MaturiteCyber, int Confiance);
record DefinirDangerositeRetenueRequest(double NiveauRetenu, string Justification);
record MesureEcosystemeRequest(string Description);
record CreerScenarioStrategiqueRequest(Guid EvenementRedouteId, string Description);
record ModifierScenarioStrategiqueRequest(Guid EvenementRedouteId, string Description);
record CreerCheminAttaqueRequest(string Description);
record ModifierCheminAttaqueRequest(string Description);
record CreerEvenementIntermediaireRequest(Guid PartiePrenanteId, string Description);
record ModeOperatoireRequest(string Description, List<ActionElementaireInput> Actions, int ProbabiliteSucces, int DifficulteTechnique);
record ActionElementaireInput(string Description, string Phase, Guid BienSupportId, string? TechniqueMitre = null);
record DefinirVraisemblanceRetenueRequest(string VraisemblanceRetenue, string Justification);
record ModifierEvenementIntermediaireRequest(string Description);
record DefinirNiveauRisqueRetenuRequest(string NiveauRetenu, string Justification);
record EvaluerRisqueResiduelRequest(int GraviteResiduelle, string VraisemblanceResiduelle);
record AccepterRisqueResiduelRequest(string NomProprietaireRisque, string NomValidateurSecurite, string? NomSponsorExecutif, string? Justification);
record MesureTraitementRisqueRequest(
    string Description, string Axe, List<Guid> ScenariosDeRisqueIds, string Responsable,
    string? FreinsEtDifficultes, string CoutComplexite, string? Echeance, string Statut,
    List<string>? CodesConformite = null);

// Rend la classe Program (générée implicitement par les top-level statements)
// accessible depuis le projet de tests, requis par WebApplicationFactory<Program>.
public partial class Program { }
