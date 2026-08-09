using EbiosRM.Api.Infrastructure.Persistence;
using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;
using Microsoft.EntityFrameworkCore;

namespace EbiosRM.Api.Modules.CoreEngine.Infrastructure;

public sealed class BienSupportRepository : IBienSupportRepository
{
    private readonly EbiosDbContext _db;

    public BienSupportRepository(EbiosDbContext db)
    {
        _db = db;
    }

    public async Task AjouterAsync(BienSupport bienSupport, CancellationToken cancellationToken)
    {
        _db.BiensSupport.Add(bienSupport);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<BienSupport>> ListerParEtudeAsync(Guid etudeId, CancellationToken cancellationToken)
    {
        return await _db.BiensSupport
            .Where(b => b.EtudeId == etudeId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<BienSupport>> ListerParValeurMetierAsync(Guid valeurMetierId, CancellationToken cancellationToken)
    {
        return await _db.BiensSupport
            .Where(b => b.ValeurMetierId == valeurMetierId)
            .ToListAsync(cancellationToken);
    }
}
