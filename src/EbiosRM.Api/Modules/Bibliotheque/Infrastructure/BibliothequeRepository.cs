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

    public async Task<List<MesureBibliotheque>> ListerMesuresAsync(Guid proprietaireId, CancellationToken cancellationToken) =>
        await _db.MesuresBibliotheque
            .Where(m => m.ProprietaireId == proprietaireId)
            .OrderByDescending(m => m.CreeLeUtc)
            .ToListAsync(cancellationToken);

    public Task<MesureBibliotheque?> ObtenirMesureAsync(Guid id, CancellationToken cancellationToken) =>
        _db.MesuresBibliotheque.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task AjouterMesureAsync(MesureBibliotheque mesure, CancellationToken cancellationToken)
    {
        _db.MesuresBibliotheque.Add(mesure);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SupprimerMesureAsync(MesureBibliotheque mesure, CancellationToken cancellationToken)
    {
        _db.MesuresBibliotheque.Remove(mesure);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<SourceRisqueBibliotheque>> ListerSourcesRisqueAsync(Guid proprietaireId, CancellationToken cancellationToken) =>
        await _db.SourcesRisqueBibliotheque
            .Where(s => s.ProprietaireId == proprietaireId)
            .OrderByDescending(s => s.CreeLeUtc)
            .ToListAsync(cancellationToken);

    public Task<SourceRisqueBibliotheque?> ObtenirSourceRisqueAsync(Guid id, CancellationToken cancellationToken) =>
        _db.SourcesRisqueBibliotheque.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task AjouterSourceRisqueAsync(SourceRisqueBibliotheque source, CancellationToken cancellationToken)
    {
        _db.SourcesRisqueBibliotheque.Add(source);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SupprimerSourceRisqueAsync(SourceRisqueBibliotheque source, CancellationToken cancellationToken)
    {
        _db.SourcesRisqueBibliotheque.Remove(source);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
