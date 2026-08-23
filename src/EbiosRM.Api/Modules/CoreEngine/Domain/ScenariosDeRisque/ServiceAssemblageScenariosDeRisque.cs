using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;
using EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;

namespace EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

/// <summary>
/// Vue plate d'un scénario de risque avec toutes les données déjà résolues :
/// Gravité et Vraisemblance initiales viennent d'agrégats externes (P8,
/// jamais dupliquées sur ScenarioDeRisque) et sont jointes ici à la volée.
/// </summary>
public sealed record ScenarioDeRisqueVue(
    Guid Id,
    Guid CheminAttaqueId,
    string LibelleChemin,
    string LibelleCouple,
    int Gravite,
    NiveauVraisemblance? VraisemblanceInitiale,
    NiveauRisque? NiveauRisqueInitialCalcule,
    NiveauRisque? NiveauRisqueInitialRetenu,
    string? JustificationNiveauRisqueInitial,
    NiveauRisque? NiveauRisqueInitial,
    ClasseAcceptation? ClasseAcceptationInitiale,
    int? GraviteResiduelle,
    NiveauVraisemblance? VraisemblanceResiduelle,
    NiveauRisque? NiveauRisqueResiduelCalcule,
    NiveauRisque? NiveauRisqueResiduelRetenu,
    string? JustificationNiveauRisqueResiduel,
    NiveauRisque? NiveauRisqueResiduel,
    ClasseAcceptation? ClasseAcceptationResiduelle,
    bool AccepteParDirection,
    string? NomProprietaireRisque,
    string? NomValidateurSecurite,
    string? NomSponsorExecutif,
    string? JustificationAcceptation,
    DateTime? DateAcceptationUtc);

/// <summary>
/// Domain Service : assemble la vue complète d'un scénario de risque en
/// joignant, à la volée, les agrégats externes dont il dépend (CheminAttaque
/// -> ScenarioStrategique -> EvenementRedoute pour la Gravité, ScenarioOperationnel
/// pour la Vraisemblance). Logique partagée entre l'endpoint GET et
/// ServiceCreationSnapshotAtelier5, extraite ici pour ne pas être dupliquée.
/// </summary>
public sealed class ServiceAssemblageScenariosDeRisque
{
    private readonly IScenarioDeRisqueRepository _scenarioDeRisqueRepository;
    private readonly ICheminAttaqueRepository _cheminAttaqueRepository;
    private readonly IScenarioStrategiqueRepository _scenarioStrategiqueRepository;
    private readonly IEvenementRedouteRepository _evenementRedouteRepository;
    private readonly IScenarioOperationnelRepository _scenarioOperationnelRepository;
    private readonly ICoupleSourceRisqueObjectifViseRepository _coupleRepository;

    public ServiceAssemblageScenariosDeRisque(
        IScenarioDeRisqueRepository scenarioDeRisqueRepository,
        ICheminAttaqueRepository cheminAttaqueRepository,
        IScenarioStrategiqueRepository scenarioStrategiqueRepository,
        IEvenementRedouteRepository evenementRedouteRepository,
        IScenarioOperationnelRepository scenarioOperationnelRepository,
        ICoupleSourceRisqueObjectifViseRepository coupleRepository)
    {
        _scenarioDeRisqueRepository = scenarioDeRisqueRepository;
        _cheminAttaqueRepository = cheminAttaqueRepository;
        _scenarioStrategiqueRepository = scenarioStrategiqueRepository;
        _evenementRedouteRepository = evenementRedouteRepository;
        _scenarioOperationnelRepository = scenarioOperationnelRepository;
        _coupleRepository = coupleRepository;
    }

    public async Task<List<ScenarioDeRisqueVue>> ListerAsync(Guid etudeId, CancellationToken cancellationToken)
    {
        var scenariosDeRisque = await _scenarioDeRisqueRepository.ListerParEtudeAsync(etudeId, cancellationToken);
        if (scenariosDeRisque.Count == 0)
            return new List<ScenarioDeRisqueVue>();

        var chemins = (await _cheminAttaqueRepository.ListerParEtudeAsync(etudeId, cancellationToken)).ToDictionary(c => c.Id);
        var scenariosStrategiques = (await _scenarioStrategiqueRepository.ListerParEtudeAsync(etudeId, cancellationToken)).ToDictionary(s => s.Id);
        var evenementsRedoutes = (await _evenementRedouteRepository.ListerParEtudeAsync(etudeId, cancellationToken)).ToDictionary(e => e.Id);
        var couples = (await _coupleRepository.ListerParEtudeAsync(etudeId, cancellationToken)).ToDictionary(c => c.Id);

        var vues = new List<ScenarioDeRisqueVue>();
        foreach (var sdr in scenariosDeRisque)
        {
            chemins.TryGetValue(sdr.CheminAttaqueId, out var chemin);
            var libelleChemin = chemin?.Description ?? "?";

            var libelleCouple = "?";
            var gravite = 0;
            if (chemin is not null && scenariosStrategiques.TryGetValue(chemin.ScenarioStrategiqueId, out var scenarioStrat))
            {
                if (evenementsRedoutes.TryGetValue(scenarioStrat.EvenementRedouteId, out var er))
                    gravite = er.Gravite;
                if (couples.TryGetValue(scenarioStrat.CoupleSourceRisqueObjectifViseId, out var couple))
                {
                    var sr = couple.SourceRisque == CategorieSourceRisque.Autre ? couple.DescriptionSourceRisque : couple.SourceRisque.ToString();
                    var ov = couple.ObjectifVise == CategorieObjectifVise.Autre ? couple.DescriptionObjectifVise : couple.ObjectifVise.ToString();
                    libelleCouple = sr + " -- " + ov;
                }
            }

            var scenarioOp = await _scenarioOperationnelRepository.ObtenirParCheminIdAsync(sdr.CheminAttaqueId, cancellationToken);
            var vraisemblanceInitiale = scenarioOp?.VraisemblanceGlobale;

            NiveauRisque? niveauInitialCalcule = (gravite > 0 && vraisemblanceInitiale.HasValue)
                ? ServiceCalculNiveauRisque.Calculer(gravite, vraisemblanceInitiale.Value)
                : null;
            var niveauInitial = sdr.NiveauRisqueInitialRetenu ?? niveauInitialCalcule;

            vues.Add(new ScenarioDeRisqueVue(
                sdr.Id, sdr.CheminAttaqueId, libelleChemin, libelleCouple,
                gravite, vraisemblanceInitiale,
                niveauInitialCalcule, sdr.NiveauRisqueInitialRetenu, sdr.JustificationNiveauRisqueInitial, niveauInitial,
                niveauInitial.HasValue ? ServiceCalculNiveauRisque.DeterminerClasseAcceptation(niveauInitial.Value) : null,
                sdr.GraviteResiduelle, sdr.VraisemblanceResiduelle,
                sdr.NiveauRisqueResiduelCalcule, sdr.NiveauRisqueResiduelRetenu, sdr.JustificationNiveauRisqueResiduel, sdr.NiveauRisqueResiduel,
                sdr.ClasseAcceptationResiduelle,
                sdr.AccepteParDirection, sdr.NomProprietaireRisque, sdr.NomValidateurSecurite, sdr.NomSponsorExecutif,
                sdr.JustificationAcceptation, sdr.DateAcceptationUtc));
        }

        return vues;
    }
}
