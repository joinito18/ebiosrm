namespace EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

public interface ISnapshotAtelier1Repository
{
    Task AjouterAsync(SnapshotAtelier1 snapshot, CancellationToken cancellationToken);
    Task<SnapshotAtelier1?> ObtenirDernierParEtudeIdAsync(Guid etudeId, CancellationToken cancellationToken);
    Task<int> CompterParEtudeIdAsync(Guid etudeId, CancellationToken cancellationToken);
}
