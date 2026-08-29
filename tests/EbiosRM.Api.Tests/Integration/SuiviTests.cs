using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using static EbiosRM.Api.Tests.Integration.OutilsEtudeDeTest;

namespace EbiosRM.Api.Tests.Integration;

/// <summary>
/// Cadre de suivi vivant : vue portefeuille multi-études, indicateurs (KRI)
/// automatiques et manuels, et comparaison N / N-1 entre deux validations de
/// l'Atelier 5.
/// </summary>
public class SuiviTests : IClassFixture<EbiosApiFactory>
{
    private readonly EbiosApiFactory _factory;

    public SuiviTests(EbiosApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Le_portefeuille_liste_les_etudes_visibles_avec_leurs_metriques()
    {
        var c = await NouveauCompteAsync(_factory, "suivi-portef");
        var etudeId = await ConstruireEtudeCompleteAsync(c);

        var portefeuille = await (await c.GetAsync("/api/v1/portefeuille")).Content.ReadFromJsonAsync<JsonElement>();
        var ligne = portefeuille.EnumerateArray().Single(l => l.GetProperty("etudeId").GetGuid() == etudeId);

        Assert.Equal("Validee", ligne.GetProperty("statutAtelier5").GetString());
        Assert.Equal(1, ligne.GetProperty("scenariosDeRisque").GetInt32());
        Assert.Equal(1, ligne.GetProperty("mesures").GetInt32());
        Assert.True(ligne.GetProperty("risquesResiduels").TryGetProperty("Faible", out _));
        Assert.False(ligne.GetProperty("tauxCouvertureNis2").ValueKind == JsonValueKind.Null);
    }

    [Fact]
    public async Task Indicateurs_automatiques_puis_creation_d_un_indicateur_manuel_et_de_ses_points()
    {
        var c = await NouveauCompteAsync(_factory, "suivi-kri");
        var etudeId = await ConstruireEtudeCompleteAsync(c);

        var vue = await (await c.GetAsync($"/api/v1/etudes/{etudeId}/indicateurs")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(vue.GetProperty("automatiques").GetArrayLength() >= 4);
        Assert.Contains(vue.GetProperty("automatiques").EnumerateArray(),
            i => i.GetProperty("nom").GetString()!.Contains("Avancement"));
        Assert.Empty(vue.GetProperty("manuels").EnumerateArray());

        var creation = await c.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/indicateurs", new
        {
            Nom = "Incidents de sécurité par mois", Unite = "", Cible = 0.0, SeuilAlerte = 3.0, Sens = "Baisse",
        });
        Assert.Equal(HttpStatusCode.Created, creation.StatusCode);
        var indicId = (await creation.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        await c.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/indicateurs/{indicId}/points", new { Date = "2026-05-01", Valeur = 1.0 });
        var apres = await c.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/indicateurs/{indicId}/points", new { Date = "2026-06-01", Valeur = 5.0, Commentaire = "pic" });
        Assert.Equal(HttpStatusCode.OK, apres.StatusCode);
        var indic = await apres.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, indic.GetProperty("points").GetArrayLength());

        // Ré-écrire le point du 2026-06-01 remplace au lieu de dupliquer.
        await c.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/indicateurs/{indicId}/points", new { Date = "2026-06-01", Valeur = 4.0 });
        var indic2 = await (await c.GetAsync($"/api/v1/etudes/{etudeId}/indicateurs")).Content.ReadFromJsonAsync<JsonElement>();
        var mien = indic2.GetProperty("manuels").EnumerateArray().Single();
        Assert.Equal(2, mien.GetProperty("points").GetArrayLength());

        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/v1/etudes/{etudeId}/indicateurs/{indicId}")).StatusCode);
    }

    [Fact]
    public async Task L_evolution_compare_les_deux_dernieres_validations_de_l_atelier_5()
    {
        var c = await NouveauCompteAsync(_factory, "suivi-evo");
        var etudeId = await ConstruireEtudeCompleteAsync(c); // valide l'A5 une 1re fois (v1)

        // Sur la 1re validation il n'y a pas de comparaison.
        var evo1 = await (await c.GetAsync($"/api/v1/etudes/{etudeId}/evolution")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, evo1.GetProperty("precedente").ValueKind);
        Assert.Equal(1, evo1.GetProperty("courante").GetProperty("version").GetInt32());

        // Rouvrir puis revalider avec un libellé -> v2.
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsync($"/api/v1/etudes/{etudeId}/rouvrir-atelier5", null)).StatusCode);
        var reval = await c.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/valider-atelier5", new { Libelle = "Revue annuelle 2026" });
        Assert.Equal(HttpStatusCode.OK, reval.StatusCode);
        Assert.Equal(2, (await reval.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("snapshotVersion").GetInt32());

        var evo2 = await (await c.GetAsync($"/api/v1/etudes/{etudeId}/evolution")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, evo2.GetProperty("courante").GetProperty("version").GetInt32());
        Assert.Equal("Revue annuelle 2026", evo2.GetProperty("courante").GetProperty("libelle").GetString());
        Assert.Equal(1, evo2.GetProperty("precedente").GetProperty("version").GetInt32());

        var scenario = evo2.GetProperty("scenarios").EnumerateArray().Single();
        Assert.Equal("Stable", scenario.GetProperty("tendance").GetString()); // rien n'a changé entre v1 et v2
        Assert.Equal(1, evo2.GetProperty("mesures").GetProperty("total").GetInt32());
    }
}
