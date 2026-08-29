using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

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

    private static async Task<HttpClient> NouveauCompteAsync(EbiosApiFactory factory, string prefixe)
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

    private static async Task<Guid> ConstruireEtudeCompleteAsync(HttpClient c)
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
        var socle = await c.PostAsync($"/api/v1/etudes/{etudeId}/socle-securite", null);
        socle.EnsureSuccessStatusCode();
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
        var ei = await c.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/chemins-attaque/{cheminId}/evenements-intermediaires",
            new { PartiePrenanteId = partieId, Description = "Franchissement de l'infogéreur" });
        ei.EnsureSuccessStatusCode();
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

        // Nom + statuts : la copie repart en brouillon partout.
        Assert.Equal("Ma copie", copie.GetProperty("etude").GetProperty("nom").GetString());
        foreach (var statut in new[] { "statut", "statutAtelier2", "statutAtelier3", "statutAtelier4", "statutAtelier5" })
            Assert.Equal("Brouillon", copie.GetProperty("etude").GetProperty(statut).GetString());

        // Comptages identiques sur chaque collection.
        foreach (var collection in new[]
        {
            "valeursMetier", "biensSupport", "evenementsRedoutes", "couplesSourceRisqueObjectifVise",
            "partiesPrenantes", "scenariosStrategiques", "cheminsAttaque", "scenariosOperationnels", "scenariosDeRisque",
        })
        {
            Assert.Equal(source.GetProperty(collection).GetArrayLength(), copie.GetProperty(collection).GetArrayLength());
        }

        var idsSource = TousLesIds(source);
        var idsCopie = TousLesIds(copie);
        Assert.Empty(idsSource.Intersect(idsCopie)); // aucun Id partagé, owned compris

        // Intégrité référentielle interne à la copie.
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

        // Le lecteur est propriétaire de sa copie (elle apparaît dans sa liste avec monRole = Proprietaire).
        var mesEtudes = await (await lecteur.GetAsync("/api/v1/etudes")).Content.ReadFromJsonAsync<JsonElement>();
        var copie = mesEtudes.EnumerateArray().Single(e => e.GetProperty("id").GetGuid() == copieId);
        Assert.Equal("Proprietaire", copie.GetProperty("monRole").GetString());
    }

    private static HashSet<Guid> IdSet(JsonElement export, string collection)
        => export.GetProperty(collection).EnumerateArray().Select(e => e.GetProperty("id").GetGuid()).ToHashSet();

    private static HashSet<Guid> TousLesIds(JsonElement export)
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
}
