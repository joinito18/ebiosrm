using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace EbiosRM.Api.Tests.Integration;

/// <summary>
/// Le journal d'audit consigne automatiquement chaque écriture réussie sur une
/// étude (auteur, action, horodatage). Append-only : aucun endpoint ne le
/// modifie ni ne le supprime.
/// </summary>
public class JournalAuditTests : IClassFixture<EbiosApiFactory>
{
    private readonly HttpClient _client;

    public JournalAuditTests(EbiosApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> AuthentifierAsync(string nom)
    {
        var email = $"journal-{Guid.NewGuid():N}@ebiosrm.local";
        var inscription = await _client.PostAsJsonAsync("/api/v1/auth/inscription",
            new { Email = email, MotDePasse = "MotDePasse123", NomAffiche = nom });
        var token = (await inscription.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return nom;
    }

    [Fact]
    public async Task Les_ecritures_sur_une_etude_sont_consignees_avec_auteur_et_action()
    {
        var nom = await AuthentifierAsync("Marie Analyste");

        var etudeId = (await (await _client.PostAsJsonAsync("/api/v1/etudes",
            new { Nom = "Etude journal", Perimetre = "P", Mission = "M" }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        await _client.PostAsync($"/api/v1/etudes/{etudeId}/demarrer-atelier1", null);
        await _client.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/valeurs-metier",
            new { Description = "Paie", EntiteProprietaire = "RH" });

        var journal = await (await _client.GetAsync($"/api/v1/etudes/{etudeId}/journal"))
            .Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(journal.GetArrayLength() >= 2);
        var actions = journal.EnumerateArray().Select(e => e.GetProperty("action").GetString()).ToList();
        Assert.Contains(actions, a => a!.Contains("Démarrage de l'atelier 1"));
        Assert.Contains(actions, a => a!.Contains("valeur métier"));
        Assert.All(journal.EnumerateArray(), e => Assert.Equal(nom, e.GetProperty("nomUtilisateur").GetString()));
        // Ordre antichronologique (le plus récent d'abord).
        var dates = journal.EnumerateArray().Select(e => e.GetProperty("dateUtc").GetDateTime()).ToList();
        Assert.Equal(dates.OrderByDescending(d => d), dates);
    }

    [Fact]
    public async Task Une_lecture_seule_ne_genere_pas_d_entree()
    {
        await AuthentifierAsync("Lecteur");
        var etudeId = (await (await _client.PostAsJsonAsync("/api/v1/etudes",
            new { Nom = "Etude lecture", Perimetre = "P", Mission = "M" }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // Plusieurs GET.
        await _client.GetAsync($"/api/v1/etudes/{etudeId}");
        await _client.GetAsync($"/api/v1/etudes/{etudeId}/valeurs-metier");
        await _client.GetAsync($"/api/v1/etudes/{etudeId}/journal");

        var journal = await (await _client.GetAsync($"/api/v1/etudes/{etudeId}/journal"))
            .Content.ReadFromJsonAsync<JsonElement>();

        // Seule la création de l'étude (POST /etudes, route "id") est consignée.
        Assert.All(journal.EnumerateArray(), e => Assert.NotEqual("GET", e.GetProperty("methode").GetString()));
    }

    [Fact]
    public async Task Le_journal_n_est_pas_modifiable()
    {
        await AuthentifierAsync("Test");
        var etudeId = (await (await _client.PostAsJsonAsync("/api/v1/etudes",
            new { Nom = "Etude immuable", Perimetre = "P", Mission = "M" }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var del = await _client.DeleteAsync($"/api/v1/etudes/{etudeId}/journal");
        var put = await _client.PutAsJsonAsync($"/api/v1/etudes/{etudeId}/journal", new { });

        // 404 (pas de route) ou 405 (route GET seule) : dans tous les cas, aucun
        // moyen de modifier ou supprimer une entree du journal.
        Assert.Contains(del.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });
        Assert.Contains(put.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });
    }
}
