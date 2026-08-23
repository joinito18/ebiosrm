namespace EbiosRM.Api.Modules.Reporting;

public sealed record RapportAtelier4Data(
    string NomEtude,
    List<ScenarioOperationnelData> ScenariosOperationnels);

public sealed record ScenarioOperationnelData(
    string LibelleCouple,
    string LibelleCheminAttaque,
    string? VraisemblanceGlobale,
    List<ModeOperatoireData> ModesOperatoires);

public sealed record ModeOperatoireData(
    string Description,
    List<ActionElementaireData> ActionsElementaires,
    int ProbabiliteSucces,
    int DifficulteTechnique,
    string Vraisemblance,
    bool VraisemblanceEstJugementExpert,
    string? JustificationVraisemblance);

public sealed record ActionElementaireData(
    string Description,
    string Phase,
    string LibelleBienSupport);
