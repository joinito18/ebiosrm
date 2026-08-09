namespace EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

public enum TypeBienSupport
{
    SystemeInformation,
    Reseau,
    RessourcesHumaines,
    Local
}

/// <summary>
/// Aggregate Root : BienSupport.
/// INV7 : un BienSupport doit être associé à au moins une ValeurMetier existante,
/// vérifié à la création (la ValeurMetier doit exister au moment de l'appel).
/// </summary>
public sealed class BienSupport
{
    public Guid Id { get; private set; }
    public Guid EtudeId { get; private set; }
    public Guid ValeurMetierId { get; private set; }
    public string Description { get; private set; } = default!;
    public TypeBienSupport Type { get; private set; }
    public string EntiteResponsable { get; private set; } = default!;
    public DateTime CreeLeUtc { get; private set; }

    private BienSupport() { }

    public static BienSupport Creer(
        Guid etudeId,
        Guid valeurMetierId,
        string description,
        TypeBienSupport type,
        string entiteResponsable)
    {
        if (etudeId == Guid.Empty)
            throw new ArgumentException("Le bien support doit être rattaché à une étude.", nameof(etudeId));

        if (valeurMetierId == Guid.Empty)
            throw new ArgumentException("Le bien support doit être associé à une valeur métier (INV7).", nameof(valeurMetierId));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La description du bien support est obligatoire.", nameof(description));

        if (string.IsNullOrWhiteSpace(entiteResponsable))
            throw new ArgumentException("L'entité responsable est obligatoire.", nameof(entiteResponsable));

        return new BienSupport
        {
            Id = Guid.NewGuid(),
            EtudeId = etudeId,
            ValeurMetierId = valeurMetierId,
            Description = description.Trim(),
            Type = type,
            EntiteResponsable = entiteResponsable.Trim(),
            CreeLeUtc = DateTime.UtcNow
        };
    }
}
