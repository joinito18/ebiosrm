namespace EbiosRM.Api.Modules.Identity.Infrastructure;

/// <summary>
/// Section de configuration "Resend". Renseignée en production via les
/// variables d'environnement Resend__ApiKey et Resend__Expediteur sur Render.
/// Absente en local : l'application bascule alors sur
/// <see cref="ServiceEmailJournalise"/>.
/// </summary>
public sealed class OptionsResend
{
    public const string Section = "Resend";

    /// <summary>Clé API Resend (préfixe "re_").</summary>
    public string? ApiKey { get; init; }

    /// <summary>
    /// Adresse d'expédition, sur un domaine vérifié chez Resend.
    /// Format accepté : "EBIOS RM &lt;no-reply@mon-domaine.me&gt;".
    /// </summary>
    public string? Expediteur { get; init; }

    public bool EstConfigure => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(Expediteur);
}
