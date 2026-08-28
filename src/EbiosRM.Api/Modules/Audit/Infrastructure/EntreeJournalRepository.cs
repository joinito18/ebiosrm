using EbiosRM.Api.Infrastructure.Persistence;
using EbiosRM.Api.Modules.Audit.Domain;
using Microsoft.EntityFrameworkCore;

namespace EbiosRM.Api.Modules.Audit.Infrastructure;

public sealed class EntreeJournalRepository : IEntreeJournalRepository
{
    private readonly EbiosDbContext _db;

    public EntreeJournalRepository(EbiosDbContext db)
    {
        _db = db;
    }

    public async Task AjouterAsync(EntreeJournal entree, CancellationToken cancellationToken)
    {
        _db.JournalAudit.Add(entree);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<EntreeJournal>> ListerParEtudeAsync(Guid etudeId, int limite, CancellationToken cancellationToken)
    {
        return await _db.JournalAudit
            .Where(e => e.EtudeId == etudeId)
            .OrderByDescending(e => e.DateUtc)
            .Take(limite)
            .ToListAsync(cancellationToken);
    }
}
