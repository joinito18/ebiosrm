namespace EbiosRM.Api.Modules.Audit.Domain;

public interface IEntreeJournalRepository
{
    Task AjouterAsync(EntreeJournal entree, CancellationToken cancellationToken);
    Task<List<EntreeJournal>> ListerParEtudeAsync(Guid etudeId, int limite, CancellationToken cancellationToken);
}
