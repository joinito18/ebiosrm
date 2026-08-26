using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace EbiosRM.Api.Tests.Integration;

/// <summary>
/// Le cadre de suivi est le seul rapport qui lit l'état courant plutôt qu'un
/// snapshot figé (cf. RapportCadreDeSuiviData) : ces tests vérifient
/// spécifiquement qu'il est disponible AVANT la validation complète de
/// l'Atelier 5 (contrairement à la synthèse globale) et qu'il reflète un
/// changement de statut de mesure sans attendre une nouvelle validation.
/// </summary>
public class CadreDeSuiviTests : IClassFixture<EbiosApiFactory>
{
    private readonly HttpClient _client;

    public CadreDeSuiviTests(EbiosApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task AuthentifierAsync()
    {
        var email = $"cadre-suivi-{Guid.NewGuid():N}@ebiosrm.local";
        var inscription = await _client.PostAsJsonAsync("/api/v1/auth/inscription", new { Email = email, MotDePasse = "MotDePasse123", NomAffiche = "Test" });
        var token = (await inscription.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task Cadre_de_suivi_refuse_avant_demarrage_atelier5()
    {
        await AuthentifierAsync();
        var etudeId = (await (await _client.PostAsJsonAsync("/api/v1/etudes", new { Nom = "Etude test", Perimetre = "P", Mission = "M" })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var response = await _client.GetAsync($"/api/v1/etudes/{etudeId}/rapports/cadre-de-suivi");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cadre_de_suivi_disponible_avant_validation_complete_et_reflete_les_changements_en_direct()
    {
        await AuthentifierAsync();

        // --- Construire une étude jusqu'à un plan de traitement avec une mesure, sans valider l'Atelier 5 ---
        var etudeId = (await (await _client.PostAsJsonAsync("/api/v1/etudes", new { Nom = "Etude suivi", Perimetre = "P", Mission = "M" })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        await _client.PostAsync($"/api/v1/etudes/{etudeId}/demarrer-atelier1", null);
        var vmId = (await (await _client.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/valeurs-metier", new { Description = "VM", EntiteProprietaire = "DSI" })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var bienSupportId = (await (await _client.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/valeurs-metier/{vmId}/biens-support", new { Description = "Bien", Type = "SystemeInformation", EntiteProprietaire = "DSI" })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await _client.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/valeurs-metier/{vmId}/evenements-redoutes", new { Description = "ER", Gravite = 3 });
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/api/v1/etudes/{etudeId}/valider-atelier1", null)).StatusCode);

        await _client.PostAsync($"/api/v1/etudes/{etudeId}/demarrer-atelier2", null);
        var coupleId = (await (await _client.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/couples-sr-ov", new
        {
            SourceRisque = "Etatique", DescriptionSourceRisque = "SR", ObjectifVise = "EspionnageEtatiqueOuIndustriel", DescriptionObjectifVise = "OV",
            ContexteVulnerabilite = "Contexte", Theme = "Technologique", Motivation = 4, Ressources = 4
        })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/api/v1/etudes/{etudeId}/valider-atelier2", null)).StatusCode);

        await _client.PostAsync($"/api/v1/etudes/{etudeId}/demarrer-atelier3", null);
        var erId = (await (await _client.GetAsync($"/api/v1/etudes/{etudeId}/evenements-redoutes")).Content.ReadFromJsonAsync<JsonElement>())[0].GetProperty("id").GetGuid();
        var scenarioId = (await (await _client.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/couples-sr-ov/{coupleId}/scenario-strategique", new { EvenementRedouteId = erId, Description = "Scenario" })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var cheminId = (await (await _client.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/scenarios-strategiques/{scenarioId}/chemins-attaque", new { Description = "Chemin" })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/api/v1/etudes/{etudeId}/valider-atelier3", null)).StatusCode);

        await _client.PostAsync($"/api/v1/etudes/{etudeId}/demarrer-atelier4", null);
        var scenarioOpId = (await (await _client.PostAsync($"/api/v1/etudes/{etudeId}/chemins-attaque/{cheminId}/scenario-operationnel", null)).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/scenarios-operationnels/{scenarioOpId}/modes-operatoires", new
        {
            Description = "Mode operatoire",
            Actions = new[] { new { Description = "Action", Phase = "Exploiter", BienSupportId = bienSupportId } },
            ProbabiliteSucces = 2,
            DifficulteTechnique = 2
        })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/api/v1/etudes/{etudeId}/valider-atelier4", null)).StatusCode);

        // Atelier 5 DEMARRE mais PAS validé -- le cadre de suivi doit deja etre disponible ici.
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/api/v1/etudes/{etudeId}/demarrer-atelier5", null)).StatusCode);
        var scenarioDeRisqueId = (await (await _client.PostAsync($"/api/v1/etudes/{etudeId}/chemins-attaque/{cheminId}/scenario-de-risque", null)).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsync($"/api/v1/etudes/{etudeId}/plan-traitement-risque", null)).StatusCode);
        var mesure = await (await _client.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/plan-traitement-risque/mesures", new
        {
            Description = "Mesure test", Axe = "Protection", ScenariosDeRisqueIds = new[] { scenarioDeRisqueId },
            Responsable = "RSSI", CoutComplexite = "Plus", Statut = "ALancer"
        })).Content.ReadFromJsonAsync<JsonElement>();
        var mesureId = mesure.GetProperty("mesures")[0].GetProperty("id").GetGuid();

        // Synthese refusee (Atelier 5 pas encore valide) alors que le cadre de suivi, lui, doit fonctionner.
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync($"/api/v1/etudes/{etudeId}/rapports/synthese")).StatusCode);

        var rapportAvant = await _client.GetAsync($"/api/v1/etudes/{etudeId}/rapports/cadre-de-suivi");
        Assert.Equal(HttpStatusCode.OK, rapportAvant.StatusCode);
        Assert.Equal("application/pdf", rapportAvant.Content.Headers.ContentType?.MediaType);
        var tailleAvant = (await rapportAvant.Content.ReadAsByteArrayAsync()).Length;

        // La mesure passe a "Termine" -- le cadre de suivi doit refleter ce changement
        // immediatement, sans passer par une nouvelle validation d'atelier.
        Assert.Equal(HttpStatusCode.OK, (await _client.PutAsJsonAsync($"/api/v1/etudes/{etudeId}/plan-traitement-risque/mesures/{mesureId}", new
        {
            Description = "Mesure test", Axe = "Protection", ScenariosDeRisqueIds = new[] { scenarioDeRisqueId },
            Responsable = "RSSI", CoutComplexite = "Plus", Statut = "Termine"
        })).StatusCode);

        var rapportApres = await _client.GetAsync($"/api/v1/etudes/{etudeId}/rapports/cadre-de-suivi");
        Assert.Equal(HttpStatusCode.OK, rapportApres.StatusCode);
        var tailleApres = (await rapportApres.Content.ReadAsByteArrayAsync()).Length;

        // Les deux PDF different (le statut/l'anneau de progression a change) --
        // preuve indirecte mais suffisante que la donnee lue est bien live.
        Assert.NotEqual(tailleAvant, tailleApres);
    }
}
