using System.Collections.Concurrent;
using EbiosRM.Api.Modules.Identity.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace EbiosRM.Api.Tests.Integration;

/// <summary>
/// Démarre l'API réelle (Program.cs, tous les endpoints, toute la pile DI)
/// contre une base Postgres de test dédiée ("ebiosrm_test", même serveur
/// que le Postgres de dev, schéma appliqué via les mêmes migrations EF --
/// jamais la base "ebiosrm" utilisée pour les données de démo BioGenTech).
/// Chaque test crée sa propre Étude (Guid unique), donc pas de nettoyage
/// entre tests nécessaire pour l'isolation -- les tables ne sont jamais
/// vidées, seulement jamais réutilisées par deux tests différents.
///
/// Seule dépendance externe court-circuitée : l'envoi d'email
/// (<see cref="IServiceEmail"/>), remplacé par <see cref="FauxServiceEmail"/>
/// qui capture le lien de réinitialisation au lieu de l'envoyer.
/// </summary>
public sealed class EbiosApiFactory : WebApplicationFactory<Program>
{
    public FauxServiceEmail Email { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:EbiosDb"] = "Host=localhost;Port=5433;Database=ebiosrm_test;Username=ebiosrm;Password=ebiosrm_dev",
                ["Jwt:Secret"] = "test-secret-do-not-use-in-production-0123456789abcdef",
                ["App:UrlFrontend"] = "https://frontend.test"
            });
        });
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IServiceEmail>();
            services.AddSingleton<IServiceEmail>(Email);
        });
    }
}

/// <summary>Capture le dernier lien de réinitialisation par destinataire.</summary>
public sealed class FauxServiceEmail : IServiceEmail
{
    public ConcurrentDictionary<string, string> LiensParDestinataire { get; } = new();

    public Task EnvoyerLienReinitialisationAsync(string destinataire, string lienReinitialisation, CancellationToken cancellationToken)
    {
        LiensParDestinataire[destinataire] = lienReinitialisation;
        return Task.CompletedTask;
    }
}
