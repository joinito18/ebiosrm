using System.Text.Json;
using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;
using EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;

namespace EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

/// <summary>
/// Domain Service : assemble l'état courant de l'Atelier 4 et le fige dans un
/// nouveau SnapshotAtelier (P13). Appelé juste après Etude.ValiderAtelier4()
/// et sa persistance. Même patron que ServiceCreationSnapshotAtelier1/2/3.
/// </summary>
public sealed class ServiceCreationSnapshotAtelier4
{
    private readonly IEtudeRepository _etudeRepository;
    private readonly IScenarioOperationnelRepository _scenarioOperationnelRepository;
    private readonly ICheminAttaqueRepository _cheminAttaqueRepository;
    private readonly IScenarioStrategiqueRepository _scenarioStrategiqueRepository;
    private readonly ICoupleSourceRisqueObjectifViseRepository _coupleRepository;
    private readonly IBienSupportRepository _bienSupportRepository;
    private readonly ISnapshotAtelierRepository _snapshotRepository;

    public ServiceCreationSnapshotAtelier4(
        IEtudeRepository etudeRepository,
        IScenarioOperationnelRepository scenarioOperationnelRepository,
        ICheminAttaqueRepository cheminAttaqueRepository,
        IScenarioStrategiqueRepository scenarioStrategiqueRepository,
        ICoupleSourceRisqueObjectifViseRepository coupleRepository,
        IBienSupportRepository bienSupportRepository,
        ISnapshotAtelierRepository snapshotRepository)
    {
        _etudeRepository = etudeRepository;
        _scenarioOperationnelRepository = scenarioOperationnelRepository;
        _cheminAttaqueRepository = cheminAttaqueRepository;
        _scenarioStrategiqueRepository = scenarioStrategiqueRepository;
        _coupleRepository = coupleRepository;
        _bienSupportRepository = bienSupportRepository;
        _snapshotRepository = snapshotRepository;
    }

    public async Task<SnapshotAtelier> CreerAsync(Guid etudeId, CancellationToken cancellationToken)
    {
        var etude = await _etudeRepository.ObtenirParIdAsync(etudeId, cancellationToken);
        if (etude is null)
            throw new InvalidOperationException($"Étude introuvable : {etudeId}.");

        if (etude.StatutAtelier4 != StatutEtude.Validee)
            throw new InvalidOperationException(
                $"Impossible de créer un snapshot : l'atelier 4 doit être 'Validee' (statut actuel : '{etude.StatutAtelier4}').");

        var scenariosOp = await _scenarioOperationnelRepository.ListerParEtudeAsync(etudeId, cancellationToken);
        var chemins = await _cheminAttaqueRepository.ListerParEtudeAsync(etudeId, cancellationToken);
        var scenariosStrategiques = await _scenarioStrategiqueRepository.ListerParEtudeAsync(etudeId, cancellationToken);
        var couples = await _coupleRepository.ListerParEtudeAsync(etudeId, cancellationToken);
        var biensSupport = await _bienSupportRepository.ListerParEtudeAsync(etudeId, cancellationToken);

        var scenariosOpSnapshot = scenariosOp.Select(s => new ScenarioOperationnelSnapshot(
            s.CheminAttaqueId,
            s.VraisemblanceGlobale,
            s.ModesOperatoires.Select(m => new ModeOperatoireSnapshot(
                m.Description,
                m.ActionsElementaires.Select(a => new ActionElementaireSnapshotContenu(a.Description, a.Phase, a.BienSupportId, a.TechniqueMitre)).ToList(),
                m.ProbabiliteSucces,
                m.DifficulteTechnique,
                m.Vraisemblance,
                m.VraisemblanceRetenue is not null,
                m.JustificationVraisemblance)).ToList()
        )).ToList();

        var cheminsSnapshot = chemins
            .Select(c => new CheminAttaqueResumeSnapshot(c.Id, c.Description, c.ScenarioStrategiqueId))
            .ToList();

        var scenariosStrategiquesSnapshot = scenariosStrategiques
            .Select(s => new ScenarioStrategiqueResumeSnapshot(s.Id, s.CoupleSourceRisqueObjectifViseId))
            .ToList();

        var couplesSnapshot = couples
            .Select(c => new CoupleSrOvResumeSnapshot(
                c.Id, c.SourceRisque.ToString(), c.DescriptionSourceRisque,
                c.ObjectifVise.ToString(), c.DescriptionObjectifVise, c.Pertinence.ToString()))
            .ToList();

        var biensSupportSnapshot = biensSupport
            .Select(b => new BienSupportResumeSnapshot(b.Id, b.Description))
            .ToList();

        var versionPrecedente = await _snapshotRepository.CompterParEtudeIdAsync(etudeId, numeroAtelier: 4, cancellationToken);
        var nouvelleVersion = versionPrecedente + 1;

        var contenu = new SnapshotAtelier4Content(
            etude.Id,
            nouvelleVersion,
            etude.Nom,
            DateTime.UtcNow,
            scenariosOpSnapshot,
            cheminsSnapshot,
            scenariosStrategiquesSnapshot,
            couplesSnapshot,
            biensSupportSnapshot);

        var contenuJson = JsonSerializer.Serialize(contenu);

        var snapshot = SnapshotAtelier.Creer(etudeId, numeroAtelier: 4, nouvelleVersion, contenuJson);
        await _snapshotRepository.AjouterAsync(snapshot, cancellationToken);

        return snapshot;
    }
}
