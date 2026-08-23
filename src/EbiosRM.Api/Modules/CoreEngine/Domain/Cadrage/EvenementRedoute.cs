namespace EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

/// <summary>
/// Aggregate Root : EvenementRedoute (ER).
/// INV8 : la Gravité doit appartenir à l'échelle officielle EBIOS (1 à 4).
/// Rattaché à une ValeurMetier existante.
/// Échelle simplifiée ici en contrainte de plage — sera remplacée par une
/// vraie référence à VersionReferentielEBIOS quand cet agrégat existera
/// (pas construit prématurément, cf. contrainte "pas d'abstraction en
/// prévision d'un besoin futur non encore concret").
/// </summary>
public sealed class EvenementRedoute
{
    public const int GraviteMin = 1;
    public const int GraviteMax = 4;

    public Guid Id { get; private set; }
    public Guid EtudeId { get; private set; }
    public Guid ValeurMetierId { get; private set; }
    public string Description { get; private set; } = default!;
    public int Gravite { get; private set; }
    public DateTime CreeLeUtc { get; private set; }

    private EvenementRedoute() { }

    public static EvenementRedoute Creer(
        Guid etudeId,
        Guid valeurMetierId,
        string description,
        int gravite)
    {
        if (etudeId == Guid.Empty)
            throw new ArgumentException("L'événement redouté doit être rattaché à une étude.", nameof(etudeId));
        if (valeurMetierId == Guid.Empty)
            throw new ArgumentException("L'événement redouté doit être rattaché à une valeur métier.", nameof(valeurMetierId));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La description de l'événement redouté est obligatoire.", nameof(description));
        if (gravite < GraviteMin || gravite > GraviteMax)
            throw new ArgumentOutOfRangeException(
                nameof(gravite),
                gravite,
                $"La gravité doit être comprise entre {GraviteMin} et {GraviteMax} (échelle EBIOS RM, INV8).");

        return new EvenementRedoute
        {
            Id = Guid.NewGuid(),
            EtudeId = etudeId,
            ValeurMetierId = valeurMetierId,
            Description = description.Trim(),
            Gravite = gravite,
            CreeLeUtc = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Permet de recoter la gravité après création — nécessaire pour le flux
    /// "modification d'une donnée d'un atelier précédent" (Phase 1.5, §3.5),
    /// qui déclenchera plus tard un recalcul des scénarios dépendants.
    /// Reste une méthode séparée de ModifierDescription : la gravité a un
    /// impact métier (recalcul futur), la description n'en a pas.
    /// </summary>
    public void RecoterGravite(int nouvelleGravite)
    {
        if (nouvelleGravite < GraviteMin || nouvelleGravite > GraviteMax)
            throw new ArgumentOutOfRangeException(
                nameof(nouvelleGravite),
                nouvelleGravite,
                $"La gravité doit être comprise entre {GraviteMin} et {GraviteMax} (échelle EBIOS RM, INV8).");

        Gravite = nouvelleGravite;
    }

    public void ModifierDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La description de l'événement redouté est obligatoire.", nameof(description));

        Description = description.Trim();
    }
}
