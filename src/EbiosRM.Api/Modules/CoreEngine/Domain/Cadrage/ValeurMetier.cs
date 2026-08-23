namespace EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

/// <summary>
/// Aggregate Root : ValeurMetier.
/// Rattachée à une Etude par référence d'ID (pas de composition forte).
/// INV6 : recommandation de 5 à 10 valeurs métier par étude — règle souple,
/// vérifiée par le Domain Service ServiceValidationComplétudeAtelier (plus tard),
/// jamais bloquante à la création individuelle.
/// "Propriétaire" est le terme officiel depuis EBIOS RM 1.5 (mars 2024,
/// conformité ISO/CEI 27005:2022) -- remplace "Responsable", plus précis
/// (aligné sur la notion de "risk owner"/"asset owner" d'ISO 27005:2022).
/// </summary>
public sealed class ValeurMetier
{
    public Guid Id { get; private set; }
    public Guid EtudeId { get; private set; }
    public string Description { get; private set; } = default!;
    public string EntiteProprietaire { get; private set; } = default!;
    public DateTime CreeLeUtc { get; private set; }

    private ValeurMetier() { }

    public static ValeurMetier Creer(Guid etudeId, string description, string entiteProprietaire)
    {
        if (etudeId == Guid.Empty)
            throw new ArgumentException("La valeur métier doit être rattachée à une étude existante.", nameof(etudeId));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La description de la valeur métier est obligatoire.", nameof(description));
        if (string.IsNullOrWhiteSpace(entiteProprietaire))
            throw new ArgumentException("L'entité propriétaire est obligatoire.", nameof(entiteProprietaire));

        return new ValeurMetier
        {
            Id = Guid.NewGuid(),
            EtudeId = etudeId,
            Description = description.Trim(),
            EntiteProprietaire = entiteProprietaire.Trim(),
            CreeLeUtc = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Corrige la description et/ou l'entité propriétaire après création —
    /// flux "modification d'une donnée d'un atelier précédent" (Phase 1.5).
    /// Ne modifie jamais un snapshot déjà figé (P13/P16) : seule une
    /// revalidation ultérieure de l'atelier créera une nouvelle version.
    /// </summary>
    public void Modifier(string description, string entiteProprietaire)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La description de la valeur métier est obligatoire.", nameof(description));
        if (string.IsNullOrWhiteSpace(entiteProprietaire))
            throw new ArgumentException("L'entité propriétaire est obligatoire.", nameof(entiteProprietaire));

        Description = description.Trim();
        EntiteProprietaire = entiteProprietaire.Trim();
    }
}
