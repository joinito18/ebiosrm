namespace EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

/// <summary>
/// Les 4 axes fixes obligatoires du plan de traitement du risque (doc
/// officielle Atelier 5, partie 1) : Gouvernance, Protection, Défense,
/// Résilience. Chaque MesureTraitementRisque est rattachée à l'un des 4.
/// </summary>
public enum AxeMesure
{
    Gouvernance,
    Protection,
    Defense,
    Resilience
}

/// <summary>
/// Échelle qualitative officielle de coût/complexité ("+"/"++"/"+++",
/// doc officielle Atelier 5, partie 1) -- libellé affiché via Libelle().
/// </summary>
public enum NiveauCoutComplexite
{
    Plus,
    PlusPlus,
    PlusPlusPlus
}

public static class NiveauCoutComplexiteExtensions
{
    /// <summary>
    /// Symbole officiel ("+"/"++"/"+++", seule forme trouvée dans la doc
    /// officielle -- aucune des occurrences de l'exemple "société de
    /// biotechnologie" n'est accompagnée d'une légende ou d'un seuil
    /// chiffré). Utiliser <see cref="LibelleAvecMot"/> pour un affichage
    /// autoportant (rapport, IHM).
    /// </summary>
    public static string Libelle(this NiveauCoutComplexite niveau) => niveau switch
    {
        NiveauCoutComplexite.Plus => "+",
        NiveauCoutComplexite.PlusPlus => "++",
        NiveauCoutComplexite.PlusPlusPlus => "+++",
        _ => throw new ArgumentOutOfRangeException(nameof(niveau), niveau, "Niveau de coût/complexité inconnu.")
    };

    /// <summary>
    /// Symbole + mot descriptif (Faible/Modéré/Élevé) -- la doc officielle ne
    /// définit ce mot nulle part (seuls les symboles bruts apparaissent),
    /// donc ce libellé est un choix d'interprétation du projet, au même
    /// titre que les seuils par défaut de ServiceCalculNiveauRisque, pas une
    /// terminologie ANSSI. Sert à rendre le symbole seul compréhensible hors
    /// contexte (rapport PDF, IHM), sans jamais remplacer le symbole exact.
    /// </summary>
    public static string LibelleAvecMot(this NiveauCoutComplexite niveau) => niveau switch
    {
        NiveauCoutComplexite.Plus => "+ (Faible)",
        NiveauCoutComplexite.PlusPlus => "++ (Modéré)",
        NiveauCoutComplexite.PlusPlusPlus => "+++ (Élevé)",
        _ => throw new ArgumentOutOfRangeException(nameof(niveau), niveau, "Niveau de coût/complexité inconnu.")
    };
}

public enum StatutMesure
{
    ALancer,
    EnCours,
    Termine
}

/// <summary>
/// Une mesure du plan de traitement du risque (doc officielle Atelier 5,
/// partie 1 -- colonnes exactes : Mesure de sécurité | Scénarios de risques
/// associés | Responsable | Freins et difficultés | Coût/Complexité |
/// Échéance | Statut). "Responsable" désigne qui EXÉCUTE la mesure --
/// distinct de "Propriétaire" (qui POSSÈDE un actif, VM/BS Atelier 1, ou un
/// risque, registre d'acceptation ScenarioDeRisque) : deux rôles ISO/CEI
/// 27005:2022 différents qui coexistent sans contradiction.
///
/// ScenariosDeRisqueIds : many-to-many par liste de Guid bruts (pas de vraie
/// FK entre agrégats séparés dans ce projet, cf. CheminAttaque.ScenarioStrategiqueId)
/// -- une mesure peut couvrir plusieurs scénarios de risque, mappée en base
/// via EF Core 8 PrimitiveCollection (colonne Postgres uuid[] native).
/// Entité owned de PlanTraitementRisque.
/// </summary>
public sealed class MesureTraitementRisque
{
    public Guid Id { get; private set; }
    public string Description { get; private set; } = default!;
    public AxeMesure Axe { get; private set; }

    private readonly List<Guid> _scenariosDeRisqueIds = new();
    public IReadOnlyList<Guid> ScenariosDeRisqueIds => _scenariosDeRisqueIds;

    public string Responsable { get; private set; } = default!;
    public string? FreinsEtDifficultes { get; private set; }
    public NiveauCoutComplexite CoutComplexite { get; private set; }
    public string? Echeance { get; private set; }
    public StatutMesure Statut { get; private set; }
    public DateTime CreeLeUtc { get; private set; }

    private MesureTraitementRisque() { }

    internal static MesureTraitementRisque Creer(
        string description, AxeMesure axe, IReadOnlyList<Guid> scenariosDeRisqueIds,
        string responsable, string? freinsEtDifficultes, NiveauCoutComplexite coutComplexite,
        string? echeance, StatutMesure statut)
    {
        Valider(description, scenariosDeRisqueIds, responsable);

        var mesure = new MesureTraitementRisque
        {
            // Id volontairement non assigné (ValueGeneratedOnAdd -- même
            // convention que ReferentielApplicable/EvenementIntermediaire).
            Description = description.Trim(),
            Axe = axe,
            Responsable = responsable.Trim(),
            FreinsEtDifficultes = string.IsNullOrWhiteSpace(freinsEtDifficultes) ? null : freinsEtDifficultes.Trim(),
            CoutComplexite = coutComplexite,
            Echeance = string.IsNullOrWhiteSpace(echeance) ? null : echeance.Trim(),
            Statut = statut,
            CreeLeUtc = DateTime.UtcNow
        };
        mesure._scenariosDeRisqueIds.AddRange(scenariosDeRisqueIds);
        return mesure;
    }

    internal void Modifier(
        string description, AxeMesure axe, IReadOnlyList<Guid> scenariosDeRisqueIds,
        string responsable, string? freinsEtDifficultes, NiveauCoutComplexite coutComplexite,
        string? echeance, StatutMesure statut)
    {
        Valider(description, scenariosDeRisqueIds, responsable);

        Description = description.Trim();
        Axe = axe;
        _scenariosDeRisqueIds.Clear();
        _scenariosDeRisqueIds.AddRange(scenariosDeRisqueIds);
        Responsable = responsable.Trim();
        FreinsEtDifficultes = string.IsNullOrWhiteSpace(freinsEtDifficultes) ? null : freinsEtDifficultes.Trim();
        CoutComplexite = coutComplexite;
        Echeance = string.IsNullOrWhiteSpace(echeance) ? null : echeance.Trim();
        Statut = statut;
    }

    /// <summary>Retire la référence à un scénario de risque supprimé, sans supprimer la mesure elle-même.</summary>
    internal bool RetirerScenario(Guid scenarioDeRisqueId) => _scenariosDeRisqueIds.Remove(scenarioDeRisqueId);

    private static void Valider(string description, IReadOnlyList<Guid> scenariosDeRisqueIds, string responsable)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La description de la mesure est obligatoire.", nameof(description));
        if (scenariosDeRisqueIds.Count == 0)
            throw new ArgumentException("Une mesure doit être associée à au moins un scénario de risque.", nameof(scenariosDeRisqueIds));
        if (string.IsNullOrWhiteSpace(responsable))
            throw new ArgumentException("Le responsable de la mesure est obligatoire.", nameof(responsable));
    }
}

/// <summary>
/// Aggregate Root : PlanTraitementRisque (module Scenarios de risque, Atelier
/// 5). Un seul plan par étude -- même moule que SocleSecurite (Atelier 1).
/// </summary>
public sealed class PlanTraitementRisque
{
    public Guid Id { get; private set; }
    public Guid EtudeId { get; private set; }

    private readonly List<MesureTraitementRisque> _mesures = new();
    public IReadOnlyList<MesureTraitementRisque> Mesures => _mesures;

    private PlanTraitementRisque() { }

    public static PlanTraitementRisque Creer(Guid etudeId)
    {
        if (etudeId == Guid.Empty)
            throw new ArgumentException("Le plan de traitement du risque doit être rattaché à une étude.", nameof(etudeId));

        return new PlanTraitementRisque { Id = Guid.NewGuid(), EtudeId = etudeId };
    }

    public void AjouterMesure(
        string description, AxeMesure axe, IReadOnlyList<Guid> scenariosDeRisqueIds,
        string responsable, string? freinsEtDifficultes, NiveauCoutComplexite coutComplexite,
        string? echeance, StatutMesure statut)
    {
        _mesures.Add(MesureTraitementRisque.Creer(description, axe, scenariosDeRisqueIds, responsable, freinsEtDifficultes, coutComplexite, echeance, statut));
    }

    public void ModifierMesure(
        Guid mesureId, string description, AxeMesure axe, IReadOnlyList<Guid> scenariosDeRisqueIds,
        string responsable, string? freinsEtDifficultes, NiveauCoutComplexite coutComplexite,
        string? echeance, StatutMesure statut)
    {
        var mesure = _mesures.FirstOrDefault(m => m.Id == mesureId);
        if (mesure is null)
            throw new ArgumentException("Mesure introuvable dans ce plan de traitement du risque.", nameof(mesureId));

        mesure.Modifier(description, axe, scenariosDeRisqueIds, responsable, freinsEtDifficultes, coutComplexite, echeance, statut);
    }

    public void SupprimerMesure(Guid mesureId)
    {
        var mesure = _mesures.FirstOrDefault(m => m.Id == mesureId);
        if (mesure is null)
            throw new ArgumentException("Mesure introuvable dans ce plan de traitement du risque.", nameof(mesureId));

        _mesures.Remove(mesure);
    }

    /// <summary>
    /// Utilisé uniquement par les cascades de suppression (cf. Program.cs) :
    /// retire un scénario de risque supprimé de toutes les mesures qui le
    /// référencent, sans jamais supprimer la mesure elle-même -- elle peut
    /// rester pertinente pour d'autres scénarios.
    /// </summary>
    public void RetirerReferenceScenario(Guid scenarioDeRisqueId)
    {
        foreach (var mesure in _mesures)
            mesure.RetirerScenario(scenarioDeRisqueId);
    }
}
