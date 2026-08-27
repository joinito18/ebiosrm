using System.Net.Http.Headers;
using System.Net.Http.Json;
using EbiosRM.Api.Modules.Identity.Domain;
using Microsoft.Extensions.Options;

namespace EbiosRM.Api.Modules.Identity.Infrastructure;

/// <summary>
/// Envoi d'email via l'API HTTP de Resend (https://resend.com/docs/api-reference).
/// Enregistré uniquement quand <see cref="OptionsResend.EstConfigure"/> est vrai.
/// </summary>
public sealed class ServiceEmailResend : IServiceEmail
{
    private readonly HttpClient _http;
    private readonly OptionsResend _options;
    private readonly ILogger<ServiceEmailResend> _logger;

    public ServiceEmailResend(HttpClient http, IOptions<OptionsResend> options, ILogger<ServiceEmailResend> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
        _http.BaseAddress ??= new Uri("https://api.resend.com/");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    public async Task EnvoyerLienReinitialisationAsync(string destinataire, string lienReinitialisation, CancellationToken cancellationToken)
    {
        var corps = new
        {
            from = _options.Expediteur,
            to = new[] { destinataire },
            subject = "Réinitialisation de votre mot de passe EBIOS RM",
            html = ConstruireHtml(lienReinitialisation),
            text = ConstruireTexte(lienReinitialisation),
        };

        using var reponse = await _http.PostAsJsonAsync("emails", corps, cancellationToken);
        if (!reponse.IsSuccessStatusCode)
        {
            var detail = await reponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Échec de l'envoi Resend ({Statut}) : {Detail}", (int)reponse.StatusCode, detail);
            throw new InvalidOperationException($"L'envoi de l'email a échoué (Resend a répondu {(int)reponse.StatusCode}).");
        }
    }

    private static string ConstruireTexte(string lien) =>
        "Vous avez demandé la réinitialisation de votre mot de passe EBIOS RM.\n\n" +
        $"Ouvrez ce lien pour choisir un nouveau mot de passe (valable 1 heure) :\n{lien}\n\n" +
        "Si vous n'êtes pas à l'origine de cette demande, ignorez cet email : votre mot de passe reste inchangé.";

    private static string ConstruireHtml(string lien)
    {
        var lienEchappe = System.Net.WebUtility.HtmlEncode(lien);
        return $"""
        <div style="font-family:-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;font-size:15px;color:#1a1a1a;line-height:1.6">
          <p>Vous avez demandé la réinitialisation de votre mot de passe <strong>EBIOS RM</strong>.</p>
          <p>
            <a href="{lienEchappe}" style="display:inline-block;background:#1a1a1a;color:#fff;text-decoration:none;padding:10px 18px;border-radius:4px">
              Choisir un nouveau mot de passe
            </a>
          </p>
          <p style="color:#666;font-size:13px">Ce lien est valable 1 heure. Si le bouton ne fonctionne pas, copiez cette adresse dans votre navigateur :<br>{lienEchappe}</p>
          <p style="color:#666;font-size:13px">Si vous n'êtes pas à l'origine de cette demande, ignorez cet email : votre mot de passe reste inchangé.</p>
        </div>
        """;
    }
}
