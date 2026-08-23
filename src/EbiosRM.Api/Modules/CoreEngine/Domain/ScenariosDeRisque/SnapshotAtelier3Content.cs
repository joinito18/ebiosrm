namespace EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

// DTO de sérialisation pur — volontairement découplé des entités EF Core,
// même principe que SnapshotAtelier1Content. Garde les références par Id
// (pas de libellés pré-résolus) : le service de rapport fait la même
// jointure qu'aujourd'hui, juste depuis le contenu figé au lieu des agrégats
// vivants -- changement minimal de la logique déjà éprouvée.
public record SnapshotAtelier3Content(
    Guid EtudeId,
    int Version,
    string NomEtude,
    DateTime DateValidationUtc,
    List<PartiePrenanteDangerositeSnapshot> PartiesPrenantes,
    List<ScenarioStrategiqueSnapshot> ScenariosStrategiques,
    List<CoupleSrOvResumeSnapshot> Couples,
    List<EvenementRedouteResumeSnapshot> EvenementsRedoutes,
    List<ValeurMetierResumeSnapshot> ValeursMetier
);

public record PartiePrenanteDangerositeSnapshot(
    Guid Id,
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

public record ScenarioStrategiqueSnapshot(
    Guid Id,
    Guid CoupleSourceRisqueObjectifViseId,
    Guid EvenementRedouteId,
    string Description,
    List<CheminAttaqueSnapshot> CheminsAttaque);

public record CheminAttaqueSnapshot(string Description, List<EvenementIntermediaireSnapshot> EvenementsIntermediaires);

public record EvenementIntermediaireSnapshot(Guid PartiePrenanteId, string Description);

public record CoupleSrOvResumeSnapshot(
    Guid Id,
    string SourceRisque,
    string DescriptionSourceRisque,
    string ObjectifVise,
    string DescriptionObjectifVise,
    string Pertinence);

public record EvenementRedouteResumeSnapshot(Guid Id, Guid ValeurMetierId, string Description, int Gravite);

public record ValeurMetierResumeSnapshot(Guid Id, string Description);
