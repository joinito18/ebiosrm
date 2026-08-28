using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace EbiosRM.Api.Tests.Integration;

/// <summary>
/// Partage d'une etude entre comptes avec 3 roles :
/// Proprietaire (tout + gestion des membres + suppression),
/// Editeur (contenu des ateliers + valider/rouvrir),
/// Lecteur (consultation + rapports).
/// </summary>
public class PartageEtudeTests : IClassFixture<EbiosApiFactory>
{
    private readonly EbiosApiFactory _factory;

    public PartageEtudeTests(EbiosApiFactory factory) => _factory = factory;

    private async Task<(HttpClient client, Guid id, string email)> NouveauCompteAsync(string prefixe)
    {
        var client = _factory.CreateClient();
        var email = $"{prefixe}-{Guid.NewGuid():N}@ebiosrm.local";
        var resp = await client.PostAsJsonAsync("/api/v1/auth/inscription",
            new { Email = email, MotDePasse = "MotDePasse123", NomAffiche = prefixe });
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("token").GetString());
        return (client, body.GetProperty("utilisateur").GetProperty("id").GetGuid(), email);
    }

    private static async Task<Guid> CreerEtudeAsync(HttpClient client)
    {
        var e = await (await client.PostAsJsonAsync("/api/v1/etudes",
            new { Nom = "Etude partagee", Perimetre = "P", Mission = "M" })).Content.ReadFromJsonAsync<JsonElement>();
        return e.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Le_createur_est_proprietaire()
    {
        var (proprio, _, _) = await NouveauCompteAsync("proprio");
        var etudeId = await CreerEtudeAsync(proprio);

        var etude = await (await proprio.GetAsync($"/api/v1/etudes/{etudeId}")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Proprietaire", etude.GetProperty("monRole").GetString());
    }

    [Fact]
    public async Task Non_membre_ne_voit_pas_l_etude()
    {
        var (proprio, _, _) = await NouveauCompteAsync("proprio");
        var etudeId = await CreerEtudeAsync(proprio);
        var (etranger, _, _) = await NouveauCompteAsync("etranger");

        Assert.Equal(HttpStatusCode.NotFound, (await etranger.GetAsync($"/api/v1/etudes/{etudeId}")).StatusCode);
        var liste = await (await etranger.GetAsync("/api/v1/etudes")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.DoesNotContain(liste.EnumerateArray(), x => x.GetProperty("id").GetGuid() == etudeId);
    }

    [Fact]
    public async Task Editeur_modifie_le_contenu_mais_pas_les_membres()
    {
        var (proprio, _, _) = await NouveauCompteAsync("proprio");
        var etudeId = await CreerEtudeAsync(proprio);
        var (editeur, _, editeurEmail) = await NouveauCompteAsync("editeur");

        Assert.Equal(HttpStatusCode.Created, (await proprio.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/membres",
            new { Email = editeurEmail, Role = "Editeur" })).StatusCode);

        // L'etude apparait maintenant chez l'editeur, avec son role.
        var etude = await (await editeur.GetAsync($"/api/v1/etudes/{etudeId}")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Editeur", etude.GetProperty("monRole").GetString());

        // Il peut demarrer un atelier et ajouter du contenu.
        Assert.Equal(HttpStatusCode.OK, (await editeur.PostAsync($"/api/v1/etudes/{etudeId}/demarrer-atelier1", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await editeur.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/valeurs-metier",
            new { Description = "VM par editeur", EntiteProprietaire = "DSI" })).StatusCode);

        // Mais pas gerer les membres.
        var (autre, _, autreEmail) = await NouveauCompteAsync("autre");
        Assert.Equal(HttpStatusCode.Forbidden, (await editeur.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/membres",
            new { Email = autreEmail, Role = "Lecteur" })).StatusCode);
        // Ni supprimer l'etude.
        Assert.Equal(HttpStatusCode.Forbidden, (await editeur.DeleteAsync($"/api/v1/etudes/{etudeId}")).StatusCode);
    }

    [Fact]
    public async Task Lecteur_consulte_mais_ne_modifie_pas()
    {
        var (proprio, _, _) = await NouveauCompteAsync("proprio");
        var etudeId = await CreerEtudeAsync(proprio);
        await proprio.PostAsync($"/api/v1/etudes/{etudeId}/demarrer-atelier1", null);
        var (lecteur, _, lecteurEmail) = await NouveauCompteAsync("lecteur");

        await proprio.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/membres", new { Email = lecteurEmail, Role = "Lecteur" });

        Assert.Equal(HttpStatusCode.OK, (await lecteur.GetAsync($"/api/v1/etudes/{etudeId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await lecteur.GetAsync($"/api/v1/etudes/{etudeId}/valeurs-metier")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await lecteur.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/valeurs-metier",
            new { Description = "interdit", EntiteProprietaire = "X" })).StatusCode);
    }

    [Fact]
    public async Task Ajouter_un_membre_email_inconnu_renvoie_404()
    {
        var (proprio, _, _) = await NouveauCompteAsync("proprio");
        var etudeId = await CreerEtudeAsync(proprio);

        Assert.Equal(HttpStatusCode.NotFound, (await proprio.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/membres",
            new { Email = "personne@nulle-part.local", Role = "Editeur" })).StatusCode);
    }

    [Fact]
    public async Task On_ne_peut_pas_retirer_le_dernier_proprietaire()
    {
        var (proprio, proprioId, _) = await NouveauCompteAsync("proprio");
        var etudeId = await CreerEtudeAsync(proprio);

        Assert.Equal(HttpStatusCode.Conflict, (await proprio.DeleteAsync($"/api/v1/etudes/{etudeId}/membres/{proprioId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await proprio.PutAsJsonAsync($"/api/v1/etudes/{etudeId}/membres/{proprioId}",
            new { Role = "Lecteur" })).StatusCode);
    }
}
