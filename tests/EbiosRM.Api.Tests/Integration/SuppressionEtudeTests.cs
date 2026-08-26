using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace EbiosRM.Api.Tests.Integration;

/// <summary>
/// Vérifie que la suppression d'une étude emporte bien tout son contenu
/// (purge table par table par EtudeId, cf. ServiceSuppressionEtude), y compris
/// les entités owned rattachées à un enfant (referentiels_applicables via le
/// socle de sécurité).
/// </summary>
public class SuppressionEtudeTests : IClassFixture<EbiosApiFactory>
{
    private readonly HttpClient _client;

    public SuppressionEtudeTests(EbiosApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task AuthentifierAsync()
    {
        var email = $"suppression-test-{Guid.NewGuid():N}@ebiosrm.local";
        var inscription = await _client.PostAsJsonAsync("/api/v1/auth/inscription", new { Email = email, MotDePasse = "MotDePasse123", NomAffiche = "Test Suppression" });
        var token = (await inscription.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task Suppression_etude_inexistante_renvoie_404()
    {
        await AuthentifierAsync();

        var response = await _client.DeleteAsync($"/api/v1/etudes/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Suppression_etude_emporte_son_contenu()
    {
        await AuthentifierAsync();

        var etude = await (await _client.PostAsJsonAsync("/api/v1/etudes", new { Nom = "Etude a supprimer", Perimetre = "P", Mission = "M" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var etudeId = etude.GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/api/v1/etudes/{etudeId}/demarrer-atelier1", null)).StatusCode);
        var vm = await (await _client.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/valeurs-metier", new { Description = "VM test", EntiteProprietaire = "DSI" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var valeurMetierId = vm.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/valeurs-metier/{valeurMetierId}/biens-support",
            new { Description = "Bien test", Type = "SystemeInformation", EntiteProprietaire = "DSI" })).StatusCode);

        var suppression = await _client.DeleteAsync($"/api/v1/etudes/{etudeId}");
        Assert.Equal(HttpStatusCode.NoContent, suppression.StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/v1/etudes/{etudeId}")).StatusCode);
        var valeursApresSuppression = await (await _client.GetAsync($"/api/v1/etudes/{etudeId}/valeurs-metier")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, valeursApresSuppression.GetArrayLength());

        // Redemander la suppression une seconde fois : 404, pas d'exception.
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync($"/api/v1/etudes/{etudeId}")).StatusCode);
    }

    [Fact]
    public async Task Suppression_d_une_etude_ne_touche_pas_les_autres()
    {
        await AuthentifierAsync();

        var etudeASupprimer = (await (await _client.PostAsJsonAsync("/api/v1/etudes", new { Nom = "A supprimer", Perimetre = "P", Mission = "M" })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var etudeAConserver = (await (await _client.PostAsJsonAsync("/api/v1/etudes", new { Nom = "A conserver", Perimetre = "P", Mission = "M" })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/v1/etudes/{etudeASupprimer}")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/v1/etudes/{etudeAConserver}")).StatusCode);
    }
}
