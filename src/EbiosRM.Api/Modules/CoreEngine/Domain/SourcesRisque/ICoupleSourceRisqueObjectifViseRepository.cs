namespace EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;

public interface ICoupleSourceRisqueObjectifViseRepository
{
    Task AjouterAsync(CoupleSourceRisqueObjectifVise couple, CancellationToken cancellationToken);
    Task<CoupleSourceRisqueObjectifVise?> ObtenirParIdAsync(Guid id, CancellationToken cancellationToken);
    Task<List<CoupleSourceRisqueObjectifVise>> ListerParEtudeAsync(Guid etudeId, CancellationToken cancellationToken);
    Task MettreAJourAsync(CoupleSourceRisqueObjectifVise couple, CancellationToken cancellationToken);
    Task SupprimerAsync(CoupleSourceRisqueObjectifVise couple, CancellationToken cancellationToken);
}
