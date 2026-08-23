using EbiosRM.Api.Infrastructure.Persistence;
using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;
using Microsoft.EntityFrameworkCore;

namespace EbiosRM.Api.Modules.CoreEngine.Infrastructure;

public sealed class CheminAttaqueRepository : ICheminAttaqueRepository
{
    private readonly EbiosDbContext _db;

    public CheminAttaqueRepository(EbiosDbContext db)
    {
        _db = db;
    }

    public async Task AjouterAsync(CheminAttaque chemin, CancellationToken cancellationToken)
    {
        _db.CheminsAttaque.Add(chemin);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CheminAttaque?> ObtenirParIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.CheminsAttaque.Include(c => c.EvenementsIntermediaires).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<List<CheminAttaque>> ListerParEtudeAsync(Guid etudeId, CancellationToken cancellationToken)
    {
        return await _db.CheminsAttaque.Include(c => c.EvenementsIntermediaires)
            .Where(c => c.EtudeId == etudeId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CheminAttaque>> ListerParScenarioAsync(Guid scenarioStrategiqueId, CancellationToken cancellationToken)
    {
        return await _db.CheminsAttaque.Include(c => c.EvenementsIntermediaires)
            .Where(c => c.ScenarioStrategiqueId == scenarioStrategiqueId)
            .ToListAsync(cancellationToken);
    }

    public async Task MettreAJourAsync(CheminAttaque chemin, CancellationToken cancellationToken)
    {
        // L'entité est déjà suivie par ce DbContext (chargée plus tôt dans la même
        // requête via ObtenirParIdAsync) : appeler Update() ici casserait le suivi
        // déjà en place sur la collection owned EvenementsIntermediaires (un ajout
        // en mémoire serait traité comme une UPDATE au lieu d'un INSERT ->
        // DbUpdateConcurrencyException "0 row(s) affected"). Même bug/correctif que
        // SocleSecuriteRepository. SaveChangesAsync seul suffit.
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SupprimerAsync(CheminAttaque chemin, CancellationToken cancellationToken)
    {
        _db.CheminsAttaque.Remove(chemin);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
