namespace EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

public interface IBienSupportRepository
{
    Task AjouterAsync(BienSupport bienSupport, CancellationToken cancellationToken);
    Task<List<BienSupport>> ListerParEtudeAsync(Guid etudeId, CancellationToken cancellationToken);
    Task<List<BienSupport>> ListerParValeurMetierAsync(Guid valeurMetierId, CancellationToken cancellationToken);
}
