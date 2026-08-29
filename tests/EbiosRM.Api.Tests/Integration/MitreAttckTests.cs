using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using static EbiosRM.Api.Tests.Integration.OutilsEtudeDeTest;

namespace EbiosRM.Api.Tests.Integration;

/// <summary>
/// Intégration MITRE ATT&CK dans l'Atelier 4 : catalogue de techniques
/// embarqué (filtrable par phase EBIOS RM) et association d'une technique à une
/// action élémentaire.
/// </summary>
public class MitreAttckTests : IClassFixture<EbiosApiFactory>
{
    private readonly EbiosApiFactory _factory;

    public MitreAttckTests(EbiosApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Le_catalogue_est_filtrable_par_phase_ebios()
    {
        var c = await NouveauCompteAsync(_factory, "mitre-cat");

        var tout = await (await c.GetAsync("/api/v1/referentiels/mitre")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(tout.GetArrayLength() > 100);

        var rentrer = await (await c.GetAsync("/api/v1/referentiels/mitre?phase=Rentrer")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.All(rentrer.EnumerateArray(), t => Assert.Equal("Rentrer", t.GetProperty("phaseEbios").GetString()));
        Assert.Contains(rentrer.EnumerateArray(), t => t.GetProperty("id").GetString() == "T1566"); // Phishing -> Initial Access -> Rentrer

        var phishing = await (await c.GetAsync("/api/v1/referentiels/mitre?phase=Rentrer&q=phishing")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, phishing.GetArrayLength());
        Assert.Equal("Phishing", phishing[0].GetProperty("nom").GetString());
        Assert.Equal("Initial Access", phishing[0].GetProperty("tactique").GetString());
    }

    [Fact]
    public async Task Une_technique_associee_a_une_action_est_persistee_et_visible_dans_le_rapport()
    {
        var c = await NouveauCompteAsync(_factory, "mitre-action");

        // Étude minimale jusqu'à l'atelier 4.
        var etudeId = (await (await c.PostAsJsonAsync("/api/v1/etudes", new { Nom = "M", Perimetre = "P", Mission = "M" }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await c.PostAsync($"/api/v1/etudes/{etudeId}/demarrer-atelier1", null);
        var vmId = (await (await c.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/valeurs-metier", new { Description = "V", EntiteProprietaire = "E" }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var bienId = (await (await c.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/valeurs-metier/{vmId}/biens-support",
            new { Description = "Serveur", Type = "SystemeInformation", EntiteProprietaire = "DSI" })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var erId = (await (await c.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/valeurs-metier/{vmId}/evenements-redoutes",
            new { Description = "Fuite", Gravite = 3 })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await c.PostAsync($"/api/v1/etudes/{etudeId}/valider-atelier1", null);

        await c.PostAsync($"/api/v1/etudes/{etudeId}/demarrer-atelier2", null);
        var coupleId = (await (await c.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/couples-sr-ov", new
        {
            SourceRisque = "CrimeOrganise", DescriptionSourceRisque = "x", ObjectifVise = "Lucratif", DescriptionObjectifVise = "x",
            ContexteVulnerabilite = "x", Theme = "Technologique", Motivation = 3, Ressources = 3,
        })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await c.PostAsync($"/api/v1/etudes/{etudeId}/valider-atelier2", null);

        await c.PostAsync($"/api/v1/etudes/{etudeId}/demarrer-atelier3", null);
        var scenarioId = (await (await c.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/couples-sr-ov/{coupleId}/scenario-strategique",
            new { EvenementRedouteId = erId, Description = "x" })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var cheminId = (await (await c.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/scenarios-strategiques/{scenarioId}/chemins-attaque",
            new { Description = "x" })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await c.PostAsync($"/api/v1/etudes/{etudeId}/valider-atelier3", null);

        await c.PostAsync($"/api/v1/etudes/{etudeId}/demarrer-atelier4", null);
        var scenarioOpId = (await (await c.PostAsync($"/api/v1/etudes/{etudeId}/chemins-attaque/{cheminId}/scenario-operationnel", null))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var ajoutMode = await c.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/scenarios-operationnels/{scenarioOpId}/modes-operatoires", new
        {
            Description = "Intrusion par compte valide",
            Actions = new[]
            {
                new { Description = "Reutilisation d'identifiants", Phase = "Rentrer", BienSupportId = bienId, TechniqueMitre = (string?)"T1078" },
                new { Description = "Exfiltration", Phase = "Exploiter", BienSupportId = bienId, TechniqueMitre = (string?)null },
            },
            ProbabiliteSucces = 3,
            DifficulteTechnique = 2,
        });
        Assert.Equal(HttpStatusCode.Created, ajoutMode.StatusCode);

        // La technique revient bien sur l'action.
        var scenariosOp = await (await c.GetAsync($"/api/v1/etudes/{etudeId}/scenarios-operationnels")).Content.ReadFromJsonAsync<JsonElement>();
        var action = scenariosOp[0].GetProperty("modesOperatoires")[0].GetProperty("actionsElementaires")
            .EnumerateArray().Single(a => a.GetProperty("phase").GetString() == "Rentrer");
        Assert.Equal("T1078", action.GetProperty("techniqueMitre").GetString());

        // Le rapport PDF de l'atelier 4 se génère (la technique y est reprise en texte).
        await c.PostAsync($"/api/v1/etudes/{etudeId}/valider-atelier4", null);
        var rapport = await c.GetAsync($"/api/v1/etudes/{etudeId}/rapports/atelier4");
        Assert.Equal(HttpStatusCode.OK, rapport.StatusCode);
        Assert.Equal("application/pdf", rapport.Content.Headers.ContentType?.MediaType);
    }
}
