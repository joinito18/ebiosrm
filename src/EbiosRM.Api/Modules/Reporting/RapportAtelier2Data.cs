using EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;

namespace EbiosRM.Api.Modules.Reporting;

public sealed record RapportAtelier2Data(
    string NomEtude,
    List<PartiePrenanteData> PartiesPrenantes,
    List<CoupleSrOvData> CouplesTechnologique,
    List<CoupleSrOvData> CouplesOrganisationnel,
    List<CoupleSrOvData> CouplesPersonnes,
    List<CoupleSrOvData> CouplesPhysique,
    RepartitionPertinenceData Repartition);

public sealed record PartiePrenanteData(
    string Nom,
    string RolesEtAttentes,
    string Representant);

public sealed record CoupleSrOvData(
    string SourceRisque,
    string ObjectifVise,
    string ContexteVulnerabilite,
    int Motivation,
    int Ressources,
    string Pertinence,
    bool PertinenceEstJugementExpert,
    string? JustificationPertinence)
{
    /// <summary>Libellé affichable : la catégorie, sauf pour "Autre" où la description libre est plus parlante.</summary>
    public string LibelleSourceRisque => SourceRisque == "Autre" ? "Autre : " + DescriptionSourceRisque : LibellesSourceRisqueObjectifVise.SourceRisque(SourceRisque);
    public string LibelleObjectifVise => ObjectifVise == "Autre" ? "Autre : " + DescriptionObjectifVise : LibellesSourceRisqueObjectifVise.ObjectifVise(ObjectifVise);
    public string DescriptionSourceRisque { get; init; } = "";
    public string DescriptionObjectifVise { get; init; } = "";
}

public sealed record NiveauPertinenceData(
    string Niveau,
    int Nombre,
    double Pourcentage);

public sealed record RepartitionPertinenceData(
    List<NiveauPertinenceData> Niveaux,
    int TotalCouples);
