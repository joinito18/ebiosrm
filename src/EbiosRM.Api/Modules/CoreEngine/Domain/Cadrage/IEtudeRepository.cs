namespace EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

public interface IEtudeRepository
{
    Task AjouterAsync(Etude etude, CancellationToken cancellationToken);
    Task<Etude?> ObtenirParIdAsync(Guid id, CancellationToken cancellationToken);
    Task<List<Etude>> ListerAsync(CancellationToken cancellationToken);
    Task MettreAJourAsync(Etude etude, CancellationToken cancellationToken);
}
