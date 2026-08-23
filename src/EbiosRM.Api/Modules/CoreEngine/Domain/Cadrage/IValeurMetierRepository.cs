namespace EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

public interface IValeurMetierRepository
{
    Task AjouterAsync(ValeurMetier valeurMetier, CancellationToken cancellationToken);
    Task<ValeurMetier?> ObtenirParIdAsync(Guid id, CancellationToken cancellationToken);
    Task<List<ValeurMetier>> ListerParEtudeAsync(Guid etudeId, CancellationToken cancellationToken);
    Task MettreAJourAsync(ValeurMetier valeurMetier, CancellationToken cancellationToken);
    Task SupprimerAsync(ValeurMetier valeurMetier, CancellationToken cancellationToken);
}
