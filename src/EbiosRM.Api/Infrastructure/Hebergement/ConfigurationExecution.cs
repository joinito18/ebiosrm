using System.Security.Cryptography;

namespace EbiosRM.Api.Infrastructure.Hebergement;

public enum FournisseurBaseDeDonnees { Postgres, Sqlite }

/// <summary>
/// Détermine comment l'application s'exécute, à partir de la configuration :
///
///  - <c>ConnectionStrings:EbiosDb</c> renseigné  → mode serveur, PostgreSQL
///    (déploiement hébergé, <c>docker compose -f docker-compose.selfhost.yml</c>).
///  - rien de renseigné                           → mode bureau, SQLite dans le
///    dossier de données local de l'utilisateur, secret JWT auto-généré,
///    ouverture du navigateur au démarrage. C'est le cas du .exe double-cliqué.
///
/// <c>Database:Provider</c> (Postgres|Sqlite) force explicitement le fournisseur.
/// </summary>
public sealed class ConfigurationExecution
{
    public FournisseurBaseDeDonnees Fournisseur { get; }
    public string ChaineConnexion { get; }
    public bool ModeBureau { get; }
    public string DossierDonnees { get; }

    /// <summary>Chemin du fichier .db en mode SQLite, <c>null</c> sinon.</summary>
    public string? FichierSqlite { get; }

    private ConfigurationExecution(FournisseurBaseDeDonnees fournisseur, string chaineConnexion, bool modeBureau, string dossierDonnees, string? fichierSqlite)
    {
        Fournisseur = fournisseur;
        ChaineConnexion = chaineConnexion;
        ModeBureau = modeBureau;
        DossierDonnees = dossierDonnees;
        FichierSqlite = fichierSqlite;
    }

    public static ConfigurationExecution Determiner(IConfiguration configuration)
    {
        var chainePostgres = configuration.GetConnectionString("EbiosDb");
        var fournisseurForce = configuration["Database:Provider"];
        var forcePostgres = string.Equals(fournisseurForce, "Postgres", StringComparison.OrdinalIgnoreCase);
        var forceSqlite = string.Equals(fournisseurForce, "Sqlite", StringComparison.OrdinalIgnoreCase);

        var utiliserPostgres = forcePostgres || (!forceSqlite && !string.IsNullOrWhiteSpace(chainePostgres));

        if (utiliserPostgres)
        {
            return new ConfigurationExecution(
                FournisseurBaseDeDonnees.Postgres,
                chainePostgres ?? throw new InvalidOperationException("ConnectionStrings:EbiosDb est requis en mode PostgreSQL."),
                modeBureau: false,
                dossierDonnees: AppContext.BaseDirectory,
                fichierSqlite: null);
        }

        var dossier = configuration["App:DossierDonnees"] ?? DossierDonneesParDefaut();
        Directory.CreateDirectory(dossier);
        var fichierDb = Path.Combine(dossier, "ebiosrm.db");

        // Mode bureau = SQLite choisi par défaut (ni chaîne Postgres ni provider
        // forcé) : c'est là qu'on ouvre le navigateur et qu'on auto-génère le
        // secret JWT. Un "Database:Provider=Sqlite" explicite reste un mode
        // serveur (pas d'ouverture de navigateur).
        var modeBureau = string.IsNullOrWhiteSpace(chainePostgres) && string.IsNullOrWhiteSpace(fournisseurForce);

        return new ConfigurationExecution(FournisseurBaseDeDonnees.Sqlite, $"Data Source={fichierDb}", modeBureau, dossier, fichierDb);
    }

    /// <summary>
    /// Tout premier lancement en mode bureau : si aucune base n'existe encore
    /// et que l'application embarque une base d'exemple (ressource
    /// <c>ebiosrm.seed.db</c>), la déposer -- l'utilisateur découvre l'outil
    /// avec une étude déjà remplie plutôt qu'un écran vide.
    /// <c>App:ChargerExemple=false</c> pour démarrer sur une base vierge.
    /// </summary>
    public void DeposerBaseExempleSiPremierLancement(IConfiguration configuration)
    {
        if (!ModeBureau
            || FichierSqlite is null
            || File.Exists(FichierSqlite)
            || !configuration.GetValue("App:ChargerExemple", true))
            return;

        var assembly = typeof(ConfigurationExecution).Assembly;
        var ressource = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("ebiosrm.seed.db", StringComparison.OrdinalIgnoreCase));
        if (ressource is null)
            return;

        using var source = assembly.GetManifestResourceStream(ressource);
        if (source is null)
            return;
        using var cible = File.Create(FichierSqlite);
        source.CopyTo(cible);
    }

    private static string DossierDonneesParDefaut()
    {
        var racine = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(racine))
            racine = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
        return Path.Combine(racine, "EbiosRM");
    }

    /// <summary>
    /// Secret de signature des jetons JWT. En mode serveur il provient de la
    /// configuration (<c>Jwt:Secret</c>). En mode bureau, aucun humain ne le
    /// renseigne : on en génère un à la première exécution et on le conserve
    /// dans le dossier de données, pour que les sessions survivent aux
    /// redémarrages.
    /// </summary>
    public string ResoudreSecretJwt(IConfiguration configuration)
    {
        var configure = configuration["Jwt:Secret"];
        if (!string.IsNullOrWhiteSpace(configure))
            return configure;

        if (!ModeBureau)
            throw new InvalidOperationException(
                "Jwt:Secret doit être configuré (appsettings ou variable d'environnement Jwt__Secret).");

        var fichierCle = Path.Combine(DossierDonnees, "jwt.key");
        if (File.Exists(fichierCle))
        {
            var existant = File.ReadAllText(fichierCle).Trim();
            if (!string.IsNullOrWhiteSpace(existant))
                return existant;
        }

        var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        File.WriteAllText(fichierCle, secret);
        return secret;
    }
}
