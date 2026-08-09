using EbiosRM.Api.Infrastructure.Persistence;
using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;
using EbiosRM.Api.Modules.CoreEngine.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddDbContext<EbiosDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("EbiosDb")
    ));

builder.Services.AddScoped<IEtudeRepository, EtudeRepository>();
builder.Services.AddScoped<IValeurMetierRepository, ValeurMetierRepository>();
builder.Services.AddScoped<IBienSupportRepository, BienSupportRepository>();
builder.Services.AddScoped<IEvenementRedouteRepository, EvenementRedouteRepository>();
builder.Services.AddScoped<ISocleSecuriteRepository, SocleSecuriteRepository>();
builder.Services.AddScoped<ServiceValidationCompletudeAtelier1>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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
});

// --- Etudes ---

app.MapPost("/api/v1/etudes", async (CreerEtudeRequest request, IEtudeRepository repo, CancellationToken ct) =>
{
    try
    {
        var etude = Etude.Creer(request.Nom, request.Perimetre);
        await repo.AjouterAsync(etude, ct);
        return Results.Created($"/api/v1/etudes/{etude.Id}", etude);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/v1/etudes/{id:guid}", async (Guid id, IEtudeRepository repo, CancellationToken ct) =>
{
    var etude = await repo.ObtenirParIdAsync(id, ct);
    return etude is null ? Results.NotFound() : Results.Ok(etude);
});

app.MapGet("/api/v1/etudes", async (IEtudeRepository repo, CancellationToken ct) =>
{
    var etudes = await repo.ListerAsync(ct);
    return Results.Ok(etudes);
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
    Guid id, IEtudeRepository repo, ServiceValidationCompletudeAtelier1 serviceValidation, CancellationToken ct) =>
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

    try
    {
        etude.ValiderAtelier1();
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
        var valeurMetier = ValeurMetier.Creer(etudeId, request.Description, request.EntiteResponsable);
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
        var bienSupport = BienSupport.Creer(etudeId, valeurMetierId, request.Description, type, request.EntiteResponsable);
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
        socle.AjouterReferentiel(request.Nom, etat);
        await socleRepo.MettreAJourAsync(socle, ct);
        return Results.Ok(socle);
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

app.Run();

record CreerEtudeRequest(string Nom, string Perimetre);
record CreerValeurMetierRequest(string Description, string EntiteResponsable);
record CreerBienSupportRequest(string Description, string Type, string EntiteResponsable);
record CreerEvenementRedouteRequest(string Description, int Gravite);
record RecoterGraviteRequest(int NouvelleGravite);
record AjouterReferentielRequest(string Nom, string Etat);
