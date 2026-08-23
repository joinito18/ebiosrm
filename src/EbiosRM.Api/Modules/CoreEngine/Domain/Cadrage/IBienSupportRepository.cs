namespace EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

public interface IBienSupportRepository
{
    Task AjouterAsync(BienSupport bienSupport, CancellationToken cancellationToken);
    Task<BienSupport?> ObtenirParIdAsync(Guid id, CancellationToken cancellationToken);
    Task<List<BienSupport>> ListerParEtudeAsync(Guid etudeId, CancellationToken cancellationToken);
    Task<List<BienSupport>> ListerParValeurMetierAsync(Guid valeurMetierId, CancellationToken cancellationToken);
    Task MettreAJourAsync(BienSupport bienSupport, CancellationToken cancellationToken);
    Task SupprimerAsync(BienSupport bienSupport, CancellationToken cancellationToken);
}
