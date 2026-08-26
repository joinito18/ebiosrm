namespace EbiosRM.Api.Modules.Identity.Domain;

/// <summary>
/// Aggregate Root : Utilisateur. Mur d'entrée uniquement -- pas de rôles ni de
/// permissions différenciées (décision actée : un seul niveau d'accès, toutes
/// les études restent visibles par tous les utilisateurs connectés). Le
/// hachage du mot de passe est calculé en amont par
/// Microsoft.AspNetCore.Identity.PasswordHasher -- jamais ici, même principe
/// que les autres valeurs calculées du Core Engine (P8).
/// </summary>
public sealed class Utilisateur
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = default!;
    public string NomAffiche { get; private set; } = default!;
    public string MotDePasseHache { get; private set; } = default!;
    public DateTime CreeLeUtc { get; private set; }

    private Utilisateur() { }

    public static Utilisateur Creer(string email, string nomAffiche, string motDePasseHache)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("L'email est obligatoire.", nameof(email));
        if (string.IsNullOrWhiteSpace(nomAffiche))
            throw new ArgumentException("Le nom affiché est obligatoire.", nameof(nomAffiche));
        if (string.IsNullOrWhiteSpace(motDePasseHache))
            throw new ArgumentException("Le mot de passe haché est obligatoire.", nameof(motDePasseHache));

        return new Utilisateur
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            NomAffiche = nomAffiche.Trim(),
            MotDePasseHache = motDePasseHache,
            CreeLeUtc = DateTime.UtcNow
        };
    }
}
