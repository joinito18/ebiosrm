using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace EbiosRM.Api.Tests.Integration;

public class AuthTests : IClassFixture<EbiosApiFactory>
{
    private readonly EbiosApiFactory _factory;

    public AuthTests(EbiosApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Endpoint_protege_refuse_l_acces_sans_jeton()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/etudes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Inscription_puis_connexion_permettent_d_acceder_aux_endpoints_proteges()
    {
        var client = _factory.CreateClient();
        var email = $"auth-test-{Guid.NewGuid():N}@ebiosrm.local";

        var inscription = await client.PostAsJsonAsync("/api/v1/auth/inscription", new { Email = email, MotDePasse = "MotDePasse123", NomAffiche = "Test Auth" });
        Assert.Equal(HttpStatusCode.Created, inscription.StatusCode);
        var tokenInscription = (await inscription.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenInscription);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/etudes")).StatusCode);

        var moi = await client.GetAsync("/api/v1/auth/moi");
        Assert.Equal(HttpStatusCode.OK, moi.StatusCode);
        Assert.Equal(email, (await moi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("email").GetString());

        // Connexion séparée, sur un nouveau client sans jeton préalable.
        var clientConnexion = _factory.CreateClient();
        var connexion = await clientConnexion.PostAsJsonAsync("/api/v1/auth/connexion", new { Email = email, MotDePasse = "MotDePasse123" });
        Assert.Equal(HttpStatusCode.OK, connexion.StatusCode);
        var tokenConnexion = (await connexion.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        clientConnexion.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenConnexion);
        Assert.Equal(HttpStatusCode.OK, (await clientConnexion.GetAsync("/api/v1/etudes")).StatusCode);
    }

    [Fact]
    public async Task Connexion_avec_mauvais_mot_de_passe_renvoie_401()
    {
        var client = _factory.CreateClient();
        var email = $"auth-test-{Guid.NewGuid():N}@ebiosrm.local";
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/v1/auth/inscription", new { Email = email, MotDePasse = "MotDePasse123", NomAffiche = "Test" })).StatusCode);

        var connexion = await client.PostAsJsonAsync("/api/v1/auth/connexion", new { Email = email, MotDePasse = "MauvaisMotDePasse" });

        Assert.Equal(HttpStatusCode.Unauthorized, connexion.StatusCode);
    }

    [Fact]
    public async Task Inscription_avec_email_deja_utilise_renvoie_400()
    {
        var client = _factory.CreateClient();
        var email = $"auth-test-{Guid.NewGuid():N}@ebiosrm.local";
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/v1/auth/inscription", new { Email = email, MotDePasse = "MotDePasse123", NomAffiche = "Test" })).StatusCode);

        var deuxieme = await client.PostAsJsonAsync("/api/v1/auth/inscription", new { Email = email, MotDePasse = "AutreMotDePasse123", NomAffiche = "Autre" });

        Assert.Equal(HttpStatusCode.BadRequest, deuxieme.StatusCode);
    }
}
