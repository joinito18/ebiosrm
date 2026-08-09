namespace EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

/// <summary>
/// Aggregate Root : ValeurMetier.
/// Rattachée à une Etude par référence d'ID (pas de composition forte).
/// INV6 : recommandation de 5 à 10 valeurs métier par étude — règle souple,
/// vérifiée par le Domain Service ServiceValidationComplétudeAtelier (plus tard),
/// jamais bloquante à la création individuelle.
/// </summary>
public sealed class ValeurMetier
{
    public Guid Id { get; private set; }
    public Guid EtudeId { get; private set; }
    public string Description { get; private set; } = default!;
    public string EntiteResponsable { get; private set; } = default!;
    public DateTime CreeLeUtc { get; private set; }

    private ValeurMetier() { }

    public static ValeurMetier Creer(Guid etudeId, string description, string entiteResponsable)
    {
        if (etudeId == Guid.Empty)
            throw new ArgumentException("La valeur métier doit être rattachée à une étude existante.", nameof(etudeId));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La description de la valeur métier est obligatoire.", nameof(description));

        if (string.IsNullOrWhiteSpace(entiteResponsable))
            throw new ArgumentException("L'entité responsable est obligatoire.", nameof(entiteResponsable));

        return new ValeurMetier
        {
            Id = Guid.NewGuid(),
            EtudeId = etudeId,
            Description = description.Trim(),
            EntiteResponsable = entiteResponsable.Trim(),
            CreeLeUtc = DateTime.UtcNow
        };
    }
}
