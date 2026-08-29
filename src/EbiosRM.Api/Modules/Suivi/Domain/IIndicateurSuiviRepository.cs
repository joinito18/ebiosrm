namespace EbiosRM.Api.Modules.Suivi.Domain;

public interface IIndicateurSuiviRepository
{
    Task<List<IndicateurSuivi>> ListerParEtudeAsync(Guid etudeId, CancellationToken cancellationToken);
    Task<IndicateurSuivi?> ObtenirAsync(Guid id, CancellationToken cancellationToken);
    Task AjouterAsync(IndicateurSuivi indicateur, CancellationToken cancellationToken);
    Task MettreAJourAsync(IndicateurSuivi indicateur, CancellationToken cancellationToken);
    Task SupprimerAsync(IndicateurSuivi indicateur, CancellationToken cancellationToken);
}
