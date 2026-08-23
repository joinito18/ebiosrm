using EbiosRM.Api.Infrastructure.Persistence;
using EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;
using Microsoft.EntityFrameworkCore;

namespace EbiosRM.Api.Modules.CoreEngine.Infrastructure;

public sealed class PartiePrenanteRepository : IPartiePrenanteRepository
{
    private readonly EbiosDbContext _db;

    public PartiePrenanteRepository(EbiosDbContext db)
    {
        _db = db;
    }

    public async Task AjouterAsync(PartiePrenante partiePrenante, CancellationToken cancellationToken)
    {
        _db.PartiesPrenantes.Add(partiePrenante);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PartiePrenante?> ObtenirParIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.PartiesPrenantes.Include(p => p.Mesures).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<List<PartiePrenante>> ListerParEtudeAsync(Guid etudeId, CancellationToken cancellationToken)
    {
        return await _db.PartiesPrenantes.Include(p => p.Mesures)
            .Where(p => p.EtudeId == etudeId)
            .ToListAsync(cancellationToken);
    }

    public async Task MettreAJourAsync(PartiePrenante partiePrenante, CancellationToken cancellationToken)
    {
        // Ne PAS appeler Update() ici : l'entité est déjà suivie par ce
        // DbContext (chargée plus tôt via ObtenirParIdAsync/ListerParEtudeAsync
        // dans la même requête). Update() casserait le suivi de la collection
        // owned Mesures (ajout traité comme UPDATE au lieu d'INSERT ->
        // DbUpdateConcurrencyException). Même bug/correctif que
        // SocleSecuriteRepository et CheminAttaqueRepository.
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SupprimerAsync(PartiePrenante partiePrenante, CancellationToken cancellationToken)
    {
        _db.PartiesPrenantes.Remove(partiePrenante);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
