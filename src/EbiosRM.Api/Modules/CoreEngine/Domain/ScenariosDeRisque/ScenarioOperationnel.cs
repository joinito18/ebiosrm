namespace EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

/// <summary>
/// Les 4 phases de la séquence type d'attaque EBIOS RM (distinct du Cyber
/// Kill Chain à 7 phases). Portée par chaque ActionElementaire, pas par le
/// ModeOperatoire qui les regroupe toutes.
/// </summary>
public enum PhaseActionElementaire { Connaitre, Rentrer, Trouver, Exploiter }

/// <summary>
/// Le geste technique le plus fin d'un mode opératoire (doc officielle
/// Atelier 4). Cible précisément un BienSupport de l'Atelier 1 : la boucle
/// est bouclée, l'attaque finit toujours par toucher un bien concret
/// identifié dès le premier atelier. Entité owned de ModeOperatoire.
/// </summary>
public sealed class ActionElementaire
{
    public Guid Id { get; private set; }
    public string Description { get; private set; } = default!;
    public PhaseActionElementaire Phase { get; private set; }
    public Guid BienSupportId { get; private set; }

    /// <summary>
    /// Identifiant de la technique MITRE ATT&amp;CK associée (ex. « T1078 »),
    /// optionnel -- aide à décrire le geste et à objectiver la vraisemblance.
    /// Texte libre : accepte un identifiant du catalogue ou saisi à la main.
    /// </summary>
    public string? TechniqueMitre { get; private set; }

    private ActionElementaire() { }

    internal static ActionElementaire Creer(string description, PhaseActionElementaire phase, Guid bienSupportId, string? techniqueMitre = null)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La description de l'action élémentaire est obligatoire.", nameof(description));
        if (bienSupportId == Guid.Empty)
            throw new ArgumentException("L'action élémentaire doit cibler un bien support.", nameof(bienSupportId));

        return new ActionElementaire
        {
            // Id volontairement non assigné (ValueGeneratedOnAdd).
            Description = description.Trim(),
            Phase = phase,
            BienSupportId = bienSupportId,
            TechniqueMitre = string.IsNullOrWhiteSpace(techniqueMitre) ? null : techniqueMitre.Trim(),
        };
    }
}

/// <summary>
/// Entrée immuable pour construire/reconstruire la liste d'ActionElementaire
/// d'un ModeOperatoire, sans exposer l'entité owned elle-même côté appelant.
/// </summary>
public readonly record struct ActionElementaireEntree(string Description, PhaseActionElementaire Phase, Guid BienSupportId, string? TechniqueMitre = null);

/// <summary>
/// Un mode opératoire technique possible pour réaliser un chemin d'attaque
/// (doc officielle Atelier 4 : "ajustez la granularité... modulaire selon si
/// l'attaquant attaque directement ou par rebond"). Se décompose en 1..*
/// ActionElementaire, réparties selon la séquence type CONNAÎTRE/RENTRER/
/// TROUVER/EXPLOITER. Entité owned de ScenarioOperationnel.
/// </summary>
public sealed class ModeOperatoire
{
    public Guid Id { get; private set; }
    public string Description { get; private set; } = default!;
    public int ProbabiliteSucces { get; private set; }
    public int DifficulteTechnique { get; private set; }

    private readonly List<ActionElementaire> _actionsElementaires = new();
    public IReadOnlyList<ActionElementaire> ActionsElementaires => _actionsElementaires;

    /// <summary>
    /// Vraisemblance calculée (grille officielle probabilité x difficulté),
    /// jamais stockée séparément (P8).
    /// </summary>
    public NiveauVraisemblance VraisemblanceCalculee => ServiceCalculVraisemblance.Calculer(ProbabiliteSucces, DifficulteTechnique);

    /// <summary>Écart de jugement d'expert de l'analyste, le cas échéant -- seul cas où une valeur passe de "jamais stockée" à "partiellement stockée".</summary>
    public NiveauVraisemblance? VraisemblanceRetenue { get; private set; }
    public string? JustificationVraisemblance { get; private set; }

    /// <summary>Valeur effective : la retenue si l'analyste s'est écarté du calcul, sinon la calculée.</summary>
    public NiveauVraisemblance Vraisemblance => VraisemblanceRetenue ?? VraisemblanceCalculee;

    private ModeOperatoire() { }

    internal static ModeOperatoire Creer(
        string description, IReadOnlyList<ActionElementaireEntree> actions,
        int probabiliteSucces, int difficulteTechnique)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La description du mode opératoire est obligatoire.", nameof(description));
        if (actions.Count == 0)
            throw new ArgumentException(
                "Au moins une action élémentaire est requise (méthodologie EBIOS RM : CONNAÎTRE/RENTRER/TROUVER/EXPLOITER).",
                nameof(actions));

        // Valide la cotation en tentant le calcul (lève ArgumentOutOfRangeException si hors échelle).
        ServiceCalculVraisemblance.Calculer(probabiliteSucces, difficulteTechnique);

        var mode = new ModeOperatoire
        {
            // Id volontairement non assigné (ValueGeneratedOnAdd).
            Description = description.Trim(),
            ProbabiliteSucces = probabiliteSucces,
            DifficulteTechnique = difficulteTechnique
        };
        foreach (var a in actions)
            mode._actionsElementaires.Add(ActionElementaire.Creer(a.Description, a.Phase, a.BienSupportId, a.TechniqueMitre));

        return mode;
    }

    internal void Modifier(
        string description, IReadOnlyList<ActionElementaireEntree> actions,
        int probabiliteSucces, int difficulteTechnique)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La description du mode opératoire est obligatoire.", nameof(description));
        if (actions.Count == 0)
            throw new ArgumentException(
                "Au moins une action élémentaire est requise (méthodologie EBIOS RM : CONNAÎTRE/RENTRER/TROUVER/EXPLOITER).",
                nameof(actions));

        ServiceCalculVraisemblance.Calculer(probabiliteSucces, difficulteTechnique);

        Description = description.Trim();
        ProbabiliteSucces = probabiliteSucces;
        DifficulteTechnique = difficulteTechnique;

        _actionsElementaires.Clear();
        foreach (var a in actions)
            _actionsElementaires.Add(ActionElementaire.Creer(a.Description, a.Phase, a.BienSupportId, a.TechniqueMitre));
        // L'override (VraisemblanceRetenue/JustificationVraisemblance), s'il existe,
        // n'est jamais effacé par une modification des entrées -- sticky jusqu'à
        // DefinirVraisemblanceRetenue/ReinitialiserVraisemblance explicites.
    }

    internal void DefinirVraisemblanceRetenue(NiveauVraisemblance retenue, string justification)
    {
        if (string.IsNullOrWhiteSpace(justification))
            throw new ArgumentException("Une justification est obligatoire pour retenir une vraisemblance différente du calcul.", nameof(justification));

        VraisemblanceRetenue = retenue;
        JustificationVraisemblance = justification.Trim();
    }

    internal void ReinitialiserVraisemblance()
    {
        VraisemblanceRetenue = null;
        JustificationVraisemblance = null;
    }
}

/// <summary>
/// Aggregate Root : ScenarioOperationnel (module Scenarios de risque, Atelier 4).
/// Relation 1:1 stricte avec un CheminAttaque (contrainte unique en base,
/// même principe que ScenarioStrategique/CoupleSourceRisqueObjectifVise).
/// "1 chemin d'attaque stratégique => 1 scénario opérationnel décrivant
/// éventuellement plusieurs modes opératoires" (doc officielle).
/// </summary>
public sealed class ScenarioOperationnel
{
    public Guid Id { get; private set; }
    public Guid EtudeId { get; private set; }
    public Guid CheminAttaqueId { get; private set; }
    public DateTime CreeLeUtc { get; private set; }

    private readonly List<ModeOperatoire> _modesOperatoires = new();
    public IReadOnlyList<ModeOperatoire> ModesOperatoires => _modesOperatoires;

    /// <summary>
    /// Vraisemblance globale du scénario = la plus vraisemblable (favorable à
    /// l'attaquant) de ses modes opératoires -- cohérent avec l'exemple
    /// officiel (3 modes cotés V3/V1/V2 => scénario global V3). Null tant
    /// qu'aucun mode opératoire n'est défini.
    /// </summary>
    public NiveauVraisemblance? VraisemblanceGlobale =>
        _modesOperatoires.Count == 0 ? null : _modesOperatoires.Max(m => m.Vraisemblance);

    private ScenarioOperationnel() { }

    public static ScenarioOperationnel Creer(Guid etudeId, Guid cheminAttaqueId)
    {
        if (etudeId == Guid.Empty)
            throw new ArgumentException("Le scénario opérationnel doit être rattaché à une étude.", nameof(etudeId));
        if (cheminAttaqueId == Guid.Empty)
            throw new ArgumentException("Le scénario opérationnel doit être rattaché à un chemin d'attaque.", nameof(cheminAttaqueId));

        return new ScenarioOperationnel
        {
            Id = Guid.NewGuid(),
            EtudeId = etudeId,
            CheminAttaqueId = cheminAttaqueId,
            CreeLeUtc = DateTime.UtcNow
        };
    }

    public void AjouterModeOperatoire(
        string description, IReadOnlyList<ActionElementaireEntree> actions,
        int probabiliteSucces, int difficulteTechnique)
    {
        _modesOperatoires.Add(ModeOperatoire.Creer(description, actions, probabiliteSucces, difficulteTechnique));
    }

    public void ModifierModeOperatoire(
        Guid modeOperatoireId, string description, IReadOnlyList<ActionElementaireEntree> actions,
        int probabiliteSucces, int difficulteTechnique)
    {
        var mode = _modesOperatoires.FirstOrDefault(m => m.Id == modeOperatoireId);
        if (mode is null)
            throw new ArgumentException("Mode opératoire introuvable sur ce scénario opérationnel.", nameof(modeOperatoireId));
        mode.Modifier(description, actions, probabiliteSucces, difficulteTechnique);
    }

    public void SupprimerModeOperatoire(Guid modeOperatoireId)
    {
        var mode = _modesOperatoires.FirstOrDefault(m => m.Id == modeOperatoireId);
        if (mode is null)
            throw new ArgumentException("Mode opératoire introuvable sur ce scénario opérationnel.", nameof(modeOperatoireId));
        _modesOperatoires.Remove(mode);
    }

    public void DefinirVraisemblanceRetenueModeOperatoire(Guid modeOperatoireId, NiveauVraisemblance retenue, string justification)
    {
        var mode = _modesOperatoires.FirstOrDefault(m => m.Id == modeOperatoireId);
        if (mode is null)
            throw new ArgumentException("Mode opératoire introuvable sur ce scénario opérationnel.", nameof(modeOperatoireId));
        mode.DefinirVraisemblanceRetenue(retenue, justification);
    }

    public void ReinitialiserVraisemblanceModeOperatoire(Guid modeOperatoireId)
    {
        var mode = _modesOperatoires.FirstOrDefault(m => m.Id == modeOperatoireId);
        if (mode is null)
            throw new ArgumentException("Mode opératoire introuvable sur ce scénario opérationnel.", nameof(modeOperatoireId));
        mode.ReinitialiserVraisemblance();
    }
}
