using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace EbiosRM.Api.Modules.Identity.Domain;

/// <summary>
/// Orchestration inscription/connexion : vérifie l'unicité de l'email, hache
/// et vérifie le mot de passe, émet le jeton JWT. Le hachage utilise
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
    private readonly PasswordHasher<object> _hasher = new();

    public ServiceAuthentification(IUtilisateurRepository repository, IConfiguration configuration)
    {
        _repository = repository;
        _configuration = configuration;
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
