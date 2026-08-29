using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using static EbiosRM.Api.Tests.Integration.OutilsEtudeDeTest;

namespace EbiosRM.Api.Tests.Integration;

/// <summary>
/// Bibliothèque communautaire : publier une entrée personnelle, la voir depuis
/// un autre compte, l'importer, la signaler (masquage automatique au-delà de
/// 3 signalements distincts).
/// </summary>
public class BibliothequeCommunautaireTests : IClassFixture<EbiosApiFactory>
{
    private readonly EbiosApiFactory _factory;

    public BibliothequeCommunautaireTests(EbiosApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<Guid> AjouterMesurePerso(HttpClient c, string titre)
    {
        var r = await c.PostAsJsonAsync("/api/v1/bibliotheque/mesures", new { Titre = titre, Categorie = "Test" });
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Publier_puis_voir_et_importer_depuis_un_autre_compte()
    {
        var alice = await NouveauCompteAsync(_factory, "comm-alice");
        var bob = await NouveauCompteAsync(_factory, "comm-bob");

        var id = await AjouterMesurePerso(alice, "Cloisonnement reseau OT/IT");

        // Publier (idempotent).
        Assert.Equal(HttpStatusCode.OK, (await alice.PostAsync($"/api/v1/bibliotheque/communaute/mesure/{id}/publier", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await alice.PostAsync($"/api/v1/bibliotheque/communaute/mesure/{id}/publier", null)).StatusCode);

        // Bob ne peut pas publier une entrée qui n'est pas la sienne.
        Assert.Equal(HttpStatusCode.BadRequest, (await bob.PostAsync($"/api/v1/bibliotheque/communaute/mesure/{id}/publier", null)).StatusCode);

        // Bob voit l'entrée dans la communauté, attribuée à Alice.
        var liste = await (await bob.GetAsync("/api/v1/bibliotheque/communaute/mesure")).Content.ReadFromJsonAsync<JsonElement>();
        var vue = liste.EnumerateArray().First(e => e.GetProperty("id").GetGuid() == id);
        Assert.False(vue.GetProperty("publieParMoi").GetBoolean());
        Assert.Equal("comm-alice", vue.GetProperty("proprietaire").GetString());
        Assert.Contains("Cloisonnement", vue.GetProperty("entree").GetProperty("titre").GetString());

        // Bob importe : copie privée dans sa bibliothèque, Id différent.
        var imp = await bob.PostAsync($"/api/v1/bibliotheque/communaute/mesure/{id}/importer", null);
        Assert.Equal(HttpStatusCode.OK, imp.StatusCode);
        var nouvelId = (await imp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.NotEqual(id, nouvelId);

        var maBiblio = await (await bob.GetAsync("/api/v1/bibliotheque/mesures?q=Cloisonnement")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(maBiblio.EnumerateArray(), m => m.GetProperty("id").GetGuid() == nouvelId && !m.GetProperty("systeme").GetBoolean());

        // Alice retire du partage -> disparaît de la communauté.
        Assert.Equal(HttpStatusCode.NoContent, (await alice.DeleteAsync($"/api/v1/bibliotheque/communaute/mesure/{id}/publier")).StatusCode);
        var apres = await (await bob.GetAsync("/api/v1/bibliotheque/communaute/mesure")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.DoesNotContain(apres.EnumerateArray(), e => e.GetProperty("id").GetGuid() == id);
    }

    [Fact]
    public async Task Trois_signalements_distincts_masquent_l_entree()
    {
        var auteur = await NouveauCompteAsync(_factory, "sig-auteur");
        var s1 = await NouveauCompteAsync(_factory, "sig-1");
        var s2 = await NouveauCompteAsync(_factory, "sig-2");
        var s3 = await NouveauCompteAsync(_factory, "sig-3");

        var id = await AjouterMesurePerso(auteur, "Entree limite a signaler");
        await auteur.PostAsync($"/api/v1/bibliotheque/communaute/mesure/{id}/publier", null);

        async Task<bool> VisiblePour(HttpClient c)
        {
            var l = await (await c.GetAsync("/api/v1/bibliotheque/communaute/mesure")).Content.ReadFromJsonAsync<JsonElement>();
            return l.EnumerateArray().Any(e => e.GetProperty("id").GetGuid() == id);
        }

        Assert.True(await VisiblePour(s1));

        // Deux signalements : encore visible. Le même compte qui re-signale ne compte pas.
        Assert.Equal(HttpStatusCode.OK, (await s1.PostAsJsonAsync($"/api/v1/bibliotheque/communaute/mesure/{id}/signaler", new { Motif = "hors sujet" })).StatusCode);
        await s1.PostAsJsonAsync($"/api/v1/bibliotheque/communaute/mesure/{id}/signaler", new { Motif = "encore" });
        await s2.PostAsJsonAsync($"/api/v1/bibliotheque/communaute/mesure/{id}/signaler", new { Motif = "spam" });
        Assert.True(await VisiblePour(s3));

        // Troisième signaleur distinct -> masquée pour tout le monde.
        await s3.PostAsJsonAsync($"/api/v1/bibliotheque/communaute/mesure/{id}/signaler", new { Motif = "contenu problematique" });
        Assert.False(await VisiblePour(s1));

        // L'auteur ne peut plus la republier tant qu'elle est masquée.
        Assert.Equal(HttpStatusCode.BadRequest, (await auteur.PostAsync($"/api/v1/bibliotheque/communaute/mesure/{id}/publier", null)).StatusCode);

        // Mais elle reste dans la bibliothèque personnelle de l'auteur.
        var perso = await (await auteur.GetAsync("/api/v1/bibliotheque/mesures?q=Entree limite")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(perso.EnumerateArray(), m => m.GetProperty("id").GetGuid() == id);
    }
}
