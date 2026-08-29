using EbiosRM.Api.Infrastructure.Persistence;
using EbiosRM.Api.Modules.Suivi.Domain;
using Microsoft.EntityFrameworkCore;

namespace EbiosRM.Api.Modules.Suivi.Infrastructure;

public sealed class IndicateurSuiviRepository : IIndicateurSuiviRepository
{
    private readonly EbiosDbContext _db;

    public IndicateurSuiviRepository(EbiosDbContext db)
    {
        _db = db;
    }

    public async Task<List<IndicateurSuivi>> ListerParEtudeAsync(Guid etudeId, CancellationToken cancellationToken) =>
        await _db.IndicateursSuivi.Where(i => i.EtudeId == etudeId).OrderBy(i => i.CreeLeUtc).ToListAsync(cancellationToken);

    public Task<IndicateurSuivi?> ObtenirAsync(Guid id, CancellationToken cancellationToken) =>
        _db.IndicateursSuivi.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task AjouterAsync(IndicateurSuivi indicateur, CancellationToken cancellationToken)
    {
        _db.IndicateursSuivi.Add(indicateur);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task MettreAJourAsync(IndicateurSuivi indicateur, CancellationToken cancellationToken)
    {
        _db.IndicateursSuivi.Update(indicateur);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SupprimerAsync(IndicateurSuivi indicateur, CancellationToken cancellationToken)
    {
        _db.IndicateursSuivi.Remove(indicateur);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
