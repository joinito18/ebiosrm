namespace EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

public interface IValeurMetierRepository
{
    Task AjouterAsync(ValeurMetier valeurMetier, CancellationToken cancellationToken);
    Task<List<ValeurMetier>> ListerParEtudeAsync(Guid etudeId, CancellationToken cancellationToken);
}
