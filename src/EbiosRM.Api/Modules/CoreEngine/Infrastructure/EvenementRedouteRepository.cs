using EbiosRM.Api.Infrastructure.Persistence;
using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;
using Microsoft.EntityFrameworkCore;

namespace EbiosRM.Api.Modules.CoreEngine.Infrastructure;

public sealed class EvenementRedouteRepository : IEvenementRedouteRepository
{
    private readonly EbiosDbContext _db;

    public EvenementRedouteRepository(EbiosDbContext db)
    {
        _db = db;
    }

    public async Task AjouterAsync(EvenementRedoute evenementRedoute, CancellationToken cancellationToken)
    {
        _db.EvenementsRedoutes.Add(evenementRedoute);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<EvenementRedoute?> ObtenirParIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.EvenementsRedoutes.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<List<EvenementRedoute>> ListerParEtudeAsync(Guid etudeId, CancellationToken cancellationToken)
    {
        return await _db.EvenementsRedoutes
            .Where(e => e.EtudeId == etudeId)
            .ToListAsync(cancellationToken);
    }

    public async Task MettreAJourAsync(EvenementRedoute evenementRedoute, CancellationToken cancellationToken)
    {
        // Entité déjà trackée dans la même requête, pas d'Update() explicite nécessaire.
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SupprimerAsync(EvenementRedoute evenementRedoute, CancellationToken cancellationToken)
    {
        _db.EvenementsRedoutes.Remove(evenementRedoute);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
