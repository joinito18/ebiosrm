using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;
using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

namespace EbiosRM.Api.Modules.Reporting;

/// <summary>
/// Assemble le cadre de suivi à partir de l'état courant (pas d'un snapshot,
/// cf. RapportCadreDeSuiviData) : nécessite qu'un plan de traitement existe
/// (Atelier 5 au moins démarré et un plan créé), pas que l'étude soit
/// entièrement validée -- le suivi a justement vocation à être consulté
/// pendant que les mesures avancent, avant la clôture formelle.
/// </summary>
public sealed class RapportCadreDeSuiviService
{
    private readonly IEtudeRepository _etudeRepository;
    private readonly IPlanTraitementRisqueRepository _planRepository;
    private readonly ServiceAssemblageScenariosDeRisque _assemblageScenarios;

    public RapportCadreDeSuiviService(
        IEtudeRepository etudeRepository,
        IPlanTraitementRisqueRepository planRepository,
        ServiceAssemblageScenariosDeRisque assemblageScenarios)
    {
        _etudeRepository = etudeRepository;
        _planRepository = planRepository;
        _assemblageScenarios = assemblageScenarios;
    }

    public async Task<RapportCadreDeSuiviData?> ConstruireAsync(Guid etudeId, CancellationToken cancellationToken, bool anglais = false)
    {
        var etude = await _etudeRepository.ObtenirParIdAsync(etudeId, cancellationToken);
        if (etude is null)
            return null;

        var plan = await _planRepository.ObtenirParEtudeAsync(etudeId, cancellationToken);
        if (plan is null)
            return null;

        var vues = await _assemblageScenarios.ListerAsync(etudeId, cancellationToken);
        var libellesParScenario = vues.ToDictionary(v => v.Id, v => v.LibelleCouple + " -- " + v.LibelleChemin);

        var scenarios = vues.Select(v => new ScenarioDeRisqueData(
            v.LibelleChemin, v.LibelleCouple, v.Gravite, v.VraisemblanceInitiale?.ToString(),
            v.NiveauRisqueInitial?.ToString(), v.NiveauRisqueInitialRetenu.HasValue, v.JustificationNiveauRisqueInitial,
            v.GraviteResiduelle, v.VraisemblanceResiduelle?.ToString(),
            v.NiveauRisqueResiduel?.ToString(), v.NiveauRisqueResiduelRetenu.HasValue, v.JustificationNiveauRisqueResiduel,
            v.ClasseAcceptationResiduelle?.ToString(),
            v.AccepteParDirection, v.NomProprietaireRisque, v.NomValidateurSecurite, v.NomSponsorExecutif,
            v.JustificationAcceptation, v.DateAcceptationUtc)).ToList();

        var mesures = plan.Mesures.Select(m => new MesureTraitementRisqueData(
            m.Description, m.Axe.ToString(),
            m.ScenariosDeRisqueIds.Select(id => libellesParScenario.GetValueOrDefault(id, "(scénario supprimé)")).ToList(),
            m.Responsable, m.FreinsEtDifficultes, m.CoutComplexite.LibelleAvecMot(anglais), m.Echeance, m.Statut.ToString())).ToList();

        var avancement = plan.Mesures
            .GroupBy(m => m.Statut.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        return new RapportCadreDeSuiviData(etude.Nom, etude.Perimetre, DateTime.UtcNow, scenarios, mesures, avancement);
    }
}
