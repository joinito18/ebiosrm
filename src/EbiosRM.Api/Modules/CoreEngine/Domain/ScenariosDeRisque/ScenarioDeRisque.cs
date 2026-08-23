using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

namespace EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

/// <summary>
/// Échelle de niveau de risque officielle EBIOS RM (Atelier 5), dérivée de la
/// grille Gravité x Vraisemblance (cf. ServiceCalculNiveauRisque).
/// </summary>
public enum NiveauRisque
{
    Faible,
    Moyen,
    Eleve
}

/// <summary>
/// Classe d'acceptation officielle associée à un niveau de risque (doc
/// officielle Atelier 5, partie 1) : Faible -> acceptable en l'état, Moyen ->
/// tolérable sous contrôle, Élevé -> inacceptable en l'état (mesures de
/// réduction impératives).
/// </summary>
public enum ClasseAcceptation
{
    AcceptableEnLEtat,
    TolerableSousControle,
    Inacceptable
}

/// <summary>
/// Aggregate Root : ScenarioDeRisque (module Scenarios de risque, Atelier 5).
/// "1 scénario de risque = 1 chemin d'attaque + son scénario opérationnel
/// associé, évalué avant ET après application du plan de traitement" (doc
/// officielle + PROJECT_CONTEXT.md). Relation 1:1 stricte avec un
/// CheminAttaque, même principe que ScenarioOperationnel/CheminAttaque.
///
/// Le niveau de risque INITIAL n'est jamais stocké ici : Gravité (de
/// l'EvenementRedoute visé) et Vraisemblance (du ScenarioOperationnel) vivent
/// sur des agrégats externes et sont lues en direct au moment du calcul (P8,
/// même règle que ScenarioStrategique.Gravite) -- voir
/// ServiceAssemblageScenariosDeRisque. Seul l'écart de jugement d'expert sur
/// ce niveau initial appartient réellement à cet agrégat.
///
/// Le risque RÉSIDUEL, en revanche, exige une nouvelle saisie de l'analyste
/// (Gravité résiduelle + Vraisemblance résiduelle, ré-estimées après
/// application du plan de traitement) : ces données et leur niveau calculé
/// sont bien la propriété de cet agrégat, même principe que
/// PartiePrenante.EvaluerDangerositeResiduelle.
/// </summary>
public sealed class ScenarioDeRisque
{
    public Guid Id { get; private set; }
    public Guid EtudeId { get; private set; }
    public Guid CheminAttaqueId { get; private set; }
    public DateTime CreeLeUtc { get; private set; }

    /// <summary>Écart de jugement d'expert de l'analyste sur le niveau initial, le cas échéant.</summary>
    public NiveauRisque? NiveauRisqueInitialRetenu { get; private set; }
    public string? JustificationNiveauRisqueInitial { get; private set; }

    /// <summary>Réévaluation manuelle après application des mesures du plan de traitement.</summary>
    public int? GraviteResiduelle { get; private set; }
    public NiveauVraisemblance? VraisemblanceResiduelle { get; private set; }

    /// <summary>Niveau résiduel calculé par ServiceCalculNiveauRisque, jamais saisi manuellement.</summary>
    public NiveauRisque? NiveauRisqueResiduelCalcule { get; private set; }

    /// <summary>Écart de jugement d'expert de l'analyste sur le niveau résiduel, le cas échéant.</summary>
    public NiveauRisque? NiveauRisqueResiduelRetenu { get; private set; }
    public string? JustificationNiveauRisqueResiduel { get; private set; }

    /// <summary>Valeur effective : la retenue si l'analyste s'est écarté du calcul, sinon la calculée.</summary>
    public NiveauRisque? NiveauRisqueResiduel => NiveauRisqueResiduelRetenu ?? NiveauRisqueResiduelCalcule;

    /// <summary>
    /// Classe d'acceptation dérivée du niveau résiduel effectif -- jamais
    /// persistée (calculée à la volée, même principe que PartiePrenante.Zone).
    /// </summary>
    public ClasseAcceptation? ClasseAcceptationResiduelle =>
        NiveauRisqueResiduel.HasValue ? ServiceCalculNiveauRisque.DeterminerClasseAcceptation(NiveauRisqueResiduel.Value) : null;

    /// <summary>
    /// Acceptation formelle par la Direction (ISO/CEI 27005:2022 + doctrine
    /// officielle EBIOS RM : "les risques résiduels sont acceptés
    /// formellement par la direction"). Trois rôles nommés en texte libre
    /// (pas de RBAC dans ce projet) : le propriétaire du risque et le
    /// validateur sécurité sont toujours exigés ; le sponsor exécutif et une
    /// justification écrite ne sont exigés que si le risque résiduel reste
    /// Élevé (exemple officiel : la direction documente pourquoi elle
    /// maintient un risque élevé plutôt que de le traiter davantage).
    /// </summary>
    public bool AccepteParDirection { get; private set; }
    public string? NomProprietaireRisque { get; private set; }
    public string? NomValidateurSecurite { get; private set; }
    public string? NomSponsorExecutif { get; private set; }
    public string? JustificationAcceptation { get; private set; }
    public DateTime? DateAcceptationUtc { get; private set; }

    private ScenarioDeRisque() { }

    public static ScenarioDeRisque Creer(Guid etudeId, Guid cheminAttaqueId)
    {
        if (etudeId == Guid.Empty)
            throw new ArgumentException("Le scénario de risque doit être rattaché à une étude.", nameof(etudeId));
        if (cheminAttaqueId == Guid.Empty)
            throw new ArgumentException("Le scénario de risque doit être rattaché à un chemin d'attaque.", nameof(cheminAttaqueId));

        return new ScenarioDeRisque
        {
            Id = Guid.NewGuid(),
            EtudeId = etudeId,
            CheminAttaqueId = cheminAttaqueId,
            CreeLeUtc = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Enregistre le jugement d'expert de l'analyste sur le niveau de risque
    /// initial, qui prévaut sur le calcul mécanique. Justification obligatoire.
    /// </summary>
    public void DefinirNiveauRisqueInitialRetenu(NiveauRisque niveauRetenu, string justification)
    {
        if (string.IsNullOrWhiteSpace(justification))
            throw new ArgumentException("Une justification est obligatoire pour retenir un niveau de risque initial différent du calcul.", nameof(justification));

        NiveauRisqueInitialRetenu = niveauRetenu;
        JustificationNiveauRisqueInitial = justification.Trim();
    }

    /// <summary>Revient à la valeur calculée, efface l'écart de l'analyste.</summary>
    public void ReinitialiserNiveauRisqueInitial()
    {
        NiveauRisqueInitialRetenu = null;
        JustificationNiveauRisqueInitial = null;
    }

    /// <summary>
    /// Enregistre l'évaluation du risque résiduel (après application des
    /// mesures du plan de traitement). Le niveau est calculé en amont par
    /// ServiceCalculNiveauRisque, jamais ici -- aucune valeur dérivée n'est
    /// jamais calculée en dehors du Core Engine.
    /// </summary>
    public void EvaluerRisqueResiduel(int graviteResiduelle, NiveauVraisemblance vraisemblanceResiduelle, NiveauRisque niveauRisqueResiduelCalcule)
    {
        if (graviteResiduelle < EvenementRedoute.GraviteMin || graviteResiduelle > EvenementRedoute.GraviteMax)
            throw new ArgumentOutOfRangeException(
                nameof(graviteResiduelle), graviteResiduelle,
                $"La gravité résiduelle doit être comprise entre {EvenementRedoute.GraviteMin} et {EvenementRedoute.GraviteMax} (échelle EBIOS RM).");

        GraviteResiduelle = graviteResiduelle;
        VraisemblanceResiduelle = vraisemblanceResiduelle;
        NiveauRisqueResiduelCalcule = niveauRisqueResiduelCalcule;
        // L'override (NiveauRisqueResiduelRetenu/JustificationNiveauRisqueResiduel),
        // s'il existe, n'est jamais effacé par une réévaluation des entrées --
        // sticky jusqu'à DefinirNiveauRisqueResiduelRetenu/ReinitialiserNiveauRisqueResiduel explicites.
    }

    /// <summary>Même principe que DefinirNiveauRisqueInitialRetenu, pour le niveau résiduel.</summary>
    public void DefinirNiveauRisqueResiduelRetenu(NiveauRisque niveauRetenu, string justification)
    {
        if (string.IsNullOrWhiteSpace(justification))
            throw new ArgumentException("Une justification est obligatoire pour retenir un niveau de risque résiduel différent du calcul.", nameof(justification));

        NiveauRisqueResiduelRetenu = niveauRetenu;
        JustificationNiveauRisqueResiduel = justification.Trim();
    }

    public void ReinitialiserNiveauRisqueResiduel()
    {
        NiveauRisqueResiduelRetenu = null;
        JustificationNiveauRisqueResiduel = null;
    }

    /// <summary>
    /// Formalise l'acceptation du risque résiduel par la Direction. Exige que
    /// le risque résiduel ait déjà été évalué. Un risque résiduel Élevé exige
    /// en plus un sponsor exécutif nommé et une justification écrite
    /// (doctrine officielle + ISO/CEI 27005:2022 : l'acceptation d'un risque
    /// élevé sans traitement complémentaire doit être documentée).
    /// </summary>
    public void AccepterRisqueResiduel(string nomProprietaireRisque, string nomValidateurSecurite, string? nomSponsorExecutif, string? justification)
    {
        if (NiveauRisqueResiduel is null)
            throw new InvalidOperationException("Le risque résiduel doit être évalué avant toute acceptation formelle.");
        if (string.IsNullOrWhiteSpace(nomProprietaireRisque))
            throw new ArgumentException("Le propriétaire du risque est obligatoire pour formaliser l'acceptation.", nameof(nomProprietaireRisque));
        if (string.IsNullOrWhiteSpace(nomValidateurSecurite))
            throw new ArgumentException("Le validateur sécurité est obligatoire pour formaliser l'acceptation.", nameof(nomValidateurSecurite));
        if (NiveauRisqueResiduel == NiveauRisque.Eleve && (string.IsNullOrWhiteSpace(nomSponsorExecutif) || string.IsNullOrWhiteSpace(justification)))
            throw new ArgumentException(
                "Un risque résiduel élevé exige un sponsor exécutif nommé et une justification écrite (doctrine EBIOS RM, ISO/CEI 27005:2022).",
                nameof(nomSponsorExecutif));

        NomProprietaireRisque = nomProprietaireRisque.Trim();
        NomValidateurSecurite = nomValidateurSecurite.Trim();
        NomSponsorExecutif = string.IsNullOrWhiteSpace(nomSponsorExecutif) ? null : nomSponsorExecutif.Trim();
        JustificationAcceptation = string.IsNullOrWhiteSpace(justification) ? null : justification.Trim();
        AccepteParDirection = true;
        DateAcceptationUtc = DateTime.UtcNow;
    }

    /// <summary>Retire une acceptation déjà formalisée -- utilisé si le risque résiduel est réévalué.</summary>
    public void RetirerAcceptation()
    {
        AccepteParDirection = false;
        NomProprietaireRisque = null;
        NomValidateurSecurite = null;
        NomSponsorExecutif = null;
        JustificationAcceptation = null;
        DateAcceptationUtc = null;
    }
}
