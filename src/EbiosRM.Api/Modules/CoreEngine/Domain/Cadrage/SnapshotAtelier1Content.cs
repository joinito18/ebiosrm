namespace EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

// DTO de sérialisation pur — volontairement découplé des entités EF Core
// pour ne pas figer le contrat JSON aux détails de mapping (owned types, etc.)
public record SnapshotAtelier1Content(
    Guid EtudeId,
    string NomEtude,
    string Perimetre,
    string StatutEtude,
    DateTime DateValidationUtc,
    List<ValeurMetierSnapshot> ValeursMetier,
    List<BienSupportSnapshot> BiensSupport,
    List<EvenementRedouteSnapshot> EvenementsRedoutes,
    SocleSecuriteSnapshot? SocleSecurite
);

public record ValeurMetierSnapshot(Guid Id, string Description, string EntiteResponsable);
public record BienSupportSnapshot(Guid Id, Guid ValeurMetierId, string Description, string Type, string EntiteResponsable);
public record EvenementRedouteSnapshot(Guid Id, Guid ValeurMetierId, string Description, int Gravite);
public record ReferentielApplicableSnapshot(Guid Id, string Nom, string EtatConformite);
public record SocleSecuriteSnapshot(Guid Id, List<ReferentielApplicableSnapshot> Referentiels);
