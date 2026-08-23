using System.Text.Json;
using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;
using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

namespace EbiosRM.Api.Modules.Reporting;

/// <summary>
/// Assemble les données du rapport Atelier 5 en lisant EXCLUSIVEMENT le dernier
/// SnapshotAtelier (P16 : le Reporting ne lit jamais un état en cours d'édition).
/// Même patron que RapportAtelier1/2/3/4Service.
/// </summary>
public sealed class RapportAtelier5Service
{
    private readonly ISnapshotAtelierRepository _snapshotRepository;

    public RapportAtelier5Service(ISnapshotAtelierRepository snapshotRepository)
    {
        _snapshotRepository = snapshotRepository;
    }

    public async Task<RapportAtelier5Data?> ConstruireAsync(Guid etudeId, CancellationToken cancellationToken)
    {
        var snapshot = await _snapshotRepository.ObtenirDernierParEtudeIdAsync(etudeId, numeroAtelier: 5, cancellationToken);
        if (snapshot is null)
            return null;

        var contenu = JsonSerializer.Deserialize<SnapshotAtelier5Content>(snapshot.ContenuJson);
        if (contenu is null)
            return null;

        var libellesParScenario = contenu.ScenariosDeRisque.ToDictionary(s => s.Id, s => s.LibelleChemin + " -- " + s.LibelleCouple);

        var scenarios = contenu.ScenariosDeRisque.Select(s => new ScenarioDeRisqueData(
            s.LibelleChemin, s.LibelleCouple, s.Gravite, s.VraisemblanceInitiale?.ToString(),
            s.NiveauRisqueInitial?.ToString(), s.NiveauInitialEstJugementExpert, s.JustificationNiveauRisqueInitial,
            s.GraviteResiduelle, s.VraisemblanceResiduelle?.ToString(),
            s.NiveauRisqueResiduel?.ToString(), s.NiveauResiduelEstJugementExpert, s.JustificationNiveauRisqueResiduel,
            s.ClasseAcceptationResiduelle?.ToString(),
            s.AccepteParDirection, s.NomProprietaireRisque, s.NomValidateurSecurite, s.NomSponsorExecutif,
            s.JustificationAcceptation, s.DateAcceptationUtc)).ToList();

        var mesures = contenu.Mesures.Select(m => new MesureTraitementRisqueData(
            m.Description, m.Axe.ToString(),
            m.ScenariosDeRisqueIds.Select(id => libellesParScenario.GetValueOrDefault(id, "(scénario supprimé)")).ToList(),
            m.Responsable, m.FreinsEtDifficultes, m.CoutComplexite.LibelleAvecMot(), m.Echeance, m.Statut.ToString())).ToList();

        return new RapportAtelier5Data(contenu.NomEtude, scenarios, mesures);
    }
}
