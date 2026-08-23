using EbiosRM.Api.Infrastructure.Persistence;
using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;
using Microsoft.EntityFrameworkCore;

namespace EbiosRM.Api.Modules.CoreEngine.Infrastructure;

public sealed class ScenarioDeRisqueRepository : IScenarioDeRisqueRepository
{
    private readonly EbiosDbContext _db;

    public ScenarioDeRisqueRepository(EbiosDbContext db)
    {
        _db = db;
    }

    public async Task AjouterAsync(ScenarioDeRisque scenario, CancellationToken cancellationToken)
    {
        _db.ScenariosDeRisque.Add(scenario);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ScenarioDeRisque?> ObtenirParIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.ScenariosDeRisque.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<ScenarioDeRisque?> ObtenirParCheminIdAsync(Guid cheminAttaqueId, CancellationToken cancellationToken)
    {
        return await _db.ScenariosDeRisque.FirstOrDefaultAsync(s => s.CheminAttaqueId == cheminAttaqueId, cancellationToken);
    }

    public async Task<List<ScenarioDeRisque>> ListerParEtudeAsync(Guid etudeId, CancellationToken cancellationToken)
    {
        return await _db.ScenariosDeRisque.Where(s => s.EtudeId == etudeId).ToListAsync(cancellationToken);
    }

    public async Task MettreAJourAsync(ScenarioDeRisque scenario, CancellationToken cancellationToken)
    {
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SupprimerAsync(ScenarioDeRisque scenario, CancellationToken cancellationToken)
    {
        _db.ScenariosDeRisque.Remove(scenario);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
