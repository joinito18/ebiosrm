using EbiosRM.Api.Infrastructure.Persistence;
using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;
using Microsoft.EntityFrameworkCore;

namespace EbiosRM.Api.Modules.CoreEngine.Infrastructure;

public sealed class SocleSecuriteRepository : ISocleSecuriteRepository
{
    private readonly EbiosDbContext _db;

    public SocleSecuriteRepository(EbiosDbContext db)
    {
        _db = db;
    }

    public async Task AjouterAsync(SocleSecurite socleSecurite, CancellationToken cancellationToken)
    {
        _db.SoclesSecurite.Add(socleSecurite);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<SocleSecurite?> ObtenirParEtudeAsync(Guid etudeId, CancellationToken cancellationToken)
    {
        return await _db.SoclesSecurite
            .Include(s => s.Referentiels)
            .FirstOrDefaultAsync(s => s.EtudeId == etudeId, cancellationToken);
    }

    public async Task MettreAJourAsync(SocleSecurite socleSecurite, CancellationToken cancellationToken)
    {
        // L'entité est déjà suivie par ce DbContext (chargée plus tôt dans la même
        // requête via ObtenirParEtudeAsync) : appeler Update() ici casserait le
        // suivi déjà en place (cause du DbUpdateConcurrencyException observé).
        // SaveChangesAsync seul suffit, EF Core détecte le changement automatiquement.
        await _db.SaveChangesAsync(cancellationToken);
    }
}
