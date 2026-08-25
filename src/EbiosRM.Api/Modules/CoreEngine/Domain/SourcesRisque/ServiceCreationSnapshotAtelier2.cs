using System.Text.Json;
using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

namespace EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;

/// <summary>
/// Domain Service : assemble l'état courant de l'Atelier 2 et le fige dans un
/// nouveau SnapshotAtelier (P13). Appelé juste après Etude.ValiderAtelier2()
/// et sa persistance. Même patron que ServiceCreationSnapshotAtelier1.
/// Ne couvre que les couples SR/OV -- les parties prenantes appartiennent à
/// la cartographie de l'écosystème de l'Atelier 3 (cf. SnapshotAtelier3Content),
/// conformément au livrable officiel ANSSI de l'Atelier 2 qui ne mentionne
/// aucune partie prenante.
/// </summary>
public sealed class ServiceCreationSnapshotAtelier2
{
    private readonly IEtudeRepository _etudeRepository;
    private readonly ICoupleSourceRisqueObjectifViseRepository _coupleRepository;
    private readonly ISnapshotAtelierRepository _snapshotRepository;

    public ServiceCreationSnapshotAtelier2(
        IEtudeRepository etudeRepository,
        ICoupleSourceRisqueObjectifViseRepository coupleRepository,
        ISnapshotAtelierRepository snapshotRepository)
    {
        _etudeRepository = etudeRepository;
        _coupleRepository = coupleRepository;
        _snapshotRepository = snapshotRepository;
    }

    public async Task<SnapshotAtelier> CreerAsync(Guid etudeId, CancellationToken cancellationToken)
    {
        var etude = await _etudeRepository.ObtenirParIdAsync(etudeId, cancellationToken);
        if (etude is null)
            throw new InvalidOperationException($"Étude introuvable : {etudeId}.");

        if (etude.StatutAtelier2 != StatutEtude.Validee)
            throw new InvalidOperationException(
                $"Impossible de créer un snapshot : l'atelier 2 doit être 'Validee' (statut actuel : '{etude.StatutAtelier2}').");

        var couples = await _coupleRepository.ListerParEtudeAsync(etudeId, cancellationToken);

        var couplesSnapshot = couples
            .Select(c => new CoupleSrOvSnapshot(
                c.SourceRisque.ToString(),
                c.DescriptionSourceRisque,
                c.ObjectifVise.ToString(),
                c.DescriptionObjectifVise,
                c.ContexteVulnerabilite,
                c.Theme,
                c.Motivation,
                c.Ressources,
                c.Pertinence.ToString(),
                c.PertinenceRetenue is not null,
                c.JustificationPertinence))
            .ToList();

        var versionPrecedente = await _snapshotRepository.CompterParEtudeIdAsync(etudeId, numeroAtelier: 2, cancellationToken);
        var nouvelleVersion = versionPrecedente + 1;

        var contenu = new SnapshotAtelier2Content(
            etude.Id,
            nouvelleVersion,
            etude.Nom,
            DateTime.UtcNow,
            couplesSnapshot);

        var contenuJson = JsonSerializer.Serialize(contenu);

        var snapshot = SnapshotAtelier.Creer(etudeId, numeroAtelier: 2, nouvelleVersion, contenuJson);
        await _snapshotRepository.AjouterAsync(snapshot, cancellationToken);

        return snapshot;
    }
}
