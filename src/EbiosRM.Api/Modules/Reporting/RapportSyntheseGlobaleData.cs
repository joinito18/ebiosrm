namespace EbiosRM.Api.Modules.Reporting;

public sealed record RapportSyntheseGlobaleData(
    string NomEtude,
    string Perimetre,
    string Mission,
    DateTime DateSynthese,
    ChiffresClesData ChiffresCles,
    List<ScenarioDeRisqueData> ScenariosDeRisque,
    List<MesureTraitementRisqueData> Mesures,
    Dictionary<string, int> AvancementPlanParStatut);

public sealed record ChiffresClesData(
    int NombreValeursMetier,
    int NombreBiensSupport,
    int NombreEvenementsRedoutes,
    int NombrePartiesPrenantes,
    int NombrePartiesPrenantesCritiques,
    int NombreScenariosStrategiques,
    int NombreScenariosOperationnels);
