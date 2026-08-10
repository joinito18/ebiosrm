using EbiosRM.Api.Infrastructure.Persistence;
using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;
using Microsoft.EntityFrameworkCore;

namespace EbiosRM.Api.Modules.CoreEngine.Infrastructure;

public class SnapshotAtelier1Repository : ISnapshotAtelier1Repository
{
    private readonly EbiosDbContext _context;

    public SnapshotAtelier1Repository(EbiosDbContext context)
    {
        _context = context;
    }

    public async Task AjouterAsync(SnapshotAtelier1 snapshot, CancellationToken cancellationToken)
    {
        _context.SnapshotsAtelier1.Add(snapshot);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<SnapshotAtelier1?> ObtenirDernierParEtudeIdAsync(Guid etudeId, CancellationToken cancellationToken)
    {
        return await _context.SnapshotsAtelier1
            .Where(s => s.EtudeId == etudeId)
            .OrderByDescending(s => s.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CompterParEtudeIdAsync(Guid etudeId, CancellationToken cancellationToken)
    {
        return await _context.SnapshotsAtelier1
            .Where(s => s.EtudeId == etudeId)
            .CountAsync(cancellationToken);
    }
}
