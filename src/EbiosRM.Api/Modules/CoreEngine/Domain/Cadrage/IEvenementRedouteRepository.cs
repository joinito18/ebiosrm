namespace EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

public interface IEvenementRedouteRepository
{
    Task AjouterAsync(EvenementRedoute evenementRedoute, CancellationToken cancellationToken);
    Task<EvenementRedoute?> ObtenirParIdAsync(Guid id, CancellationToken cancellationToken);
    Task<List<EvenementRedoute>> ListerParEtudeAsync(Guid etudeId, CancellationToken cancellationToken);
    Task MettreAJourAsync(EvenementRedoute evenementRedoute, CancellationToken cancellationToken);
    Task SupprimerAsync(EvenementRedoute evenementRedoute, CancellationToken cancellationToken);
}
