namespace EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;

public interface IPartiePrenanteRepository
{
    Task AjouterAsync(PartiePrenante partiePrenante, CancellationToken cancellationToken);
    Task<PartiePrenante?> ObtenirParIdAsync(Guid id, CancellationToken cancellationToken);
    Task<List<PartiePrenante>> ListerParEtudeAsync(Guid etudeId, CancellationToken cancellationToken);
    Task MettreAJourAsync(PartiePrenante partiePrenante, CancellationToken cancellationToken);
    Task SupprimerAsync(PartiePrenante partiePrenante, CancellationToken cancellationToken);
}
