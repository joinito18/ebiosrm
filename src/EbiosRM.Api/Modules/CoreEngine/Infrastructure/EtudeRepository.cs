using EbiosRM.Api.Infrastructure.Persistence;
using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;
using Microsoft.EntityFrameworkCore;

namespace EbiosRM.Api.Modules.CoreEngine.Infrastructure;

public sealed class EtudeRepository : IEtudeRepository
{
    private readonly EbiosDbContext _db;

    public EtudeRepository(EbiosDbContext db)
    {
        _db = db;
    }

    public async Task AjouterAsync(Etude etude, CancellationToken cancellationToken)
    {
        _db.Etudes.Add(etude);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Etude?> ObtenirParIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.Etudes.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<List<Etude>> ListerAsync(CancellationToken cancellationToken)
    {
        return await _db.Etudes.ToListAsync(cancellationToken);
    }

    public async Task<List<Etude>> ListerVisiblesAsync(Guid utilisateurId, CancellationToken cancellationToken)
    {
        return await _db.Etudes
            .Where(e => e.ProprietaireId == null || e.ProprietaireId == utilisateurId)
            .ToListAsync(cancellationToken);
    }

    public async Task MettreAJourAsync(Etude etude, CancellationToken cancellationToken)
    {
        await _db.SaveChangesAsync(cancellationToken);
    }
}
