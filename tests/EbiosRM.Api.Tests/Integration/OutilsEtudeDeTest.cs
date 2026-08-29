using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace EbiosRM.Api.Tests.Integration;

/// <summary>
/// Helpers partagés entre les tests qui ont besoin d'une étude réelle et
/// complète (duplication, import) : création d'un compte + parcours des 5
/// ateliers via l'API HTTP, et inspection des Id d'un export.
/// </summary>
internal static class OutilsEtudeDeTest
{
    public static async Task<HttpClient> NouveauCompteAsync(EbiosApiFactory factory, string prefixe)
    {
        var client = factory.CreateClient();
        var inscription = await client.PostAsJsonAsync("/api/v1/auth/inscription", new
        {
            Email = $"{prefixe}-{Guid.NewGuid():N}@ebiosrm.local",
            MotDePasse = "MotDePasseTest123",
            NomAffiche = prefixe,
        });
        inscription.EnsureSuccessStatusCode();
        var token = (await inscription.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>Crée une étude et la parcourt jusqu'à la validation de l'atelier 5.</summary>
    public static async Task<Guid> ConstruireEtudeCompleteAsync(HttpClient c)
    {
        var etudeId = (await (await c.PostAsJsonAsync("/api/v1/etudes",
            new { Nom = "Modele", Perimetre = "P", Mission = "M" })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // Atelier 1
        await c.PostAsync($"/api/v1/etudes/{etudeId}/demarrer-atelier1", null);
        var vmId = (await (await c.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/valeurs-metier",
            new { Description = "Facturation", EntiteProprietaire = "DAF" })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var bienId = (await (await c.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/valeurs-metier/{vmId}/biens-support",
            new { Description = "ERP", Type = "SystemeInformation", EntiteProprietaire = "DSI" })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var erId = (await (await c.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/valeurs-metier/{vmId}/evenements-redoutes",
            new { Description = "Fraude", Gravite = 4 })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await c.PostAsync($"/api/v1/etudes/{etudeId}/socle-securite", null)).EnsureSuccessStatusCode();
        await c.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/socle-securite/referentiels",
            new { Nom = "ISO 27001 A.8.24", Etat = "NonConforme", Theme = "Technologique", CodeControle = "A.8.24", EtatActuel = "Pas de chiffrement" });
        await c.PostAsync($"/api/v1/etudes/{etudeId}/valider-atelier1", null);

        // Atelier 2
        await c.PostAsync($"/api/v1/etudes/{etudeId}/demarrer-atelier2", null);
        var coupleId = (await (await c.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/couples-sr-ov", new
        {
            SourceRisque = "CrimeOrganise",
            DescriptionSourceRisque = "Groupe rançongiciel",
            ObjectifVise = "Lucratif",
            DescriptionObjectifVise = "Extorsion",
            ContexteVulnerabilite = "Exposition RDP",
            Theme = "Technologique",
            Motivation = 4,
            Ressources = 3,
        })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var partieId = (await (await c.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/parties-prenantes", new
        {
            Nom = "Infogéreur",
            RolesEtAttentes = "Exploitation",
            Representant = "ACME IT",
            Categorie = "Prestataire",
        })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await c.PostAsync($"/api/v1/etudes/{etudeId}/valider-atelier2", null);

        // Atelier 3
        await c.PostAsync($"/api/v1/etudes/{etudeId}/demarrer-atelier3", null);
        await c.PutAsJsonAsync($"/api/v1/etudes/{etudeId}/parties-prenantes/{partieId}/dangerosite",
            new { Dependance = 4, Penetration = 3, MaturiteCyber = 2, Confiance = 2 });
        var scenarioId = (await (await c.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/couples-sr-ov/{coupleId}/scenario-strategique",
            new { EvenementRedouteId = erId, Description = "Rançongiciel via l'infogéreur" })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var cheminId = (await (await c.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/scenarios-strategiques/{scenarioId}/chemins-attaque",
            new { Description = "Rebond depuis le SI de l'infogéreur" })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await c.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/chemins-attaque/{cheminId}/evenements-intermediaires",
            new { PartiePrenanteId = partieId, Description = "Franchissement de l'infogéreur" })).EnsureSuccessStatusCode();
        await c.PostAsync($"/api/v1/etudes/{etudeId}/valider-atelier3", null);

        // Atelier 4
        await c.PostAsync($"/api/v1/etudes/{etudeId}/demarrer-atelier4", null);
        var scenarioOpId = (await (await c.PostAsync($"/api/v1/etudes/{etudeId}/chemins-attaque/{cheminId}/scenario-operationnel", null))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await c.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/scenarios-operationnels/{scenarioOpId}/modes-operatoires", new
        {
            Description = "Chiffrement des serveurs",
            Actions = new[] { new { Description = "Déploiement du rançongiciel", Phase = "Exploiter", BienSupportId = bienId } },
            ProbabiliteSucces = 3,
            DifficulteTechnique = 2,
        });
        await c.PostAsync($"/api/v1/etudes/{etudeId}/valider-atelier4", null);

        // Atelier 5
        await c.PostAsync($"/api/v1/etudes/{etudeId}/demarrer-atelier5", null);
        var srId = (await (await c.PostAsync($"/api/v1/etudes/{etudeId}/chemins-attaque/{cheminId}/scenario-de-risque", null))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await c.PutAsJsonAsync($"/api/v1/etudes/{etudeId}/scenarios-de-risque/{srId}/risque-residuel",
            new { GraviteResiduelle = 2, VraisemblanceResiduelle = "V2" });
        await c.PostAsync($"/api/v1/etudes/{etudeId}/plan-traitement-risque", null);
        await c.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/plan-traitement-risque/mesures", new
        {
            Description = "Sauvegardes hors-ligne",
            Axe = "Resilience",
            ScenariosDeRisqueIds = new[] { srId },
            Responsable = "RSSI",
            FreinsEtDifficultes = (string?)null,
            CoutComplexite = "PlusPlus",
            Echeance = "3 mois",
            Statut = "ALancer",
        });
        await c.PostAsync($"/api/v1/etudes/{etudeId}/valider-atelier5", null);

        return etudeId;
    }

    public static HashSet<Guid> IdSet(JsonElement export, string collection)
        => export.GetProperty(collection).EnumerateArray().Select(e => e.GetProperty("id").GetGuid()).ToHashSet();

    public static HashSet<Guid> TousLesIds(JsonElement export)
    {
        var ids = new HashSet<Guid>();
        void Visiter(JsonElement el)
        {
            switch (el.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var prop in el.EnumerateObject())
                    {
                        if (prop.Name == "id" && prop.Value.ValueKind == JsonValueKind.String && prop.Value.TryGetGuid(out var g))
                            ids.Add(g);
                        else
                            Visiter(prop.Value);
                    }
                    break;
                case JsonValueKind.Array:
                    foreach (var item in el.EnumerateArray())
                        Visiter(item);
                    break;
            }
        }
        Visiter(export);
        return ids;
    }

    /// <summary>
    /// Vérifie que la copie/import a le bon nombre d'éléments, aucun Id partagé
    /// avec la source, et une intégrité référentielle interne complète.
    /// </summary>
    public static void VerifierGrapheRecable(JsonElement source, JsonElement copie)
    {
        foreach (var collection in new[]
        {
            "valeursMetier", "biensSupport", "evenementsRedoutes", "couplesSourceRisqueObjectifVise",
            "partiesPrenantes", "scenariosStrategiques", "cheminsAttaque", "scenariosOperationnels", "scenariosDeRisque",
        })
        {
            Assert.Equal(source.GetProperty(collection).GetArrayLength(), copie.GetProperty(collection).GetArrayLength());
        }

        Assert.Empty(TousLesIds(source).Intersect(TousLesIds(copie)));

        var vm = IdSet(copie, "valeursMetier");
        Assert.All(copie.GetProperty("biensSupport").EnumerateArray(),
            b => Assert.Contains(b.GetProperty("valeurMetierId").GetGuid(), vm));

        var couples = IdSet(copie, "couplesSourceRisqueObjectifVise");
        var ers = IdSet(copie, "evenementsRedoutes");
        Assert.All(copie.GetProperty("scenariosStrategiques").EnumerateArray(), s =>
        {
            Assert.Contains(s.GetProperty("coupleSourceRisqueObjectifViseId").GetGuid(), couples);
            Assert.Contains(s.GetProperty("evenementRedouteId").GetGuid(), ers);
        });

        var ss = IdSet(copie, "scenariosStrategiques");
        var pp = IdSet(copie, "partiesPrenantes");
        var bs = IdSet(copie, "biensSupport");
        foreach (var chemin in copie.GetProperty("cheminsAttaque").EnumerateArray())
        {
            Assert.Contains(chemin.GetProperty("scenarioStrategiqueId").GetGuid(), ss);
            foreach (var evi in chemin.GetProperty("evenementsIntermediaires").EnumerateArray())
                Assert.Contains(evi.GetProperty("partiePrenanteId").GetGuid(), pp);
        }

        foreach (var so in copie.GetProperty("scenariosOperationnels").EnumerateArray())
            foreach (var mo in so.GetProperty("modesOperatoires").EnumerateArray())
                foreach (var ae in mo.GetProperty("actionsElementaires").EnumerateArray())
                    Assert.Contains(ae.GetProperty("bienSupportId").GetGuid(), bs);

        var sr = IdSet(copie, "scenariosDeRisque");
        foreach (var mesure in copie.GetProperty("planTraitementRisque").GetProperty("mesures").EnumerateArray())
            foreach (var refSr in mesure.GetProperty("scenariosDeRisqueIds").EnumerateArray())
                Assert.Contains(refSr.GetGuid(), sr);
    }
}
