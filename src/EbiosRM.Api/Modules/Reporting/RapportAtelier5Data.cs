namespace EbiosRM.Api.Modules.Reporting;

public sealed record RapportAtelier5Data(
    string NomEtude,
    string Perimetre,
    string Mission,
    ChiffresClesData ChiffresCles,
    ConformiteSocleData ConformiteSocle,
    List<ScenarioDeRisqueData> ScenariosDeRisque,
    List<MesureTraitementRisqueData> Mesures);

public sealed record ScenarioDeRisqueData(
    string LibelleChemin,
    string LibelleCouple,
    int Gravite,
    string? VraisemblanceInitiale,
    string? NiveauRisqueInitial,
    bool NiveauInitialEstJugementExpert,
    string? JustificationNiveauRisqueInitial,
    int? GraviteResiduelle,
    string? VraisemblanceResiduelle,
    string? NiveauRisqueResiduel,
    bool NiveauResiduelEstJugementExpert,
    string? JustificationNiveauRisqueResiduel,
    string? ClasseAcceptationResiduelle,
    bool AccepteParDirection,
    string? NomProprietaireRisque,
    string? NomValidateurSecurite,
    string? NomSponsorExecutif,
    string? JustificationAcceptation,
    DateTime? DateAcceptationUtc);

public sealed record MesureTraitementRisqueData(
    string Description,
    string Axe,
    List<string> LibellesScenariosDeRisque,
    string Responsable,
    string? FreinsEtDifficultes,
    string CoutComplexite,
    string? Echeance,
    string Statut);
