using System.Text.Json;
using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;
using EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;

namespace EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

/// <summary>
/// Domain Service : assemble l'état courant de l'Atelier 3 et le fige dans un
/// nouveau SnapshotAtelier (P13). Appelé juste après Etude.ValiderAtelier3()
/// et sa persistance. Même patron que ServiceCreationSnapshotAtelier1/2.
/// </summary>
public sealed class ServiceCreationSnapshotAtelier3
{
    private readonly IEtudeRepository _etudeRepository;
    private readonly IPartiePrenanteRepository _partiePrenanteRepository;
    private readonly IScenarioStrategiqueRepository _scenarioRepository;
    private readonly ICoupleSourceRisqueObjectifViseRepository _coupleRepository;
    private readonly IEvenementRedouteRepository _evenementRedouteRepository;
    private readonly IValeurMetierRepository _valeurMetierRepository;
    private readonly ICheminAttaqueRepository _cheminAttaqueRepository;
    private readonly ISnapshotAtelierRepository _snapshotRepository;

    public ServiceCreationSnapshotAtelier3(
        IEtudeRepository etudeRepository,
        IPartiePrenanteRepository partiePrenanteRepository,
        IScenarioStrategiqueRepository scenarioRepository,
        ICoupleSourceRisqueObjectifViseRepository coupleRepository,
        IEvenementRedouteRepository evenementRedouteRepository,
        IValeurMetierRepository valeurMetierRepository,
        ICheminAttaqueRepository cheminAttaqueRepository,
        ISnapshotAtelierRepository snapshotRepository)
    {
        _etudeRepository = etudeRepository;
        _partiePrenanteRepository = partiePrenanteRepository;
        _scenarioRepository = scenarioRepository;
        _coupleRepository = coupleRepository;
        _evenementRedouteRepository = evenementRedouteRepository;
        _valeurMetierRepository = valeurMetierRepository;
        _cheminAttaqueRepository = cheminAttaqueRepository;
        _snapshotRepository = snapshotRepository;
    }

    public async Task<SnapshotAtelier> CreerAsync(Guid etudeId, CancellationToken cancellationToken)
    {
        var etude = await _etudeRepository.ObtenirParIdAsync(etudeId, cancellationToken);
        if (etude is null)
            throw new InvalidOperationException($"Étude introuvable : {etudeId}.");

        if (etude.StatutAtelier3 != StatutEtude.Validee)
            throw new InvalidOperationException(
                $"Impossible de créer un snapshot : l'atelier 3 doit être 'Validee' (statut actuel : '{etude.StatutAtelier3}').");

        var parties = await _partiePrenanteRepository.ListerParEtudeAsync(etudeId, cancellationToken);
        var scenarios = await _scenarioRepository.ListerParEtudeAsync(etudeId, cancellationToken);
        var couples = await _coupleRepository.ListerParEtudeAsync(etudeId, cancellationToken);
        var evenements = await _evenementRedouteRepository.ListerParEtudeAsync(etudeId, cancellationToken);
        var valeurs = await _valeurMetierRepository.ListerParEtudeAsync(etudeId, cancellationToken);

        var partiesSnapshot = parties
            .Select(p => new PartiePrenanteDangerositeSnapshot(
                p.Id, p.Nom, p.RolesEtAttentes, p.Representant,
                p.Categorie == CategoriePartiePrenante.Autre ? p.DescriptionCategorie ?? "Autre" : p.Categorie.ToString(),
                p.Dependance, p.Penetration, p.MaturiteCyber, p.Confiance, p.NiveauDangerosite,
                p.Zone?.ToString(),
                p.NiveauDangerositeRetenu is not null, p.JustificationDangerosite,
                p.Mesures.Select(m => m.Description).ToList(),
                p.NiveauDangerositeResiduel, p.ZoneResiduelle?.ToString(),
                p.NiveauDangerositeResiduelRetenu is not null, p.JustificationDangerositeResiduelle))
            .ToList();

        var scenariosSnapshot = new List<ScenarioStrategiqueSnapshot>();
        foreach (var s in scenarios)
        {
            var chemins = await _cheminAttaqueRepository.ListerParScenarioAsync(s.Id, cancellationToken);
            var cheminsSnapshot = chemins.Select(c => new CheminAttaqueSnapshot(
                c.Description,
                c.EvenementsIntermediaires.Select(ei => new EvenementIntermediaireSnapshot(ei.PartiePrenanteId, ei.Description)).ToList()
            )).ToList();

            scenariosSnapshot.Add(new ScenarioStrategiqueSnapshot(
                s.Id, s.CoupleSourceRisqueObjectifViseId, s.EvenementRedouteId, s.Description, cheminsSnapshot));
        }

        var couplesSnapshot = couples
            .Select(c => new CoupleSrOvResumeSnapshot(
                c.Id, c.SourceRisque.ToString(), c.DescriptionSourceRisque,
                c.ObjectifVise.ToString(), c.DescriptionObjectifVise, c.Pertinence.ToString()))
            .ToList();

        var evenementsSnapshot = evenements
            .Select(e => new EvenementRedouteResumeSnapshot(e.Id, e.ValeurMetierId, e.Description, e.Gravite))
            .ToList();

        var valeursSnapshot = valeurs
            .Select(v => new ValeurMetierResumeSnapshot(v.Id, v.Description))
            .ToList();

        var versionPrecedente = await _snapshotRepository.CompterParEtudeIdAsync(etudeId, numeroAtelier: 3, cancellationToken);
        var nouvelleVersion = versionPrecedente + 1;

        var contenu = new SnapshotAtelier3Content(
            etude.Id,
            nouvelleVersion,
            etude.Nom,
            DateTime.UtcNow,
            partiesSnapshot,
            scenariosSnapshot,
            couplesSnapshot,
            evenementsSnapshot,
            valeursSnapshot);

        var contenuJson = JsonSerializer.Serialize(contenu);

        var snapshot = SnapshotAtelier.Creer(etudeId, numeroAtelier: 3, nouvelleVersion, contenuJson);
        await _snapshotRepository.AjouterAsync(snapshot, cancellationToken);

        return snapshot;
    }
}
