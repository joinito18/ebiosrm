using System.Security.Cryptography;
using System.Text;

namespace EbiosRM.Api.Modules.Identity.Domain;

/// <summary>
/// Aggregate Root : Utilisateur. Mur d'entrée uniquement -- pas de rôles ni de
/// permissions différenciées (décision actée : un seul niveau d'accès, toutes
/// les études restent visibles par tous les utilisateurs connectés). Le
/// hachage du mot de passe est calculé en amont par
/// Microsoft.AspNetCore.Identity.PasswordHasher -- jamais ici, même principe
/// que les autres valeurs calculées du Core Engine (P8).
///
/// Réinitialisation de mot de passe : seul le SHA-256 du jeton est persisté
/// (<see cref="JetonReinitialisationHache"/>), jamais le jeton en clair --
/// une fuite de la table ne permet pas de forger un lien valide. Le jeton en
/// clair n'existe que le temps d'un email.
/// </summary>
public sealed class Utilisateur
{
    /// <summary>Durée de validité d'un lien de réinitialisation.</summary>
    public static readonly TimeSpan DureeValiditeJetonReinitialisation = TimeSpan.FromHours(1);

    public Guid Id { get; private set; }
    public string Email { get; private set; } = default!;
    public string NomAffiche { get; private set; } = default!;
    public string MotDePasseHache { get; private set; } = default!;
    public DateTime CreeLeUtc { get; private set; }

    /// <summary>SHA-256 (hex) du jeton de réinitialisation en cours, ou null si aucune demande active.</summary>
    public string? JetonReinitialisationHache { get; private set; }
    public DateTime? JetonReinitialisationExpireLeUtc { get; private set; }

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

    /// <summary>
    /// SHA-256 (hex minuscule) d'un jeton de réinitialisation. Statique : le
    /// service en a besoin pour retrouver l'utilisateur à partir du jeton reçu
    /// dans le lien, avant même d'avoir une instance.
    /// </summary>
    public static string HacherJetonReinitialisation(string jetonEnClair)
    {
        var octets = SHA256.HashData(Encoding.UTF8.GetBytes(jetonEnClair));
        return Convert.ToHexString(octets).ToLowerInvariant();
    }

    /// <summary>
    /// Enregistre une nouvelle demande de réinitialisation. Écrase toute demande
    /// précédente encore active (le dernier lien envoyé est le seul valable).
    /// </summary>
    public void DemarrerReinitialisation(string jetonEnClair, DateTime maintenantUtc)
    {
        if (string.IsNullOrWhiteSpace(jetonEnClair))
            throw new ArgumentException("Le jeton est obligatoire.", nameof(jetonEnClair));

        JetonReinitialisationHache = HacherJetonReinitialisation(jetonEnClair);
        JetonReinitialisationExpireLeUtc = maintenantUtc.Add(DureeValiditeJetonReinitialisation);
    }

    /// <summary>
    /// Applique un nouveau mot de passe si le jeton fourni correspond à la
    /// demande active et n'est pas expiré. Consomme le jeton en cas de succès
    /// (un lien ne sert qu'une fois). Renvoie false sinon, sans rien modifier.
    /// </summary>
    public bool EssayerReinitialiserMotDePasse(string jetonEnClair, string nouveauMotDePasseHache, DateTime maintenantUtc)
    {
        if (JetonReinitialisationHache is null || JetonReinitialisationExpireLeUtc is null)
            return false;
        if (maintenantUtc >= JetonReinitialisationExpireLeUtc.Value)
            return false;
        if (string.IsNullOrWhiteSpace(nouveauMotDePasseHache))
            throw new ArgumentException("Le mot de passe haché est obligatoire.", nameof(nouveauMotDePasseHache));

        var fourniHache = HacherJetonReinitialisation(jetonEnClair);
        var correspond = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(fourniHache),
            Encoding.UTF8.GetBytes(JetonReinitialisationHache));
        if (!correspond)
            return false;

        MotDePasseHache = nouveauMotDePasseHache;
        JetonReinitialisationHache = null;
        JetonReinitialisationExpireLeUtc = null;
        return true;
    }
}
