namespace EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

public interface IScenarioDeRisqueRepository
{
    Task AjouterAsync(ScenarioDeRisque scenario, CancellationToken cancellationToken);
    Task<ScenarioDeRisque?> ObtenirParIdAsync(Guid id, CancellationToken cancellationToken);
    Task<ScenarioDeRisque?> ObtenirParCheminIdAsync(Guid cheminAttaqueId, CancellationToken cancellationToken);
    Task<List<ScenarioDeRisque>> ListerParEtudeAsync(Guid etudeId, CancellationToken cancellationToken);
    Task MettreAJourAsync(ScenarioDeRisque scenario, CancellationToken cancellationToken);
    Task SupprimerAsync(ScenarioDeRisque scenario, CancellationToken cancellationToken);
}
