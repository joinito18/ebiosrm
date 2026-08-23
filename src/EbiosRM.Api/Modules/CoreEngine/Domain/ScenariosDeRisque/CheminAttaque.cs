namespace EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

/// <summary>
/// Un evenement genere par la source de risque en franchissant une partie
/// prenante de l'ecosysteme, sur le chemin vers sa cible (doc officielle
/// Atelier 3 : "EI : evenement intermediaire associe a une partie prenante
/// de l'ecosysteme"). Entite enfant de CheminAttaque (owned), ordonnee.
/// </summary>
public sealed class EvenementIntermediaire
{
    public Guid Id { get; private set; }
    public Guid PartiePrenanteId { get; private set; }
    public string Description { get; private set; } = default!;
    public int Ordre { get; private set; }

    private EvenementIntermediaire() { }

    internal static EvenementIntermediaire Creer(Guid partiePrenanteId, string description, int ordre)
    {
        if (partiePrenanteId == Guid.Empty)
            throw new ArgumentException("L'événement intermédiaire doit être rattaché à une partie prenante.", nameof(partiePrenanteId));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La description de l'événement intermédiaire est obligatoire.", nameof(description));

        return new EvenementIntermediaire
        {
            // Id volontairement non assigné : mapping EF ValueGeneratedOnAdd()
            // (même convention que ReferentielApplicable) -- l'assigner ici
            // faisait croire à EF Core que l'entité existait déjà, générant
            // un UPDATE au lieu d'un INSERT (DbUpdateConcurrencyException).
            PartiePrenanteId = partiePrenanteId,
            Description = description.Trim(),
            Ordre = ordre
        };
    }

    internal void Modifier(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La description de l'événement intermédiaire est obligatoire.", nameof(description));
        Description = description.Trim();
    }
}

/// <summary>
/// Aggregate Root : CheminAttaque (module Scenarios de risque, Atelier 3).
/// "1 scenario stratégique => plusieurs chemins d'attaque" ; "1 chemin
/// d'attaque => sequencement d'actions ou d'effets que la source de risque
/// devra probablement generer pour atteindre son objectif" (doc officielle).
/// Un chemin traverse 0..N parties prenantes (0 = attaque directe, canal
/// d'exfiltration direct par exemple), chaque franchissement generant un
/// EvenementIntermediaire ordonne.
/// </summary>
public sealed class CheminAttaque
{
    public Guid Id { get; private set; }
    public Guid EtudeId { get; private set; }
    public Guid ScenarioStrategiqueId { get; private set; }
    public string Description { get; private set; } = default!;
    public DateTime CreeLeUtc { get; private set; }

    private readonly List<EvenementIntermediaire> _evenementsIntermediaires = new();
    public IReadOnlyList<EvenementIntermediaire> EvenementsIntermediaires => _evenementsIntermediaires;

    private CheminAttaque() { }

    public static CheminAttaque Creer(Guid etudeId, Guid scenarioStrategiqueId, string description)
    {
        if (etudeId == Guid.Empty)
            throw new ArgumentException("Le chemin d'attaque doit être rattaché à une étude.", nameof(etudeId));
        if (scenarioStrategiqueId == Guid.Empty)
            throw new ArgumentException("Le chemin d'attaque doit être rattaché à un scénario stratégique.", nameof(scenarioStrategiqueId));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La description du chemin d'attaque est obligatoire.", nameof(description));

        return new CheminAttaque
        {
            Id = Guid.NewGuid(),
            EtudeId = etudeId,
            ScenarioStrategiqueId = scenarioStrategiqueId,
            Description = description.Trim(),
            CreeLeUtc = DateTime.UtcNow
        };
    }

    public void Modifier(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La description du chemin d'attaque est obligatoire.", nameof(description));
        Description = description.Trim();
    }

    public void AjouterEvenementIntermediaire(Guid partiePrenanteId, string description)
    {
        _evenementsIntermediaires.Add(EvenementIntermediaire.Creer(partiePrenanteId, description, _evenementsIntermediaires.Count));
    }

    public void ModifierEvenementIntermediaire(Guid evenementIntermediaireId, string description)
    {
        var ei = _evenementsIntermediaires.FirstOrDefault(e => e.Id == evenementIntermediaireId);
        if (ei is null)
            throw new ArgumentException("Événement intermédiaire introuvable sur ce chemin d'attaque.", nameof(evenementIntermediaireId));
        ei.Modifier(description);
    }

    public void SupprimerEvenementIntermediaire(Guid evenementIntermediaireId)
    {
        var ei = _evenementsIntermediaires.FirstOrDefault(e => e.Id == evenementIntermediaireId);
        if (ei is null)
            throw new ArgumentException("Événement intermédiaire introuvable sur ce chemin d'attaque.", nameof(evenementIntermediaireId));
        _evenementsIntermediaires.Remove(ei);
    }
}
