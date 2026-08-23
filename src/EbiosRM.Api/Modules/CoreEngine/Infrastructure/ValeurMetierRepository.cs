using EbiosRM.Api.Infrastructure.Persistence;
using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;
using Microsoft.EntityFrameworkCore;

namespace EbiosRM.Api.Modules.CoreEngine.Infrastructure;

public sealed class ValeurMetierRepository : IValeurMetierRepository
{
    private readonly EbiosDbContext _db;

    public ValeurMetierRepository(EbiosDbContext db)
    {
        _db = db;
    }

    public async Task AjouterAsync(ValeurMetier valeurMetier, CancellationToken cancellationToken)
    {
        _db.ValeursMetier.Add(valeurMetier);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ValeurMetier?> ObtenirParIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.ValeursMetier.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<List<ValeurMetier>> ListerParEtudeAsync(Guid etudeId, CancellationToken cancellationToken)
    {
        return await _db.ValeursMetier
            .Where(v => v.EtudeId == etudeId)
            .ToListAsync(cancellationToken);
    }

    public async Task MettreAJourAsync(ValeurMetier valeurMetier, CancellationToken cancellationToken)
    {
        _db.ValeursMetier.Update(valeurMetier);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SupprimerAsync(ValeurMetier valeurMetier, CancellationToken cancellationToken)
    {
        _db.ValeursMetier.Remove(valeurMetier);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
