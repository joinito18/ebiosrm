using System.Text.Json;
using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;
using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;
using EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;

namespace EbiosRM.Api.Modules.Reporting;

/// <summary>
/// Assemble le rapport de synthèse globale (document présenté à la Direction
/// en fin d'étude) en lisant EXCLUSIVEMENT les 5 SnapshotAtelier (P16) --
/// jamais un état en cours d'édition. Distinct du rapport d'Atelier 5 :
/// consolide les 5 ateliers, pas seulement le dernier. Retourne null si un
/// seul des 5 snapshots manque -- l'étude doit avoir traversé les 5 ateliers.
/// </summary>
public sealed class RapportSyntheseGlobaleService
{
    private readonly ISnapshotAtelierRepository _snapshotRepository;

    public RapportSyntheseGlobaleService(ISnapshotAtelierRepository snapshotRepository)
    {
        _snapshotRepository = snapshotRepository;
    }

    public async Task<RapportSyntheseGlobaleData?> ConstruireAsync(Guid etudeId, CancellationToken cancellationToken)
    {
        var snapshot1 = await _snapshotRepository.ObtenirDernierParEtudeIdAsync(etudeId, numeroAtelier: 1, cancellationToken);
        var snapshot2 = await _snapshotRepository.ObtenirDernierParEtudeIdAsync(etudeId, numeroAtelier: 2, cancellationToken);
        var snapshot3 = await _snapshotRepository.ObtenirDernierParEtudeIdAsync(etudeId, numeroAtelier: 3, cancellationToken);
        var snapshot4 = await _snapshotRepository.ObtenirDernierParEtudeIdAsync(etudeId, numeroAtelier: 4, cancellationToken);
        var snapshot5 = await _snapshotRepository.ObtenirDernierParEtudeIdAsync(etudeId, numeroAtelier: 5, cancellationToken);
        if (snapshot1 is null || snapshot2 is null || snapshot3 is null || snapshot4 is null || snapshot5 is null)
            return null;

        var contenu1 = JsonSerializer.Deserialize<SnapshotAtelier1Content>(snapshot1.ContenuJson);
        var contenu2 = JsonSerializer.Deserialize<SnapshotAtelier2Content>(snapshot2.ContenuJson);
        var contenu3 = JsonSerializer.Deserialize<SnapshotAtelier3Content>(snapshot3.ContenuJson);
        var contenu4 = JsonSerializer.Deserialize<SnapshotAtelier4Content>(snapshot4.ContenuJson);
        var contenu5 = JsonSerializer.Deserialize<SnapshotAtelier5Content>(snapshot5.ContenuJson);
        if (contenu1 is null || contenu2 is null || contenu3 is null || contenu4 is null || contenu5 is null)
            return null;

        var chiffresCles = new ChiffresClesData(
            contenu1.ValeursMetier.Count,
            contenu1.BiensSupport.Count,
            contenu1.EvenementsRedoutes.Count,
            contenu2.PartiesPrenantes.Count,
            contenu3.PartiesPrenantes.Count(p => p.Zone == "Controle" || p.Zone == "Danger"),
            contenu3.ScenariosStrategiques.Count,
            contenu4.ScenariosOperationnels.Count);

        var libellesParScenario = contenu5.ScenariosDeRisque.ToDictionary(s => s.Id, s => s.LibelleCouple + " -- " + s.LibelleChemin);

        var scenarios = contenu5.ScenariosDeRisque.Select(s => new ScenarioDeRisqueData(
            s.LibelleChemin, s.LibelleCouple, s.Gravite, s.VraisemblanceInitiale?.ToString(),
            s.NiveauRisqueInitial?.ToString(), s.NiveauInitialEstJugementExpert, s.JustificationNiveauRisqueInitial,
            s.GraviteResiduelle, s.VraisemblanceResiduelle?.ToString(),
            s.NiveauRisqueResiduel?.ToString(), s.NiveauResiduelEstJugementExpert, s.JustificationNiveauRisqueResiduel,
            s.ClasseAcceptationResiduelle?.ToString(),
            s.AccepteParDirection, s.NomProprietaireRisque, s.NomValidateurSecurite, s.NomSponsorExecutif,
            s.JustificationAcceptation, s.DateAcceptationUtc)).ToList();

        var mesures = contenu5.Mesures.Select(m => new MesureTraitementRisqueData(
            m.Description, m.Axe.ToString(),
            m.ScenariosDeRisqueIds.Select(id => libellesParScenario.GetValueOrDefault(id, "(scénario supprimé)")).ToList(),
            m.Responsable, m.FreinsEtDifficultes, m.CoutComplexite.LibelleAvecMot(), m.Echeance, m.Statut.ToString())).ToList();

        var avancement = contenu5.Mesures
            .GroupBy(m => m.Statut.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var referentiels = contenu1.SocleSecurite?.Referentiels ?? new List<ReferentielApplicableSnapshot>();
        var conformiteSocle = ConformiteSocleData.DepuisReferentiels(referentiels);

        return new RapportSyntheseGlobaleData(
            contenu1.NomEtude, contenu1.Perimetre, contenu1.Mission, DateTime.UtcNow,
            chiffresCles, scenarios, mesures, avancement, conformiteSocle);
    }
}
