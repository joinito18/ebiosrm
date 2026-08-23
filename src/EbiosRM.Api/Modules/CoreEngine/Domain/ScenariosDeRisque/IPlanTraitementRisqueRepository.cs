namespace EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

public interface IPlanTraitementRisqueRepository
{
    Task AjouterAsync(PlanTraitementRisque plan, CancellationToken cancellationToken);
    Task<PlanTraitementRisque?> ObtenirParEtudeAsync(Guid etudeId, CancellationToken cancellationToken);
    Task MettreAJourAsync(PlanTraitementRisque plan, CancellationToken cancellationToken);
}
