using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using static EbiosRM.Api.Tests.Integration.OutilsEtudeDeTest;

namespace EbiosRM.Api.Tests.Integration;

/// <summary>
/// Bibliothèque d'éléments réutilisables : catalogue système (ISO 27002 +
/// hygiène ANSSI, dans le code, non modifiable) fusionné avec les entrées
/// personnelles de l'appelant (persistées, isolées par utilisateur).
/// </summary>
public class BibliothequeTests : IClassFixture<EbiosApiFactory>
{
    private readonly EbiosApiFactory _factory;

    public BibliothequeTests(EbiosApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Le_catalogue_systeme_de_mesures_est_toujours_present_et_non_modifiable()
    {
        var c = await NouveauCompteAsync(_factory, "biblio-cat");

        var toutes = await (await c.GetAsync("/api/v1/bibliotheque/mesures")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(toutes.GetArrayLength() >= 135); // 93 ISO 27002 + 42 hygiène ANSSI

        var iso = await (await c.GetAsync("/api/v1/bibliotheque/mesures?referentiel=Iso27002")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(93, iso.GetArrayLength());
        Assert.All(iso.EnumerateArray(), m => Assert.True(m.GetProperty("systeme").GetBoolean()));

        var anssi = await (await c.GetAsync("/api/v1/bibliotheque/mesures?referentiel=HygieneAnssi&q=sauvegarde")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, anssi.GetArrayLength());
        Assert.Contains("sauvegarde", anssi[0].GetProperty("titre").GetString(), StringComparison.OrdinalIgnoreCase);

        // Impossible de supprimer une entrée du catalogue système.
        var idSysteme = iso[0].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await c.DeleteAsync($"/api/v1/bibliotheque/mesures/{idSysteme}")).StatusCode);
    }

    [Fact]
    public async Task Une_mesure_personnelle_est_ajoutee_visible_puis_retiree()
    {
        var c = await NouveauCompteAsync(_factory, "biblio-perso");

        var ajout = await c.PostAsJsonAsync("/api/v1/bibliotheque/mesures", new
        {
            Titre = "MFA sur tous les acces d'administration",
            Description = "Clé physique obligatoire",
            Categorie = "Protection",
        });
        Assert.Equal(HttpStatusCode.Created, ajout.StatusCode);
        var creee = await ajout.Content.ReadFromJsonAsync<JsonElement>();
        var id = creee.GetProperty("id").GetGuid();
        Assert.False(creee.GetProperty("systeme").GetBoolean());
        Assert.Equal("Libre", creee.GetProperty("referentiel").GetString());

        var apresAjout = await (await c.GetAsync("/api/v1/bibliotheque/mesures?q=administration")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(apresAjout.EnumerateArray(), m => m.GetProperty("id").GetGuid() == id);

        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/v1/bibliotheque/mesures/{id}")).StatusCode);

        var apresRetrait = await (await c.GetAsync("/api/v1/bibliotheque/mesures?q=administration")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.DoesNotContain(apresRetrait.EnumerateArray(), m => m.GetProperty("id").GetGuid() == id);
    }

    [Fact]
    public async Task La_bibliotheque_personnelle_est_isolee_par_utilisateur()
    {
        var alice = await NouveauCompteAsync(_factory, "biblio-alice");
        var bob = await NouveauCompteAsync(_factory, "biblio-bob");

        var id = (await (await alice.PostAsJsonAsync("/api/v1/bibliotheque/mesures", new { Titre = "Mesure privée d'Alice" }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var vueBob = await (await bob.GetAsync("/api/v1/bibliotheque/mesures")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.DoesNotContain(vueBob.EnumerateArray(), m => m.GetProperty("id").GetGuid() == id);

        // Bob ne peut pas non plus la supprimer.
        Assert.Equal(HttpStatusCode.NotFound, (await bob.DeleteAsync($"/api/v1/bibliotheque/mesures/{id}")).StatusCode);

        // Mais Bob voit bien le catalogue système, comme tout le monde.
        var isoBob = await (await bob.GetAsync("/api/v1/bibliotheque/mesures?referentiel=Iso27002")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(93, isoBob.GetArrayLength());
    }

    [Fact]
    public async Task Sources_de_risque_catalogue_plus_perso()
    {
        var c = await NouveauCompteAsync(_factory, "biblio-sr");

        var catalogue = await (await c.GetAsync("/api/v1/bibliotheque/sources-risque")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(catalogue.GetArrayLength() >= 9);
        Assert.All(catalogue.EnumerateArray(), s => Assert.True(s.GetProperty("systeme").GetBoolean()));

        var ajout = await c.PostAsJsonAsync("/api/v1/bibliotheque/sources-risque", new
        {
            SourceRisque = "Vengeur",
            DescriptionSourceRisque = "Administrateur licencié",
            ObjectifVise = "EntraveAuFonctionnement",
            DescriptionObjectifVise = "Sabotage des sauvegardes",
            Theme = "Personnes",
            MotivationTypique = 3,
            RessourcesTypiques = 2,
        });
        Assert.Equal(HttpStatusCode.Created, ajout.StatusCode);
        var id = (await ajout.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var apres = await (await c.GetAsync("/api/v1/bibliotheque/sources-risque?q=licencié")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(apres.EnumerateArray(), s => s.GetProperty("id").GetGuid() == id && !s.GetProperty("systeme").GetBoolean());

        // Catégorie invalide rejetée.
        var mauvais = await c.PostAsJsonAsync("/api/v1/bibliotheque/sources-risque", new
        {
            SourceRisque = "PasUneCategorie",
            DescriptionSourceRisque = "x",
            ObjectifVise = "Lucratif",
            DescriptionObjectifVise = "x",
        });
        Assert.Equal(HttpStatusCode.BadRequest, mauvais.StatusCode);
    }
}
