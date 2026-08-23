namespace EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

public interface IScenarioOperationnelRepository
{
    Task AjouterAsync(ScenarioOperationnel scenario, CancellationToken cancellationToken);
    Task<ScenarioOperationnel?> ObtenirParIdAsync(Guid id, CancellationToken cancellationToken);
    Task<ScenarioOperationnel?> ObtenirParCheminIdAsync(Guid cheminAttaqueId, CancellationToken cancellationToken);
    Task<List<ScenarioOperationnel>> ListerParEtudeAsync(Guid etudeId, CancellationToken cancellationToken);
    Task MettreAJourAsync(ScenarioOperationnel scenario, CancellationToken cancellationToken);
    Task SupprimerAsync(ScenarioOperationnel scenario, CancellationToken cancellationToken);
}
