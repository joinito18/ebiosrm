using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using static EbiosRM.Api.Tests.Integration.OutilsEtudeDeTest;

namespace EbiosRM.Api.Tests.Integration;

/// <summary>
/// Mapping de conformité : catalogue d'exigences (ISO 27001 Annexe A + NIS2
/// art. 21) et tableau de couverture d'une étude, croisant le socle de
/// sécurité (A1) et le plan de traitement (A5).
/// </summary>
public class ConformiteTests : IClassFixture<EbiosApiFactory>
{
    private readonly EbiosApiFactory _factory;

    public ConformiteTests(EbiosApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Le_catalogue_d_exigences_contient_iso_et_nis2()
    {
        var c = await NouveauCompteAsync(_factory, "conf-cat");

        var iso = await (await c.GetAsync("/api/v1/referentiels/conformite?referentiel=Iso27001")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(93, iso.GetArrayLength());

        var nis2 = await (await c.GetAsync("/api/v1/referentiels/conformite?referentiel=Nis2")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(10, nis2.GetArrayLength());
        Assert.Contains(nis2.EnumerateArray(), e => e.GetProperty("code").GetString() == "21.2.b");
    }

    [Fact]
    public async Task Le_tableau_de_couverture_croise_le_socle_et_le_plan()
    {
        var c = await NouveauCompteAsync(_factory, "conf-etude");

        // Étude jusqu'à l'atelier 5 (helper), qui ajoute au socle un référentiel
        // « ISO 27001 A.8.24 » à l'état NonConforme et une mesure de traitement.
        var etudeId = await ConstruireEtudeCompleteAsync(c);

        // On tague la mesure de traitement existante avec des exigences.
        var plan = await (await c.GetAsync($"/api/v1/etudes/{etudeId}/plan-traitement-risque")).Content.ReadFromJsonAsync<JsonElement>();
        var mesure = plan.GetProperty("mesures")[0];
        var mesureId = mesure.GetProperty("id").GetGuid();
        var maj = await c.PutAsJsonAsync($"/api/v1/etudes/{etudeId}/plan-traitement-risque/mesures/{mesureId}", new
        {
            Description = mesure.GetProperty("description").GetString(),
            Axe = mesure.GetProperty("axe").GetString(),
            ScenariosDeRisqueIds = mesure.GetProperty("scenariosDeRisqueIds").EnumerateArray().Select(x => x.GetGuid()).ToArray(),
            Responsable = mesure.GetProperty("responsable").GetString(),
            FreinsEtDifficultes = (string?)null,
            CoutComplexite = mesure.GetProperty("coutComplexite").GetString(),
            Echeance = mesure.GetProperty("echeance").GetString(),
            Statut = mesure.GetProperty("statut").GetString(),
            CodesConformite = new[] { "21.2.c", "A.8.13" },
        });
        Assert.Equal(HttpStatusCode.OK, maj.StatusCode);

        // --- ISO ---
        var iso = await (await c.GetAsync($"/api/v1/etudes/{etudeId}/conformite?referentiel=Iso27001")).Content.ReadFromJsonAsync<JsonElement>();
        var lignesIso = iso.GetProperty("lignes").EnumerateArray().ToList();

        var a824 = lignesIso.Single(l => l.GetProperty("code").GetString() == "A.8.24");
        Assert.Equal("Partielle", a824.GetProperty("couverture").GetString()); // socle NonConforme
        Assert.Equal("NonConforme", a824.GetProperty("etatSocle").GetString());

        var a813 = lignesIso.Single(l => l.GetProperty("code").GetString() == "A.8.13");
        Assert.Equal("Partielle", a813.GetProperty("couverture").GetString()); // une mesure la vise
        Assert.NotEmpty(a813.GetProperty("mesures").EnumerateArray());

        // --- NIS2 (dérivé de l'état ISO + mesures) ---
        var nis2 = await (await c.GetAsync($"/api/v1/etudes/{etudeId}/conformite?referentiel=Nis2")).Content.ReadFromJsonAsync<JsonElement>();
        var lignesNis2 = nis2.GetProperty("lignes").EnumerateArray().ToList();

        var c21 = lignesNis2.Single(l => l.GetProperty("code").GetString() == "21.2.c");
        Assert.Contains(c21.GetProperty("couverture").GetString(), new[] { "Partielle", "Conforme" });
        Assert.NotEmpty(c21.GetProperty("mesures").EnumerateArray());

        Assert.True(nis2.GetProperty("synthese").GetProperty("total").GetInt32() == 10);
    }

    [Fact]
    public async Task L_annexe_pdf_de_conformite_se_genere()
    {
        var c = await NouveauCompteAsync(_factory, "conf-pdf");
        var etudeId = (await (await c.PostAsJsonAsync("/api/v1/etudes", new { Nom = "C", Perimetre = "P", Mission = "M" }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var pdf = await c.GetAsync($"/api/v1/etudes/{etudeId}/rapports/conformite");
        Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
        Assert.True((await pdf.Content.ReadAsByteArrayAsync()).Length > 1000);
    }
}
