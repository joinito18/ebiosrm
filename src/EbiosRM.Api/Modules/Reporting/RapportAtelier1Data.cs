namespace EbiosRM.Api.Modules.Reporting;

public sealed record RapportAtelier1Data(
    string NomEtude,
    string Perimetre,
    string Statut,
    DateTime DateValidationUtc,
    List<ValeurMetierData> ValeursMetier,
    List<EvenementRedouteData> EvenementsRedoutes,
    List<ReferentielApplicableData> ReferentielsApplicables);

public sealed record ValeurMetierData(
    string Description,
    string EntiteResponsable,
    List<BienSupportData> BiensSupport);

public sealed record BienSupportData(
    string Description,
    string Type,
    string EntiteResponsable);

public sealed record EvenementRedouteData(
    string ValeurMetierDescription,
    string Description,
    int Gravite);

public sealed record ReferentielApplicableData(
    string Nom,
    string Etat);
