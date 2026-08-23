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
    public string EntiteProprietaire { get; private set; } = default!;
    public DateTime CreeLeUtc { get; private set; }

    private BienSupport() { }

    public static BienSupport Creer(
        Guid etudeId,
        Guid valeurMetierId,
        string description,
        TypeBienSupport type,
        string entiteProprietaire)
    {
        if (etudeId == Guid.Empty)
            throw new ArgumentException("Le bien support doit être rattaché à une étude.", nameof(etudeId));
        if (valeurMetierId == Guid.Empty)
            throw new ArgumentException("Le bien support doit être associé à une valeur métier (INV7).", nameof(valeurMetierId));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La description du bien support est obligatoire.", nameof(description));
        if (string.IsNullOrWhiteSpace(entiteProprietaire))
            throw new ArgumentException("L'entité propriétaire est obligatoire.", nameof(entiteProprietaire));

        return new BienSupport
        {
            Id = Guid.NewGuid(),
            EtudeId = etudeId,
            ValeurMetierId = valeurMetierId,
            Description = description.Trim(),
            Type = type,
            EntiteProprietaire = entiteProprietaire.Trim(),
            CreeLeUtc = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Corrige description, type et/ou entité propriétaire après création.
    /// Ne rattache jamais à une autre valeur métier (INV7 vérifié uniquement
    /// à la création) — pas de besoin identifié de le permettre pour l'instant.
    /// </summary>
    public void Modifier(string description, TypeBienSupport type, string entiteProprietaire)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La description du bien support est obligatoire.", nameof(description));
        if (string.IsNullOrWhiteSpace(entiteProprietaire))
            throw new ArgumentException("L'entité propriétaire est obligatoire.", nameof(entiteProprietaire));

        Description = description.Trim();
        Type = type;
        EntiteProprietaire = entiteProprietaire.Trim();
    }
}
