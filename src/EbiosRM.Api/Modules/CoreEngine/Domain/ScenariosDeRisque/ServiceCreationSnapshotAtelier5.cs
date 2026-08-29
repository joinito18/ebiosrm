using System.Text.Json;
using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

namespace EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

/// <summary>
/// Domain Service : assemble l'état courant de l'Atelier 5 et le fige dans un
/// nouveau SnapshotAtelier (P13). Appelé juste après Etude.ValiderAtelier5()
/// et sa persistance. Même patron que ServiceCreationSnapshotAtelier1/2/3/4.
/// </summary>
public sealed class ServiceCreationSnapshotAtelier5
{
    private readonly IEtudeRepository _etudeRepository;
    private readonly ServiceAssemblageScenariosDeRisque _serviceAssemblage;
    private readonly IPlanTraitementRisqueRepository _planRepository;
    private readonly ISnapshotAtelierRepository _snapshotRepository;

    public ServiceCreationSnapshotAtelier5(
        IEtudeRepository etudeRepository,
        ServiceAssemblageScenariosDeRisque serviceAssemblage,
        IPlanTraitementRisqueRepository planRepository,
        ISnapshotAtelierRepository snapshotRepository)
    {
        _etudeRepository = etudeRepository;
        _serviceAssemblage = serviceAssemblage;
        _planRepository = planRepository;
        _snapshotRepository = snapshotRepository;
    }

    public async Task<SnapshotAtelier> CreerAsync(Guid etudeId, CancellationToken cancellationToken)
    {
        var etude = await _etudeRepository.ObtenirParIdAsync(etudeId, cancellationToken);
        if (etude is null)
            throw new InvalidOperationException($"Étude introuvable : {etudeId}.");

        if (etude.StatutAtelier5 != StatutEtude.Validee)
            throw new InvalidOperationException(
                $"Impossible de créer un snapshot : l'atelier 5 doit être 'Validee' (statut actuel : '{etude.StatutAtelier5}').");

        var vues = await _serviceAssemblage.ListerAsync(etudeId, cancellationToken);
        var plan = await _planRepository.ObtenirParEtudeAsync(etudeId, cancellationToken);

        var scenariosSnapshot = vues.Select(v => new ScenarioDeRisqueSnapshot(
            v.Id, v.LibelleChemin, v.LibelleCouple, v.Gravite, v.VraisemblanceInitiale,
            v.NiveauRisqueInitial, v.NiveauRisqueInitialRetenu is not null, v.JustificationNiveauRisqueInitial, v.ClasseAcceptationInitiale,
            v.GraviteResiduelle, v.VraisemblanceResiduelle,
            v.NiveauRisqueResiduel, v.NiveauRisqueResiduelRetenu is not null, v.JustificationNiveauRisqueResiduel, v.ClasseAcceptationResiduelle,
            v.AccepteParDirection, v.NomProprietaireRisque, v.NomValidateurSecurite, v.NomSponsorExecutif,
            v.JustificationAcceptation, v.DateAcceptationUtc)).ToList();

        var mesuresSnapshot = (plan?.Mesures ?? new List<MesureTraitementRisque>()).Select(m => new MesureTraitementRisqueSnapshot(
            m.Id, m.Description, m.Axe, m.ScenariosDeRisqueIds.ToList(), m.Responsable,
            m.FreinsEtDifficultes, m.CoutComplexite, m.Echeance, m.Statut, m.CodesConformite.ToList())).ToList();

        var versionPrecedente = await _snapshotRepository.CompterParEtudeIdAsync(etudeId, numeroAtelier: 5, cancellationToken);
        var nouvelleVersion = versionPrecedente + 1;

        var contenu = new SnapshotAtelier5Content(etude.Id, nouvelleVersion, etude.Nom, DateTime.UtcNow, scenariosSnapshot, mesuresSnapshot);
        var contenuJson = JsonSerializer.Serialize(contenu);

        var snapshot = SnapshotAtelier.Creer(etudeId, numeroAtelier: 5, nouvelleVersion, contenuJson);
        await _snapshotRepository.AjouterAsync(snapshot, cancellationToken);

        return snapshot;
    }
}
