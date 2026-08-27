namespace EbiosRM.Api.Modules.Identity.Domain;

/// <summary>
/// Envoi des emails transactionnels de l'authentification. Abstraction : en
/// production l'implémentation appelle Resend, en dev/test/CI (aucune clé
/// configurée) une implémentation qui se contente de journaliser le lien --
/// aucun secret requis pour faire tourner l'application localement.
/// </summary>
public interface IServiceEmail
{
    Task EnvoyerLienReinitialisationAsync(string destinataire, string lienReinitialisation, CancellationToken cancellationToken);
}
