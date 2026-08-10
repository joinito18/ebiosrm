using System.Text.Json;

namespace EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

/// <summary>
/// Domain Service : assemble l'état courant de l'Atelier 1 et le fige dans un
/// nouveau SnapshotAtelier1 (P13). Appelé juste après Etude.ValiderAtelier1()
/// et sa persistance. Chaque appel crée une NOUVELLE version -- y compris en
/// cas de re-validation après correction (une modification crée une nouvelle
/// version, l'historique des versions précédentes est conservé, jamais écrasé).
/// Lecture seule sur les autres agrégats du Core Engine.
/// </summary>
public sealed class ServiceCreationSnapshotAtelier1
{
    private readonly IEtudeRepository _etudeRepository;
    private readonly IValeurMetierRepository _valeurMetierRepository;
    private readonly IBienSupportRepository _bienSupportRepository;
    private readonly IEvenementRedouteRepository _evenementRedouteRepository;
    private readonly ISocleSecuriteRepository _socleSecuriteRepository;
    private readonly ISnapshotAtelier1Repository _snapshotRepository;

    public ServiceCreationSnapshotAtelier1(
        IEtudeRepository etudeRepository,
        IValeurMetierRepository valeurMetierRepository,
        IBienSupportRepository bienSupportRepository,
        IEvenementRedouteRepository evenementRedouteRepository,
        ISocleSecuriteRepository socleSecuriteRepository,
        ISnapshotAtelier1Repository snapshotRepository)
    {
        _etudeRepository = etudeRepository;
        _valeurMetierRepository = valeurMetierRepository;
        _bienSupportRepository = bienSupportRepository;
        _evenementRedouteRepository = evenementRedouteRepository;
        _socleSecuriteRepository = socleSecuriteRepository;
        _snapshotRepository = snapshotRepository;
    }

    public async Task<SnapshotAtelier1> CreerAsync(Guid etudeId, CancellationToken cancellationToken)
    {
        var etude = await _etudeRepository.ObtenirParIdAsync(etudeId, cancellationToken);
        if (etude is null)
            throw new InvalidOperationException($"Étude introuvable : {etudeId}.");

        if (etude.Statut != StatutEtude.Validee)
            throw new InvalidOperationException(
                $"Impossible de créer un snapshot : l'étude doit être 'Validee' (statut actuel : '{etude.Statut}').");

        var valeursMetier = await _valeurMetierRepository.ListerParEtudeAsync(etudeId, cancellationToken);
        var biensSupport = await _bienSupportRepository.ListerParEtudeAsync(etudeId, cancellationToken);
        var evenementsRedoutes = await _evenementRedouteRepository.ListerParEtudeAsync(etudeId, cancellationToken);
        var socleSecurite = await _socleSecuriteRepository.ObtenirParEtudeAsync(etudeId, cancellationToken);

        var valeursMetierSnapshot = valeursMetier
            .Select(vm => new ValeurMetierSnapshot(vm.Id, vm.Description, vm.EntiteResponsable))
            .ToList();

        var biensSupportSnapshot = biensSupport
            .Select(b => new BienSupportSnapshot(b.Id, b.ValeurMetierId, b.Description, b.Type.ToString(), b.EntiteResponsable))
            .ToList();

        var evenementsRedoutesSnapshot = evenementsRedoutes
            .Select(er => new EvenementRedouteSnapshot(er.Id, er.ValeurMetierId, er.Description, er.Gravite))
            .ToList();

        SocleSecuriteSnapshot? socleSnapshot = socleSecurite is null
            ? null
            : new SocleSecuriteSnapshot(
                socleSecurite.Id,
                socleSecurite.Referentiels
                    .Select(r => new ReferentielApplicableSnapshot(r.Id, r.Nom, r.Etat.ToString()))
                    .ToList());

        var contenu = new SnapshotAtelier1Content(
            etude.Id,
            etude.Nom,
            etude.Perimetre,
            etude.Statut.ToString(),
            DateTime.UtcNow,
            valeursMetierSnapshot,
            biensSupportSnapshot,
            evenementsRedoutesSnapshot,
            socleSnapshot);

        var contenuJson = JsonSerializer.Serialize(contenu);

        var versionPrecedente = await _snapshotRepository.CompterParEtudeIdAsync(etudeId, cancellationToken);
        var nouvelleVersion = versionPrecedente + 1;

        var snapshot = SnapshotAtelier1.Creer(etudeId, nouvelleVersion, contenuJson);
        await _snapshotRepository.AjouterAsync(snapshot, cancellationToken);

        return snapshot;
    }
}
