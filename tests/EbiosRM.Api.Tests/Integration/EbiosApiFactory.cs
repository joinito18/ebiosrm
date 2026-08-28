using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EbiosRM.Api.Tests.Integration;

/// <summary>
/// Démarre l'API réelle (Program.cs, tous les endpoints, toute la pile DI)
/// contre une base Postgres de test dédiée ("ebiosrm_test", même serveur
/// que le Postgres de dev, schéma appliqué via les mêmes migrations EF --
/// jamais la base "ebiosrm" utilisée pour les données de démo).
/// Chaque test crée sa propre Étude (Guid unique), donc pas de nettoyage
/// entre tests nécessaire pour l'isolation -- les tables ne sont jamais
/// vidées, seulement jamais réutilisées par deux tests différents.
/// </summary>
public sealed class EbiosApiFactory : WebApplicationFactory<Program>
{
    static EbiosApiFactory()
    {
        // ConfigurationExecution.Determiner (Program.cs) lit builder.Configuration
        // des WebApplication.CreateBuilder, AVANT que WebApplicationFactory n'ait
        // fusionne un ConfigureAppConfiguration. Il faut donc passer par des
        // variables d'environnement, deja presentes a ce moment-la -- sinon les
        // tests basculeraient en mode bureau SQLite (dossier de donnees de
        // l'utilisateur, ouverture du navigateur) au lieu du Postgres de test.
        Environment.SetEnvironmentVariable("Database__Provider", "Postgres");
        Environment.SetEnvironmentVariable("ConnectionStrings__EbiosDb",
            "Host=localhost;Port=5433;Database=ebiosrm_test;Username=ebiosrm;Password=ebiosrm_dev");
        Environment.SetEnvironmentVariable("Jwt__Secret",
            "test-secret-do-not-use-in-production-0123456789abcdef");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}
