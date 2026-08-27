using EbiosRM.Api.Modules.Identity.Domain;

namespace EbiosRM.Api.Modules.Identity.Infrastructure;

/// <summary>
/// Implémentation par défaut quand aucun fournisseur d'email n'est configuré
/// (dev, tests, CI) : n'envoie rien, écrit le lien dans les logs pour qu'un
/// développeur puisse suivre le parcours de bout en bout sans boîte mail.
/// </summary>
public sealed class ServiceEmailJournalise : IServiceEmail
{
    private readonly ILogger<ServiceEmailJournalise> _logger;

    public ServiceEmailJournalise(ILogger<ServiceEmailJournalise> logger)
    {
        _logger = logger;
    }

    public Task EnvoyerLienReinitialisationAsync(string destinataire, string lienReinitialisation, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Email de réinitialisation NON envoyé (aucun fournisseur configuré). Destinataire={Destinataire} Lien={Lien}",
            destinataire, lienReinitialisation);
        return Task.CompletedTask;
    }
}
