namespace EbiosRM.Api.Modules.Bibliotheque.Domain;

/// <summary>
/// Accès aux entrées de bibliothèque <b>personnelles</b> uniquement (celles qui
/// sont persistées). Le catalogue système vient de <see cref="CatalogueSysteme"/>,
/// pas de la base.
///
/// Générique : un seul jeu de méthodes pour tous les types d'entrées
/// (<see cref="MesureBibliotheque"/>, <see cref="SourceRisqueBibliotheque"/>,
/// <see cref="PartiePrenanteBibliotheque"/>, <see cref="ValeurMetierBibliotheque"/>,
/// <see cref="BienSupportBibliotheque"/>, <see cref="EvenementRedouteBibliotheque"/>...).
/// </summary>
public interface IBibliothequeRepository
{
    Task<List<T>> ListerAsync<T>(Guid proprietaireId, CancellationToken cancellationToken) where T : class, IEntreeBibliotheque;
    Task<T?> ObtenirAsync<T>(Guid id, CancellationToken cancellationToken) where T : class, IEntreeBibliotheque;
    Task AjouterAsync<T>(T entree, CancellationToken cancellationToken) where T : class, IEntreeBibliotheque;
    Task SupprimerAsync<T>(T entree, CancellationToken cancellationToken) where T : class, IEntreeBibliotheque;
}
