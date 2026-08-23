using EbiosRM.Api.Infrastructure.Persistence;
using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;
using Microsoft.EntityFrameworkCore;

namespace EbiosRM.Api.Modules.CoreEngine.Infrastructure;

public sealed class PlanTraitementRisqueRepository : IPlanTraitementRisqueRepository
{
    private readonly EbiosDbContext _db;

    public PlanTraitementRisqueRepository(EbiosDbContext db)
    {
        _db = db;
    }

    public async Task AjouterAsync(PlanTraitementRisque plan, CancellationToken cancellationToken)
    {
        _db.PlansTraitementRisque.Add(plan);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PlanTraitementRisque?> ObtenirParEtudeAsync(Guid etudeId, CancellationToken cancellationToken)
    {
        return await _db.PlansTraitementRisque
            .Include(p => p.Mesures)
            .FirstOrDefaultAsync(p => p.EtudeId == etudeId, cancellationToken);
    }

    public async Task MettreAJourAsync(PlanTraitementRisque plan, CancellationToken cancellationToken)
    {
        // Ne PAS appeler Update() -- entité déjà suivie par ce DbContext, a une
        // collection owned Mesures (cf. leçon ScenarioOperationnelRepository).
        await _db.SaveChangesAsync(cancellationToken);
    }
}
