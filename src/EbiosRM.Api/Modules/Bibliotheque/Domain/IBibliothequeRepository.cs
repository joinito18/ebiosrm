namespace EbiosRM.Api.Modules.Bibliotheque.Domain;

/// <summary>
/// Accès aux entrées de bibliothèque <b>personnelles</b> uniquement (celles qui
/// sont persistées). Le catalogue système vient de <see cref="CatalogueSysteme"/>,
/// pas de la base.
/// </summary>
public interface IBibliothequeRepository
{
    Task<List<MesureBibliotheque>> ListerMesuresAsync(Guid proprietaireId, CancellationToken cancellationToken);
    Task<MesureBibliotheque?> ObtenirMesureAsync(Guid id, CancellationToken cancellationToken);
    Task AjouterMesureAsync(MesureBibliotheque mesure, CancellationToken cancellationToken);
    Task SupprimerMesureAsync(MesureBibliotheque mesure, CancellationToken cancellationToken);

    Task<List<SourceRisqueBibliotheque>> ListerSourcesRisqueAsync(Guid proprietaireId, CancellationToken cancellationToken);
    Task<SourceRisqueBibliotheque?> ObtenirSourceRisqueAsync(Guid id, CancellationToken cancellationToken);
    Task AjouterSourceRisqueAsync(SourceRisqueBibliotheque source, CancellationToken cancellationToken);
    Task SupprimerSourceRisqueAsync(SourceRisqueBibliotheque source, CancellationToken cancellationToken);
}
