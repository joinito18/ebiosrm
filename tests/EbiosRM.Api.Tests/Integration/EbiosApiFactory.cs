using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
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
/// </summary>
public sealed class EbiosApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:EbiosDb"] = "Host=localhost;Port=5433;Database=ebiosrm_test;Username=ebiosrm;Password=ebiosrm_dev",
                ["Jwt:Secret"] = "test-secret-do-not-use-in-production-0123456789abcdef"
            });
        });
    }
}
