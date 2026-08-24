namespace EbiosRM.Api.Modules.Reporting;

public sealed record RapportSyntheseGlobaleData(
    string NomEtude,
    string Perimetre,
    string Mission,
    DateTime DateSynthese,
    ChiffresClesData ChiffresCles,
    List<ScenarioDeRisqueData> ScenariosDeRisque,
    List<MesureTraitementRisqueData> Mesures,
    Dictionary<string, int> AvancementPlanParStatut,
    ConformiteSocleData ConformiteSocle);

public sealed record ConformiteSocleData(
    int NombreConforme,
    int NombreNonConforme,
    int NombreNonApplicable,
    List<ControleNonConformeData> ControlesNonConformes);

public sealed record ControleNonConformeData(string? CodeControle, string Nom, string? EtatActuel);

public sealed record ChiffresClesData(
    int NombreValeursMetier,
    int NombreBiensSupport,
    int NombreEvenementsRedoutes,
    int NombrePartiesPrenantes,
    int NombrePartiesPrenantesCritiques,
    int NombreScenariosStrategiques,
    int NombreScenariosOperationnels);
