using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using static EbiosRM.Api.Tests.Integration.OutilsEtudeDeTest;

namespace EbiosRM.Api.Tests.Integration;

/// <summary>
/// Cartographie graphique de l'Atelier 3 : le radar de l'écosystème et l'arbre
/// des chemins d'attaque, exposés en SVG (mêmes données que le rapport PDF).
/// </summary>
public class CartographieTests : IClassFixture<EbiosApiFactory>
{
    private readonly EbiosApiFactory _factory;

    public CartographieTests(EbiosApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Les_schemas_svg_reflètent_le_contenu_de_l_etude()
    {
        var c = await NouveauCompteAsync(_factory, "carto");
        var etudeId = await ConstruireEtudeCompleteAsync(c);

        var eco = await c.GetAsync($"/api/v1/etudes/{etudeId}/cartographie/ecosysteme.svg");
        Assert.Equal(HttpStatusCode.OK, eco.StatusCode);
        Assert.Equal("image/svg+xml", eco.Content.Headers.ContentType?.MediaType);
        var svgEco = await eco.Content.ReadAsStringAsync();
        Assert.StartsWith("<svg", svgEco);
        Assert.Contains("Objet de", svgEco);
        Assert.Contains("Infogéreur", svgEco);          // la partie prenante évaluée de l'étude type
        Assert.Contains("seuil de criticité", svgEco);

        var ecoResiduel = await (await c.GetAsync($"/api/v1/etudes/{etudeId}/cartographie/ecosysteme.svg?residuel=true")).Content.ReadAsStringAsync();
        Assert.Contains("résiduelle", ecoResiduel);

        var chemins = await c.GetAsync($"/api/v1/etudes/{etudeId}/cartographie/chemins-attaque.svg");
        Assert.Equal(HttpStatusCode.OK, chemins.StatusCode);
        var svgChemins = await chemins.Content.ReadAsStringAsync();
        Assert.Contains("Scénario 1", svgChemins);
        Assert.Contains("Objet de l", svgChemins);      // apostrophe echappee en &apos; dans le SVG
        Assert.Contains("Infogéreur", svgChemins);      // partie prenante traversée par le chemin (événement intermédiaire)
        Assert.Contains("très pertinent", svgChemins);  // libellé normalisé...
        Assert.DoesNotContain("TresPertinent", svgChemins); // ...pas l'enum brut
    }

    [Fact]
    public async Task Etude_introuvable_renvoie_404()
    {
        var c = await NouveauCompteAsync(_factory, "carto-404");
        Assert.Equal(HttpStatusCode.NotFound,
            (await c.GetAsync($"/api/v1/etudes/{Guid.NewGuid()}/cartographie/ecosysteme.svg")).StatusCode);
    }

    [Fact]
    public async Task Un_non_membre_ne_voit_pas_la_cartographie()
    {
        var proprio = await NouveauCompteAsync(_factory, "carto-p");
        var intrus = await NouveauCompteAsync(_factory, "carto-i");

        var etudeId = (await (await proprio.PostAsJsonAsync("/api/v1/etudes",
            new { Nom = "Privee", Perimetre = "P", Mission = "M" })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.NotFound,
            (await intrus.GetAsync($"/api/v1/etudes/{etudeId}/cartographie/chemins-attaque.svg")).StatusCode);
    }
}
