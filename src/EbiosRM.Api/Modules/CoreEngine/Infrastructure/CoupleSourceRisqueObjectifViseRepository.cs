using EbiosRM.Api.Infrastructure.Persistence;
using EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;
using Microsoft.EntityFrameworkCore;

namespace EbiosRM.Api.Modules.CoreEngine.Infrastructure;

public sealed class CoupleSourceRisqueObjectifViseRepository : ICoupleSourceRisqueObjectifViseRepository
{
    private readonly EbiosDbContext _db;

    public CoupleSourceRisqueObjectifViseRepository(EbiosDbContext db)
    {
        _db = db;
    }

    public async Task AjouterAsync(CoupleSourceRisqueObjectifVise couple, CancellationToken cancellationToken)
    {
        _db.CouplesSrOv.Add(couple);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CoupleSourceRisqueObjectifVise?> ObtenirParIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.CouplesSrOv.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<List<CoupleSourceRisqueObjectifVise>> ListerParEtudeAsync(Guid etudeId, CancellationToken cancellationToken)
    {
        return await _db.CouplesSrOv
            .Where(c => c.EtudeId == etudeId)
            .ToListAsync(cancellationToken);
    }

    public async Task MettreAJourAsync(CoupleSourceRisqueObjectifVise couple, CancellationToken cancellationToken)
    {
        _db.CouplesSrOv.Update(couple);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SupprimerAsync(CoupleSourceRisqueObjectifVise couple, CancellationToken cancellationToken)
    {
        _db.CouplesSrOv.Remove(couple);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
