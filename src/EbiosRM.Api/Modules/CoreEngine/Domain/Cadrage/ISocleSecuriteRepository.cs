namespace EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

public interface ISocleSecuriteRepository
{
    Task AjouterAsync(SocleSecurite socleSecurite, CancellationToken cancellationToken);
    Task<SocleSecurite?> ObtenirParEtudeAsync(Guid etudeId, CancellationToken cancellationToken);
    Task MettreAJourAsync(SocleSecurite socleSecurite, CancellationToken cancellationToken);
}
