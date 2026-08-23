namespace EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

public interface ISnapshotAtelierRepository
{
    Task AjouterAsync(SnapshotAtelier snapshot, CancellationToken cancellationToken);
    Task<SnapshotAtelier?> ObtenirDernierParEtudeIdAsync(Guid etudeId, int numeroAtelier, CancellationToken cancellationToken);
    Task<int> CompterParEtudeIdAsync(Guid etudeId, int numeroAtelier, CancellationToken cancellationToken);
}
