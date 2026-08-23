using EbiosRM.Api.Infrastructure.Persistence;
using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;
using Microsoft.EntityFrameworkCore;

namespace EbiosRM.Api.Modules.CoreEngine.Infrastructure;

public sealed class ScenarioStrategiqueRepository : IScenarioStrategiqueRepository
{
    private readonly EbiosDbContext _db;

    public ScenarioStrategiqueRepository(EbiosDbContext db)
    {
        _db = db;
    }

    public async Task AjouterAsync(ScenarioStrategique scenario, CancellationToken cancellationToken)
    {
        _db.ScenariosStrategiques.Add(scenario);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ScenarioStrategique?> ObtenirParIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.ScenariosStrategiques.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<ScenarioStrategique?> ObtenirParCoupleIdAsync(Guid coupleSourceRisqueObjectifViseId, CancellationToken cancellationToken)
    {
        return await _db.ScenariosStrategiques.FirstOrDefaultAsync(
            s => s.CoupleSourceRisqueObjectifViseId == coupleSourceRisqueObjectifViseId, cancellationToken);
    }

    public async Task<List<ScenarioStrategique>> ListerParEtudeAsync(Guid etudeId, CancellationToken cancellationToken)
    {
        return await _db.ScenariosStrategiques
            .Where(s => s.EtudeId == etudeId)
            .ToListAsync(cancellationToken);
    }

    public async Task MettreAJourAsync(ScenarioStrategique scenario, CancellationToken cancellationToken)
    {
        _db.ScenariosStrategiques.Update(scenario);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SupprimerAsync(ScenarioStrategique scenario, CancellationToken cancellationToken)
    {
        _db.ScenariosStrategiques.Remove(scenario);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
