using EbiosRM.Api.Modules.Identity.Domain;
using EbiosRM.Api.Tests.TestDoubles;
using Microsoft.Extensions.Configuration;

namespace EbiosRM.Api.Tests.Domain.Identity;

public class UtilisateurTests
{
    [Fact]
    public void Creer_normalise_l_email_en_minuscules_et_trim()
    {
        var utilisateur = Utilisateur.Creer("  Test@Exemple.COM  ", "Test", "hash");

        Assert.Equal("test@exemple.com", utilisateur.Email);
    }

    [Fact]
    public void Creer_refuse_un_email_vide()
    {
        Assert.Throws<ArgumentException>(() => Utilisateur.Creer("", "Test", "hash"));
    }

    [Fact]
    public void Creer_refuse_un_nom_affiche_vide()
    {
        Assert.Throws<ArgumentException>(() => Utilisateur.Creer("test@exemple.com", "", "hash"));
    }
}

public class ServiceAuthentificationTests
{
    private static ServiceAuthentification CreerService(FakeUtilisateurRepository? repository = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Secret"] = "test-secret-do-not-use-in-production-0123456789abcdef" })
            .Build();
        return new ServiceAuthentification(repository ?? new FakeUtilisateurRepository(), config);
    }

    [Fact]
    public async Task InscrireAsync_cree_l_utilisateur_et_retourne_un_jeton()
    {
        var service = CreerService();

        var (token, utilisateur) = await service.InscrireAsync("nouveau@exemple.com", "MotDePasse123", "Nouveau", CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Equal("nouveau@exemple.com", utilisateur.Email);
        Assert.NotEqual("MotDePasse123", utilisateur.MotDePasseHache); // jamais en clair
    }

    [Fact]
    public async Task InscrireAsync_refuse_un_email_deja_utilise()
    {
        var repository = new FakeUtilisateurRepository();
        var service = CreerService(repository);
        await service.InscrireAsync("existe@exemple.com", "MotDePasse123", "Premier", CancellationToken.None);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.InscrireAsync("existe@exemple.com", "AutreMotDePasse123", "Second", CancellationToken.None));
    }

    [Fact]
    public async Task InscrireAsync_refuse_un_mot_de_passe_trop_court()
    {
        var service = CreerService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.InscrireAsync("test@exemple.com", "court", "Test", CancellationToken.None));
    }

    [Fact]
    public async Task ConnecterAsync_reussit_avec_le_bon_mot_de_passe()
    {
        var repository = new FakeUtilisateurRepository();
        var service = CreerService(repository);
        await service.InscrireAsync("connexion@exemple.com", "MotDePasse123", "Test", CancellationToken.None);

        var resultat = await service.ConnecterAsync("connexion@exemple.com", "MotDePasse123", CancellationToken.None);

        Assert.NotNull(resultat);
        Assert.False(string.IsNullOrWhiteSpace(resultat!.Value.Token));
    }

    [Fact]
    public async Task ConnecterAsync_echoue_avec_un_mauvais_mot_de_passe()
    {
        var repository = new FakeUtilisateurRepository();
        var service = CreerService(repository);
        await service.InscrireAsync("connexion2@exemple.com", "MotDePasse123", "Test", CancellationToken.None);

        var resultat = await service.ConnecterAsync("connexion2@exemple.com", "MauvaisMotDePasse", CancellationToken.None);

        Assert.Null(resultat);
    }

    [Fact]
    public async Task ConnecterAsync_echoue_si_l_email_est_inconnu()
    {
        var service = CreerService();

        var resultat = await service.ConnecterAsync("inconnu@exemple.com", "MotDePasse123", CancellationToken.None);

        Assert.Null(resultat);
    }
}
