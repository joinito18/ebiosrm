namespace EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

public interface IEtudeRepository
{
    Task AjouterAsync(Etude etude, CancellationToken cancellationToken);
    Task<Etude?> ObtenirParIdAsync(Guid id, CancellationToken cancellationToken);
    Task<List<Etude>> ListerAsync(CancellationToken cancellationToken);
    /// <summary>Etudes visibles pour un utilisateur : les siennes, plus les etudes de demonstration publiques (ProprietaireId null).</summary>
    Task<List<Etude>> ListerVisiblesAsync(Guid utilisateurId, CancellationToken cancellationToken);
    Task MettreAJourAsync(Etude etude, CancellationToken cancellationToken);
}
