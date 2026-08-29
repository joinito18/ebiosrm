namespace EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

// DTO de sérialisation pur -- même principe que SnapshotAtelier3/4Content :
// libellés déjà résolus (le scénario de risque assemble des données de
// plusieurs agrégats vivants, cf. ServiceAssemblageScenariosDeRisque), pas de
// nouvelle jointure requise au moment du reporting.
public record SnapshotAtelier5Content(
    Guid EtudeId,
    int Version,
    string NomEtude,
    DateTime DateValidationUtc,
    List<ScenarioDeRisqueSnapshot> ScenariosDeRisque,
    List<MesureTraitementRisqueSnapshot> Mesures
);

public record ScenarioDeRisqueSnapshot(
    Guid Id,
    string LibelleChemin,
    string LibelleCouple,
    int Gravite,
    NiveauVraisemblance? VraisemblanceInitiale,
    NiveauRisque? NiveauRisqueInitial,
    bool NiveauInitialEstJugementExpert,
    string? JustificationNiveauRisqueInitial,
    ClasseAcceptation? ClasseAcceptationInitiale,
    int? GraviteResiduelle,
    NiveauVraisemblance? VraisemblanceResiduelle,
    NiveauRisque? NiveauRisqueResiduel,
    bool NiveauResiduelEstJugementExpert,
    string? JustificationNiveauRisqueResiduel,
    ClasseAcceptation? ClasseAcceptationResiduelle,
    bool AccepteParDirection,
    string? NomProprietaireRisque,
    string? NomValidateurSecurite,
    string? NomSponsorExecutif,
    string? JustificationAcceptation,
    DateTime? DateAcceptationUtc);

public record MesureTraitementRisqueSnapshot(
    Guid Id,
    string Description,
    AxeMesure Axe,
    List<Guid> ScenariosDeRisqueIds,
    string Responsable,
    string? FreinsEtDifficultes,
    NiveauCoutComplexite CoutComplexite,
    string? Echeance,
    StatutMesure Statut,
    List<string>? CodesConformite = null);
