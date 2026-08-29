namespace EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

// DTO de sérialisation pur — même principe que SnapshotAtelier3Content :
// références par Id conservées, le service de rapport fait la même jointure
// qu'aujourd'hui depuis le contenu figé.
public record SnapshotAtelier4Content(
    Guid EtudeId,
    int Version,
    string NomEtude,
    DateTime DateValidationUtc,
    List<ScenarioOperationnelSnapshot> ScenariosOperationnels,
    List<CheminAttaqueResumeSnapshot> CheminsAttaque,
    List<ScenarioStrategiqueResumeSnapshot> ScenariosStrategiques,
    List<CoupleSrOvResumeSnapshot> Couples,
    List<BienSupportResumeSnapshot> BiensSupport
);

public record ScenarioOperationnelSnapshot(
    Guid CheminAttaqueId,
    NiveauVraisemblance? VraisemblanceGlobale,
    List<ModeOperatoireSnapshot> ModesOperatoires);

public record ModeOperatoireSnapshot(
    string Description,
    List<ActionElementaireSnapshotContenu> ActionsElementaires,
    int ProbabiliteSucces,
    int DifficulteTechnique,
    NiveauVraisemblance Vraisemblance,
    bool VraisemblanceEstJugementExpert,
    string? JustificationVraisemblance);

public record ActionElementaireSnapshotContenu(string Description, PhaseActionElementaire Phase, Guid BienSupportId, string? TechniqueMitre = null);

public record CheminAttaqueResumeSnapshot(Guid Id, string Description, Guid ScenarioStrategiqueId);

public record ScenarioStrategiqueResumeSnapshot(Guid Id, Guid CoupleSourceRisqueObjectifViseId);

public record BienSupportResumeSnapshot(Guid Id, string Description);
