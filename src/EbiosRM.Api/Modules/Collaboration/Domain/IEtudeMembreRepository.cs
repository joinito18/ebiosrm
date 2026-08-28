namespace EbiosRM.Api.Modules.Collaboration.Domain;

public interface IEtudeMembreRepository
{
    Task<EtudeMembre?> ObtenirAsync(Guid etudeId, Guid utilisateurId, CancellationToken cancellationToken);
    Task<List<EtudeMembre>> ListerParEtudeAsync(Guid etudeId, CancellationToken cancellationToken);
    Task<List<EtudeMembre>> ListerParUtilisateurAsync(Guid utilisateurId, CancellationToken cancellationToken);
    Task<int> CompterProprietairesAsync(Guid etudeId, CancellationToken cancellationToken);
    Task AjouterAsync(EtudeMembre membre, CancellationToken cancellationToken);
    Task MettreAJourAsync(EtudeMembre membre, CancellationToken cancellationToken);
    Task SupprimerAsync(EtudeMembre membre, CancellationToken cancellationToken);
}
