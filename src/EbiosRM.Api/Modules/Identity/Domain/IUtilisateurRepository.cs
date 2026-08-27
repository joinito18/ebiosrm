namespace EbiosRM.Api.Modules.Identity.Domain;

public interface IUtilisateurRepository
{
    Task AjouterAsync(Utilisateur utilisateur, CancellationToken cancellationToken);
    Task<Utilisateur?> ObtenirParIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Utilisateur?> ObtenirParEmailAsync(string email, CancellationToken cancellationToken);
    Task<Utilisateur?> ObtenirParJetonReinitialisationHacheAsync(string jetonHache, CancellationToken cancellationToken);
    Task MettreAJourAsync(Utilisateur utilisateur, CancellationToken cancellationToken);
}
