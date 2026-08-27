using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace EbiosRM.Api.Modules.Identity.Domain;

/// <summary>
/// Orchestration inscription/connexion/réinitialisation : vérifie l'unicité de
/// l'email, hache et vérifie le mot de passe, émet le jeton JWT, pilote le
/// cycle de vie des jetons de réinitialisation. Le hachage utilise
/// Microsoft.AspNetCore.Identity.PasswordHasher (PBKDF2, paramètres sûrs par
/// défaut) plutôt qu'une crypto maison.
/// </summary>
public sealed class ServiceAuthentification
{
    public const int MotDePasseLongueurMin = 8;

    // PasswordHasher<object> plutôt que PasswordHasher<Utilisateur> : l'implémentation
    // par défaut n'utilise jamais l'instance passée (juste un sel aléatoire), et
    // Utilisateur ne peut pas exister avant d'avoir son hash (constructeur privé,
    // Creer exige le hash) -- ce typage évite l'oeuf-et-la-poule sans exposer de
    // setter juste pour ce besoin technique.
    private readonly IUtilisateurRepository _repository;
    private readonly IConfiguration _configuration;
    private readonly IServiceEmail _email;
    private readonly ILogger<ServiceAuthentification> _logger;
    private readonly PasswordHasher<object> _hasher = new();

    public ServiceAuthentification(
        IUtilisateurRepository repository,
        IConfiguration configuration,
        IServiceEmail email,
        ILogger<ServiceAuthentification> logger)
    {
        _repository = repository;
        _configuration = configuration;
        _email = email;
        _logger = logger;
    }

    public async Task<(string Token, Utilisateur Utilisateur)> InscrireAsync(string email, string motDePasse, string nomAffiche, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(motDePasse) || motDePasse.Length < MotDePasseLongueurMin)
            throw new ArgumentException($"Le mot de passe doit contenir au moins {MotDePasseLongueurMin} caractères.", nameof(motDePasse));

        var emailNormalise = email.Trim().ToLowerInvariant();
        var existant = await _repository.ObtenirParEmailAsync(emailNormalise, cancellationToken);
        if (existant is not null)
            throw new ArgumentException("Un compte existe déjà avec cet email.", nameof(email));

        var hache = _hasher.HashPassword(new object(), motDePasse);
        var utilisateur = Utilisateur.Creer(emailNormalise, nomAffiche, hache);

        await _repository.AjouterAsync(utilisateur, cancellationToken);
        return (GenererJeton(utilisateur), utilisateur);
    }

    public async Task<(string Token, Utilisateur Utilisateur)?> ConnecterAsync(string email, string motDePasse, CancellationToken cancellationToken)
    {
        var utilisateur = await _repository.ObtenirParEmailAsync(email.Trim().ToLowerInvariant(), cancellationToken);
        if (utilisateur is null)
            return null;

        var resultat = _hasher.VerifyHashedPassword(new object(), utilisateur.MotDePasseHache, motDePasse);
        if (resultat == PasswordVerificationResult.Failed)
            return null;

        return (GenererJeton(utilisateur), utilisateur);
    }

    /// <summary>
    /// Déclenche l'envoi d'un lien de réinitialisation si l'email correspond à un
    /// compte. Ne révèle jamais si le compte existe : renvoie toujours sans
    /// erreur, même email inconnu ou échec d'envoi (journalisé, pas propagé).
    /// </summary>
    public async Task DemanderReinitialisationAsync(string email, CancellationToken cancellationToken)
    {
        var emailNormalise = email.Trim().ToLowerInvariant();
        var utilisateur = await _repository.ObtenirParEmailAsync(emailNormalise, cancellationToken);
        if (utilisateur is null)
        {
            _logger.LogInformation("Demande de réinitialisation pour un email inconnu : {Email}", emailNormalise);
            return;
        }

        var jetonEnClair = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        utilisateur.DemarrerReinitialisation(jetonEnClair, DateTime.UtcNow);
        await _repository.MettreAJourAsync(utilisateur, cancellationToken);

        var lien = ConstruireLienReinitialisation(jetonEnClair);
        try
        {
            await _email.EnvoyerLienReinitialisationAsync(utilisateur.Email, lien, cancellationToken);
        }
        catch (Exception ex)
        {
            // On n'échoue pas la requête : un 500 renseignerait indirectement sur
            // l'existence du compte. L'opérateur voit l'incident dans les logs.
            _logger.LogError(ex, "Échec de l'envoi du lien de réinitialisation à {Email}", utilisateur.Email);
        }
    }

    /// <summary>
    /// Applique un nouveau mot de passe à partir d'un jeton de lien.
    /// Renvoie false si le jeton est inconnu, déjà consommé ou expiré.
    /// Lève <see cref="ArgumentException"/> si le nouveau mot de passe est trop court.
    /// </summary>
    public async Task<bool> ReinitialiserMotDePasseAsync(string jetonEnClair, string nouveauMotDePasse, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(jetonEnClair))
            return false;
        if (string.IsNullOrWhiteSpace(nouveauMotDePasse) || nouveauMotDePasse.Length < MotDePasseLongueurMin)
            throw new ArgumentException($"Le mot de passe doit contenir au moins {MotDePasseLongueurMin} caractères.", nameof(nouveauMotDePasse));

        var jetonHache = Utilisateur.HacherJetonReinitialisation(jetonEnClair);
        var utilisateur = await _repository.ObtenirParJetonReinitialisationHacheAsync(jetonHache, cancellationToken);
        if (utilisateur is null)
            return false;

        var nouveauHache = _hasher.HashPassword(new object(), nouveauMotDePasse);
        if (!utilisateur.EssayerReinitialiserMotDePasse(jetonEnClair, nouveauHache, DateTime.UtcNow))
            return false;

        await _repository.MettreAJourAsync(utilisateur, cancellationToken);
        return true;
    }

    private string ConstruireLienReinitialisation(string jetonEnClair)
    {
        var baseUrl = (_configuration["App:UrlFrontend"] ?? "http://localhost:5173").TrimEnd('/');
        return $"{baseUrl}/reinitialiser-mot-de-passe?token={Uri.EscapeDataString(jetonEnClair)}";
    }

    private string GenererJeton(Utilisateur utilisateur)
    {
        var secret = _configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret n'est pas configuré.");
        var cle = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var identifiants = new SigningCredentials(cle, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, utilisateur.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, utilisateur.Email),
        };

        var jeton = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: identifiants);

        return new JwtSecurityTokenHandler().WriteToken(jeton);
    }
}
