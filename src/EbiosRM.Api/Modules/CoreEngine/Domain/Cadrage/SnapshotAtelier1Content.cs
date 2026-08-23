namespace EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

// DTO de sérialisation pur — volontairement découplé des entités EF Core
// pour ne pas figer le contrat JSON aux détails de mapping (owned types, etc.)
public record SnapshotAtelier1Content(
    Guid EtudeId,
    int Version,
    string NomEtude,
    string Perimetre,
    string Mission,
    string StatutEtude,
    DateTime DateValidationUtc,
    List<ValeurMetierSnapshot> ValeursMetier,
    List<BienSupportSnapshot> BiensSupport,
    List<EvenementRedouteSnapshot> EvenementsRedoutes,
    SocleSecuriteSnapshot? SocleSecurite
);

public record ValeurMetierSnapshot(Guid Id, string Description, string EntiteProprietaire);
public record BienSupportSnapshot(Guid Id, Guid ValeurMetierId, string Description, string Type, string EntiteProprietaire);
public record EvenementRedouteSnapshot(Guid Id, Guid ValeurMetierId, string Description, int Gravite);
public record ReferentielApplicableSnapshot(Guid Id, string Nom, string EtatConformite, string? Theme, string? CodeControle, string? EtatActuel);
public record SocleSecuriteSnapshot(Guid Id, List<ReferentielApplicableSnapshot> Referentiels);
