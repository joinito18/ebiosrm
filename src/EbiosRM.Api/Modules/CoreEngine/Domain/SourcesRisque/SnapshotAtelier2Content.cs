namespace EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;

// DTO de sérialisation pur — volontairement découplé des entités EF Core,
// même principe que SnapshotAtelier1Content (Domain/Cadrage).
public record SnapshotAtelier2Content(
    Guid EtudeId,
    int Version,
    string NomEtude,
    DateTime DateValidationUtc,
    List<PartiePrenanteSnapshot> PartiesPrenantes,
    List<CoupleSrOvSnapshot> Couples
);

public record PartiePrenanteSnapshot(string Nom, string RolesEtAttentes, string Representant);

public record CoupleSrOvSnapshot(
    string SourceRisque,
    string DescriptionSourceRisque,
    string ObjectifVise,
    string DescriptionObjectifVise,
    string ContexteVulnerabilite,
    string Theme,
    int Motivation,
    int Ressources,
    string Pertinence,
    bool PertinenceEstJugementExpert,
    string? JustificationPertinence);
