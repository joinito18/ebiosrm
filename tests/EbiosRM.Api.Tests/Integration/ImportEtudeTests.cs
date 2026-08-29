using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using static EbiosRM.Api.Tests.Integration.OutilsEtudeDeTest;

namespace EbiosRM.Api.Tests.Integration;

/// <summary>
/// Import d'une étude depuis un fichier JSON produit par <c>.../export</c> :
/// même moteur de re-câblage que la duplication, mais la source est un fichier
/// (entrée non fiable). L'appelant devient propriétaire de l'étude reconstruite.
/// </summary>
public class ImportEtudeTests : IClassFixture<EbiosApiFactory>
{
    private readonly EbiosApiFactory _factory;

    public ImportEtudeTests(EbiosApiFactory factory)
    {
        _factory = factory;
    }

    private static StringContent Json(string contenu) => new(contenu, Encoding.UTF8, "application/json");

    [Fact]
    public async Task Importer_reconstruit_une_etude_exportee_a_l_identique()
    {
        var auteur = await NouveauCompteAsync(_factory, "imp-auteur");
        var sourceId = await ConstruireEtudeCompleteAsync(auteur);
        var fichier = await (await auteur.GetAsync($"/api/v1/etudes/{sourceId}/export")).Content.ReadAsStringAsync();
        var source = JsonSerializer.Deserialize<JsonElement>(fichier);

        // Un autre compte importe le fichier.
        var destinataire = await NouveauCompteAsync(_factory, "imp-destinataire");
        var reponse = await destinataire.PostAsync("/api/v1/etudes/importer", Json(fichier));
        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);
        var copieId = (await reponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var copie = await (await destinataire.GetAsync($"/api/v1/etudes/{copieId}/export")).Content.ReadFromJsonAsync<JsonElement>();

        Assert.EndsWith("(importée)", copie.GetProperty("etude").GetProperty("nom").GetString());
        foreach (var statut in new[] { "statut", "statutAtelier2", "statutAtelier3", "statutAtelier4", "statutAtelier5" })
            Assert.Equal("Brouillon", copie.GetProperty("etude").GetProperty(statut).GetString());

        VerifierGrapheRecable(source, copie);

        // Le destinataire est propriétaire de l'étude importée.
        var mesEtudes = await (await destinataire.GetAsync("/api/v1/etudes")).Content.ReadFromJsonAsync<JsonElement>();
        var vue = mesEtudes.EnumerateArray().Single(e => e.GetProperty("id").GetGuid() == copieId);
        Assert.Equal("Proprietaire", vue.GetProperty("monRole").GetString());

        // L'auteur d'origine ne voit pas l'étude importée par quelqu'un d'autre.
        var etudesAuteur = await (await auteur.GetAsync("/api/v1/etudes")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.DoesNotContain(etudesAuteur.EnumerateArray(), e => e.GetProperty("id").GetGuid() == copieId);
    }

    [Theory]
    [InlineData("{ ceci n est pas du json")]
    [InlineData("{\"formatVersion\": 1}")]                       // pas d'étude
    [InlineData("{\"formatVersion\": 99, \"etude\": {\"nom\":\"x\",\"perimetre\":\"x\",\"mission\":\"x\"}}")]
    public async Task Importer_un_fichier_invalide_renvoie_400_sans_rien_creer(string corps)
    {
        var client = await NouveauCompteAsync(_factory, "imp-ko");
        var avant = (await (await client.GetAsync("/api/v1/etudes")).Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength();

        var reponse = await client.PostAsync("/api/v1/etudes/importer", Json(corps));

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(
            (await reponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString()));

        var apres = (await (await client.GetAsync("/api/v1/etudes")).Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength();
        Assert.Equal(avant, apres);
    }

    [Fact]
    public async Task Importer_un_fichier_avec_une_reference_cassee_est_rejete()
    {
        var auteur = await NouveauCompteAsync(_factory, "imp-ref");
        var sourceId = await ConstruireEtudeCompleteAsync(auteur);
        var fichier = await (await auteur.GetAsync($"/api/v1/etudes/{sourceId}/export")).Content.ReadAsStringAsync();

        // On casse le lien bien support -> valeur métier.
        var noeud = JsonNode.Parse(fichier)!;
        noeud["biensSupport"]![0]!["valeurMetierId"] = Guid.NewGuid().ToString();

        var reponse = await auteur.PostAsync("/api/v1/etudes/importer", Json(noeud.ToJsonString()));

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
        var erreur = (await reponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString();
        Assert.Contains("incohérent", erreur);
    }
}
