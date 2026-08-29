using EbiosRM.Api.Infrastructure.Persistence;
using EbiosRM.Api.Modules.Bibliotheque.Domain;
using Microsoft.EntityFrameworkCore;

namespace EbiosRM.Api.Modules.Bibliotheque.Infrastructure;

public sealed class BibliothequeRepository : IBibliothequeRepository
{
    private readonly EbiosDbContext _db;

    public BibliothequeRepository(EbiosDbContext db)
    {
        _db = db;
    }

    public async Task<List<T>> ListerAsync<T>(Guid proprietaireId, CancellationToken cancellationToken)
        where T : class, IEntreeBibliotheque =>
        await _db.Set<T>()
            .Where(e => EF.Property<Guid?>(e, "ProprietaireId") == proprietaireId)
            .OrderByDescending(e => EF.Property<DateTime>(e, "CreeLeUtc"))
            .ToListAsync(cancellationToken);

    public Task<T?> ObtenirAsync<T>(Guid id, CancellationToken cancellationToken)
        where T : class, IEntreeBibliotheque =>
        _db.Set<T>().FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);

    public async Task AjouterAsync<T>(T entree, CancellationToken cancellationToken)
        where T : class, IEntreeBibliotheque
    {
        _db.Set<T>().Add(entree);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SupprimerAsync<T>(T entree, CancellationToken cancellationToken)
        where T : class, IEntreeBibliotheque
    {
        _db.Set<T>().Remove(entree);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
