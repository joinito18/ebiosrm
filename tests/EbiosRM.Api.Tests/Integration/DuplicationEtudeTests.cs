using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using static EbiosRM.Api.Tests.Integration.OutilsEtudeDeTest;

namespace EbiosRM.Api.Tests.Integration;

/// <summary>
/// Duplication d'une étude ("base de modèles") : recopie tout le contenu
/// éditable des 5 ateliers dans une nouvelle étude, avec ré-attribution de
/// toutes les clés (aucun Id partagé avec la source) et re-câblage des
/// références entre agrégats (bien -> valeur métier, scénario -> couple,
/// chemin -> scénario stratégique + partie prenante via l'événement
/// intermédiaire, action élémentaire -> bien support, mesure de traitement
/// -> scénario de risque).
/// </summary>
public class DuplicationEtudeTests : IClassFixture<EbiosApiFactory>
{
    private readonly EbiosApiFactory _factory;

    public DuplicationEtudeTests(EbiosApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Dupliquer_recopie_tout_le_contenu_avec_des_cles_neuves()
    {
        var client = await NouveauCompteAsync(_factory, "dup-proprio");
        var sourceId = await ConstruireEtudeCompleteAsync(client);

        var source = await (await client.GetAsync($"/api/v1/etudes/{sourceId}/export")).Content.ReadFromJsonAsync<JsonElement>();

        var reponse = await client.PostAsJsonAsync($"/api/v1/etudes/{sourceId}/dupliquer", new { Nom = "Ma copie" });
        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);
        var copieId = (await reponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.NotEqual(sourceId, copieId);

        var copie = await (await client.GetAsync($"/api/v1/etudes/{copieId}/export")).Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("Ma copie", copie.GetProperty("etude").GetProperty("nom").GetString());
        foreach (var statut in new[] { "statut", "statutAtelier2", "statutAtelier3", "statutAtelier4", "statutAtelier5" })
            Assert.Equal("Brouillon", copie.GetProperty("etude").GetProperty(statut).GetString());

        VerifierGrapheRecable(source, copie);

        // La source est intacte.
        var sourceApres = await (await client.GetAsync($"/api/v1/etudes/{sourceId}/export")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Validee", sourceApres.GetProperty("etude").GetProperty("statut").GetString());
        Assert.Equal(source.GetProperty("valeursMetier").GetArrayLength(), sourceApres.GetProperty("valeursMetier").GetArrayLength());
    }

    [Fact]
    public async Task Un_lecteur_peut_dupliquer_une_etude_partagee()
    {
        var proprio = await NouveauCompteAsync(_factory, "dup-p");
        var lecteur = await NouveauCompteAsync(_factory, "dup-l");
        var lecteurEmail = (await (await lecteur.GetAsync("/api/v1/auth/moi")).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("email").GetString();

        var sourceId = (await (await proprio.PostAsJsonAsync("/api/v1/etudes",
            new { Nom = "Partagee", Perimetre = "P", Mission = "M" })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await proprio.PostAsync($"/api/v1/etudes/{sourceId}/demarrer-atelier1", null);
        await proprio.PostAsJsonAsync($"/api/v1/etudes/{sourceId}/valeurs-metier", new { Description = "V", EntiteProprietaire = "E" });
        await proprio.PostAsJsonAsync($"/api/v1/etudes/{sourceId}/membres", new { Email = lecteurEmail, Role = "Lecteur" });

        var dup = await lecteur.PostAsJsonAsync($"/api/v1/etudes/{sourceId}/dupliquer", new { Nom = "Copie du lecteur" });
        Assert.Equal(HttpStatusCode.Created, dup.StatusCode);
        var copieId = (await dup.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var mesEtudes = await (await lecteur.GetAsync("/api/v1/etudes")).Content.ReadFromJsonAsync<JsonElement>();
        var copie = mesEtudes.EnumerateArray().Single(e => e.GetProperty("id").GetGuid() == copieId);
        Assert.Equal("Proprietaire", copie.GetProperty("monRole").GetString());
    }
}
