using EbiosRM.Api.Infrastructure.Persistence;
using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;
using Microsoft.EntityFrameworkCore;

namespace EbiosRM.Api.Modules.CoreEngine.Infrastructure;

public class SnapshotAtelierRepository : ISnapshotAtelierRepository
{
    private readonly EbiosDbContext _context;

    public SnapshotAtelierRepository(EbiosDbContext context)
    {
        _context = context;
    }

    public async Task AjouterAsync(SnapshotAtelier snapshot, CancellationToken cancellationToken)
    {
        _context.SnapshotsAtelier.Add(snapshot);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<SnapshotAtelier?> ObtenirDernierParEtudeIdAsync(Guid etudeId, int numeroAtelier, CancellationToken cancellationToken)
    {
        return await _context.SnapshotsAtelier
            .Where(s => s.EtudeId == etudeId && s.NumeroAtelier == numeroAtelier)
            .OrderByDescending(s => s.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CompterParEtudeIdAsync(Guid etudeId, int numeroAtelier, CancellationToken cancellationToken)
    {
        return await _context.SnapshotsAtelier
            .Where(s => s.EtudeId == etudeId && s.NumeroAtelier == numeroAtelier)
            .CountAsync(cancellationToken);
    }
}
