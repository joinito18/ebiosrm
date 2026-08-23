namespace EbiosRM.Api.Modules.Reporting;

public sealed record RapportAtelier3Data(
    string NomEtude,
    List<PartiePrenanteDangerositeData> PartiesPrenantes,
    List<ScenarioStrategiqueData> ScenariosStrategiques);

public sealed record PartiePrenanteDangerositeData(
    string Nom,
    string RolesEtAttentes,
    string Representant,
    string LibelleCategorie,
    int? Dependance,
    int? Penetration,
    int? MaturiteCyber,
    int? Confiance,
    double? NiveauDangerosite,
    string? Zone,
    bool DangerositeEstJugementExpert,
    string? JustificationDangerosite,
    List<string> Mesures,
    double? NiveauDangerositeResiduel,
    string? ZoneResiduelle,
    bool DangerositeResiduelleEstJugementExpert,
    string? JustificationDangerositeResiduelle);

public sealed record ScenarioStrategiqueData(
    string LibelleSourceRisque,
    string LibelleObjectifVise,
    string Pertinence,
    string LibelleEvenementRedoute,
    int Gravite,
    string Description,
    List<CheminAttaqueData> CheminsAttaque);

public sealed record CheminAttaqueData(
    string Description,
    List<EvenementIntermediaireData> EvenementsIntermediaires);

public sealed record EvenementIntermediaireData(
    string LibellePartiePrenante,
    string Description);
