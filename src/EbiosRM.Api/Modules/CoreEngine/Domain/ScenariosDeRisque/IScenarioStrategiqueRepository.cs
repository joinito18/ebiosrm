namespace EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

public interface IScenarioStrategiqueRepository
{
    Task AjouterAsync(ScenarioStrategique scenario, CancellationToken cancellationToken);
    Task<ScenarioStrategique?> ObtenirParIdAsync(Guid id, CancellationToken cancellationToken);
    Task<ScenarioStrategique?> ObtenirParCoupleIdAsync(Guid coupleSourceRisqueObjectifViseId, CancellationToken cancellationToken);
    Task<List<ScenarioStrategique>> ListerParEtudeAsync(Guid etudeId, CancellationToken cancellationToken);
    Task MettreAJourAsync(ScenarioStrategique scenario, CancellationToken cancellationToken);
    Task SupprimerAsync(ScenarioStrategique scenario, CancellationToken cancellationToken);
}
