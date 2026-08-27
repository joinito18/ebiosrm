using EbiosRM.Api.Modules.Identity.Domain;
using EbiosRM.Api.Tests.TestDoubles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

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

    [Fact]
    public void DemarrerReinitialisation_ne_stocke_jamais_le_jeton_en_clair()
    {
        var utilisateur = Utilisateur.Creer("test@exemple.com", "Test", "hash");

        utilisateur.DemarrerReinitialisation("jeton-en-clair-secret", DateTime.UtcNow);

        Assert.NotNull(utilisateur.JetonReinitialisationHache);
        Assert.NotEqual("jeton-en-clair-secret", utilisateur.JetonReinitialisationHache);
        Assert.Equal(Utilisateur.HacherJetonReinitialisation("jeton-en-clair-secret"), utilisateur.JetonReinitialisationHache);
    }

    [Fact]
    public void EssayerReinitialiser_reussit_avec_le_bon_jeton_puis_le_consomme()
    {
        var utilisateur = Utilisateur.Creer("test@exemple.com", "Test", "ancien-hash");
        var maintenant = DateTime.UtcNow;
        utilisateur.DemarrerReinitialisation("bon-jeton", maintenant);

        var premier = utilisateur.EssayerReinitialiserMotDePasse("bon-jeton", "nouveau-hash", maintenant.AddMinutes(5));
        var second = utilisateur.EssayerReinitialiserMotDePasse("bon-jeton", "encore-un-hash", maintenant.AddMinutes(6));

        Assert.True(premier);
        Assert.Equal("nouveau-hash", utilisateur.MotDePasseHache);
        Assert.False(second); // jeton consommé : ne sert qu'une fois
        Assert.Null(utilisateur.JetonReinitialisationHache);
    }

    [Fact]
    public void EssayerReinitialiser_echoue_avec_un_mauvais_jeton()
    {
        var utilisateur = Utilisateur.Creer("test@exemple.com", "Test", "ancien-hash");
        var maintenant = DateTime.UtcNow;
        utilisateur.DemarrerReinitialisation("bon-jeton", maintenant);

        var resultat = utilisateur.EssayerReinitialiserMotDePasse("mauvais-jeton", "nouveau-hash", maintenant.AddMinutes(5));

        Assert.False(resultat);
        Assert.Equal("ancien-hash", utilisateur.MotDePasseHache);
        Assert.NotNull(utilisateur.JetonReinitialisationHache); // toujours actif
    }

    [Fact]
    public void EssayerReinitialiser_echoue_apres_expiration()
    {
        var utilisateur = Utilisateur.Creer("test@exemple.com", "Test", "ancien-hash");
        var maintenant = DateTime.UtcNow;
        utilisateur.DemarrerReinitialisation("bon-jeton", maintenant);

        var resultat = utilisateur.EssayerReinitialiserMotDePasse(
            "bon-jeton", "nouveau-hash", maintenant.Add(Utilisateur.DureeValiditeJetonReinitialisation).AddSeconds(1));

        Assert.False(resultat);
        Assert.Equal("ancien-hash", utilisateur.MotDePasseHache);
    }

    [Fact]
    public void EssayerReinitialiser_echoue_si_aucune_demande_active()
    {
        var utilisateur = Utilisateur.Creer("test@exemple.com", "Test", "ancien-hash");

        var resultat = utilisateur.EssayerReinitialiserMotDePasse("nimporte", "nouveau-hash", DateTime.UtcNow);

        Assert.False(resultat);
    }
}

public class ServiceAuthentificationTests
{
    private static ServiceAuthentification CreerService(
        FakeUtilisateurRepository? repository = null,
        FakeServiceEmail? email = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "test-secret-do-not-use-in-production-0123456789abcdef",
                ["App:UrlFrontend"] = "https://frontend.test",
            })
            .Build();
        return new ServiceAuthentification(
            repository ?? new FakeUtilisateurRepository(),
            config,
            email ?? new FakeServiceEmail(),
            NullLogger<ServiceAuthentification>.Instance);
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

    [Fact]
    public async Task DemanderReinitialisationAsync_email_inconnu_n_envoie_rien_et_ne_leve_pas()
    {
        var email = new FakeServiceEmail();
        var service = CreerService(email: email);

        await service.DemanderReinitialisationAsync("inconnu@exemple.com", CancellationToken.None);

        Assert.Equal(0, email.NombreEnvois);
    }

    [Fact]
    public async Task DemanderReinitialisationAsync_envoie_un_lien_contenant_un_jeton()
    {
        var repository = new FakeUtilisateurRepository();
        var email = new FakeServiceEmail();
        var service = CreerService(repository, email);
        await service.InscrireAsync("reset@exemple.com", "MotDePasse123", "Test", CancellationToken.None);

        await service.DemanderReinitialisationAsync("  Reset@Exemple.com ", CancellationToken.None);

        Assert.Equal(1, email.NombreEnvois);
        Assert.Equal("reset@exemple.com", email.DernierDestinataire);
        Assert.StartsWith("https://frontend.test/reinitialiser-mot-de-passe?token=", email.DernierLien);
    }

    [Fact]
    public async Task ReinitialiserMotDePasseAsync_flux_complet_change_le_mot_de_passe()
    {
        var repository = new FakeUtilisateurRepository();
        var email = new FakeServiceEmail();
        var service = CreerService(repository, email);
        await service.InscrireAsync("flux@exemple.com", "AncienMotDePasse1", "Test", CancellationToken.None);
        await service.DemanderReinitialisationAsync("flux@exemple.com", CancellationToken.None);
        var jeton = ExtraireJeton(email.DernierLien!);

        var reussi = await service.ReinitialiserMotDePasseAsync(jeton, "NouveauMotDePasse1", CancellationToken.None);

        Assert.True(reussi);
        Assert.Null(await service.ConnecterAsync("flux@exemple.com", "AncienMotDePasse1", CancellationToken.None));
        Assert.NotNull(await service.ConnecterAsync("flux@exemple.com", "NouveauMotDePasse1", CancellationToken.None));
    }

    [Fact]
    public async Task ReinitialiserMotDePasseAsync_jeton_inconnu_renvoie_false()
    {
        var service = CreerService();

        var reussi = await service.ReinitialiserMotDePasseAsync("jeton-bidon", "NouveauMotDePasse1", CancellationToken.None);

        Assert.False(reussi);
    }

    [Fact]
    public async Task ReinitialiserMotDePasseAsync_mot_de_passe_trop_court_leve()
    {
        var repository = new FakeUtilisateurRepository();
        var email = new FakeServiceEmail();
        var service = CreerService(repository, email);
        await service.InscrireAsync("court@exemple.com", "AncienMotDePasse1", "Test", CancellationToken.None);
        await service.DemanderReinitialisationAsync("court@exemple.com", CancellationToken.None);
        var jeton = ExtraireJeton(email.DernierLien!);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ReinitialiserMotDePasseAsync(jeton, "court", CancellationToken.None));
    }

    private static string ExtraireJeton(string lien)
    {
        var query = new Uri(lien).Query.TrimStart('?');
        var paire = query.Split('&').First(p => p.StartsWith("token="));
        return Uri.UnescapeDataString(paire["token=".Length..]);
    }
}
