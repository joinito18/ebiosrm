using EbiosRM.Api.Infrastructure.Persistence;
using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;
using Microsoft.EntityFrameworkCore;

namespace EbiosRM.Api.Modules.CoreEngine.Infrastructure;

public sealed class ScenarioOperationnelRepository : IScenarioOperationnelRepository
{
    private readonly EbiosDbContext _db;

    public ScenarioOperationnelRepository(EbiosDbContext db)
    {
        _db = db;
    }

    public async Task AjouterAsync(ScenarioOperationnel scenario, CancellationToken cancellationToken)
    {
        _db.ScenariosOperationnels.Add(scenario);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ScenarioOperationnel?> ObtenirParIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.ScenariosOperationnels.Include(s => s.ModesOperatoires).ThenInclude(m => m.ActionsElementaires)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<ScenarioOperationnel?> ObtenirParCheminIdAsync(Guid cheminAttaqueId, CancellationToken cancellationToken)
    {
        return await _db.ScenariosOperationnels.Include(s => s.ModesOperatoires).ThenInclude(m => m.ActionsElementaires)
            .FirstOrDefaultAsync(s => s.CheminAttaqueId == cheminAttaqueId, cancellationToken);
    }

    public async Task<List<ScenarioOperationnel>> ListerParEtudeAsync(Guid etudeId, CancellationToken cancellationToken)
    {
        return await _db.ScenariosOperationnels.Include(s => s.ModesOperatoires).ThenInclude(m => m.ActionsElementaires)
            .Where(s => s.EtudeId == etudeId)
            .ToListAsync(cancellationToken);
    }

    public async Task MettreAJourAsync(ScenarioOperationnel scenario, CancellationToken cancellationToken)
    {
        // Ne PAS appeler Update() -- entité déjà suivie par ce DbContext, a une
        // collection owned ModesOperatoires (cf. leçon CheminAttaqueRepository).
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SupprimerAsync(ScenarioOperationnel scenario, CancellationToken cancellationToken)
    {
        _db.ScenariosOperationnels.Remove(scenario);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
