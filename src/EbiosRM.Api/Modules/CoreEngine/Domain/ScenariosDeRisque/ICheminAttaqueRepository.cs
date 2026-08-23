namespace EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

public interface ICheminAttaqueRepository
{
    Task AjouterAsync(CheminAttaque chemin, CancellationToken cancellationToken);
    Task<CheminAttaque?> ObtenirParIdAsync(Guid id, CancellationToken cancellationToken);
    Task<List<CheminAttaque>> ListerParEtudeAsync(Guid etudeId, CancellationToken cancellationToken);
    Task<List<CheminAttaque>> ListerParScenarioAsync(Guid scenarioStrategiqueId, CancellationToken cancellationToken);
    Task MettreAJourAsync(CheminAttaque chemin, CancellationToken cancellationToken);
    Task SupprimerAsync(CheminAttaque chemin, CancellationToken cancellationToken);
}
