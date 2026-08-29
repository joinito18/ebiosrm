using System.Text.Json;
using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;
using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;
using EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;

namespace EbiosRM.Api.Modules.Reporting;

/// <summary>
/// Assemble les données du rapport Atelier 3 en lisant EXCLUSIVEMENT le dernier
/// SnapshotAtelier (P16 : le Reporting ne lit jamais un état en cours d'édition).
/// Même patron que RapportAtelier1Service/RapportAtelier2Service. La logique de
/// jointure (couples/événements/valeurs par Id) est inchangée -- seule la
/// source des données change (snapshot au lieu des agrégats vivants).
/// </summary>
public sealed class RapportAtelier3Service
{
    private readonly ISnapshotAtelierRepository _snapshotRepository;

    public RapportAtelier3Service(ISnapshotAtelierRepository snapshotRepository)
    {
        _snapshotRepository = snapshotRepository;
    }

    public async Task<RapportAtelier3Data?> ConstruireAsync(Guid etudeId, CancellationToken cancellationToken, bool anglais = false)
    {
        var snapshot = await _snapshotRepository.ObtenirDernierParEtudeIdAsync(etudeId, numeroAtelier: 3, cancellationToken);
        if (snapshot is null)
            return null;

        var contenu = JsonSerializer.Deserialize<SnapshotAtelier3Content>(snapshot.ContenuJson);
        if (contenu is null)
            return null;

        var partiesParId = contenu.PartiesPrenantes.ToDictionary(p => p.Id);
        var couplesParId = contenu.Couples.ToDictionary(c => c.Id);
        var evenementsParId = contenu.EvenementsRedoutes.ToDictionary(e => e.Id);
        var valeursParId = contenu.ValeursMetier.ToDictionary(v => v.Id);

        var partiesData = contenu.PartiesPrenantes
            .Select(p => new PartiePrenanteDangerositeData(
                p.Nom, p.RolesEtAttentes, p.Representant, p.LibelleCategorie,
                p.Dependance, p.Penetration, p.MaturiteCyber, p.Confiance, p.NiveauDangerosite,
                p.Zone,
                p.DangerositeEstJugementExpert, p.JustificationDangerosite,
                p.Mesures,
                p.NiveauDangerositeResiduel, p.ZoneResiduelle,
                p.DangerositeResiduelleEstJugementExpert, p.JustificationDangerositeResiduelle))
            .ToList();

        var scenariosData = new List<ScenarioStrategiqueData>();
        foreach (var s in contenu.ScenariosStrategiques)
        {
            couplesParId.TryGetValue(s.CoupleSourceRisqueObjectifViseId, out var couple);
            var libelleSr = couple is null ? "?" : (couple.SourceRisque == "Autre" ? couple.DescriptionSourceRisque : LibellesSourceRisqueObjectifVise.SourceRisque(couple.SourceRisque, anglais));
            var libelleOv = couple is null ? "?" : (couple.ObjectifVise == "Autre" ? couple.DescriptionObjectifVise : LibellesSourceRisqueObjectifVise.ObjectifVise(couple.ObjectifVise, anglais));
            var pertinence = couple?.Pertinence ?? "";

            evenementsParId.TryGetValue(s.EvenementRedouteId, out var er);
            var gravite = er?.Gravite ?? 0;
            var libelleEr = "?";
            if (er is not null)
            {
                valeursParId.TryGetValue(er.ValeurMetierId, out var vm);
                libelleEr = (vm?.Description ?? "?") + " -- " + er.Description;
            }

            var cheminsData = s.CheminsAttaque.Select(c => new CheminAttaqueData(
                c.Description,
                c.EvenementsIntermediaires.Select(ei =>
                {
                    partiesParId.TryGetValue(ei.PartiePrenanteId, out var pp);
                    return new EvenementIntermediaireData(pp?.Nom ?? "?", ei.Description);
                }).ToList()
            )).ToList();

            scenariosData.Add(new ScenarioStrategiqueData(libelleSr, libelleOv, pertinence, libelleEr, gravite, s.Description, cheminsData));
        }

        return new RapportAtelier3Data(contenu.NomEtude, partiesData, scenariosData);
    }
}
