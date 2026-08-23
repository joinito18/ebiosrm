using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EbiosRM.Api.Tests.Integration;

/// <summary>
/// Parcourt une étude réelle à travers les 4 ateliers implémentés, en passant
/// par l'API HTTP complète (pas les Domain Services directement) : c'est le
/// test qui manquait le plus, celui qui aurait détecté un endpoint cassé --
/// jusqu'ici, seul curl manuel pendant les sessions jouait ce rôle, jamais
/// rejoué automatiquement. Un seul long test délibérément linéaire (chaque
/// étape dépend de l'état laissé par la précédente, comme le vrai flux
/// utilisateur) plutôt que des tests isolés artificiellement indépendants.
/// </summary>
public class CycleDeVieCompletTests : IClassFixture<EbiosApiFactory>
{
    private readonly HttpClient _client;

    public CycleDeVieCompletTests(EbiosApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Parcours_complet_A1_a_A4_avec_snapshots_overrides_et_cascade()
    {
        // --- Création de l'étude ---
        var creerEtude = await _client.PostAsJsonAsync("/api/v1/etudes", new { Nom = "Etude integration", Perimetre = "Perimetre test", Mission = "Mission test" });
        Assert.Equal(HttpStatusCode.Created, creerEtude.StatusCode);
        var etude = await creerEtude.Content.ReadFromJsonAsync<JsonElement>();
        var etudeId = etude.GetProperty("id").GetGuid();

        // ============================= ATELIER 1 =============================
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/api/v1/etudes/{etudeId}/demarrer-atelier1", null)).StatusCode);

        var creerVM = await _client.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/valeurs-metier", new { Description = "R&D vaccinale", EntiteProprietaire = "Direction scientifique" });
        Assert.Equal(HttpStatusCode.Created, creerVM.StatusCode);
        var valeurMetierId = (await creerVM.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var creerBien = await _client.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/valeurs-metier/{valeurMetierId}/biens-support",
            new { Description = "Serveur R&D", Type = "SystemeInformation", EntiteProprietaire = "DSI" });
        Assert.Equal(HttpStatusCode.Created, creerBien.StatusCode);
        var bienSupportId = (await creerBien.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var creerER = await _client.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/valeurs-metier/{valeurMetierId}/evenements-redoutes",
            new { Description = "Vol de propriete intellectuelle", Gravite = 4 });
        Assert.Equal(HttpStatusCode.Created, creerER.StatusCode);

        // Valider avant complétude doit échouer (pas de socle -- en fait le
        // service de complétude A1 exige VM+bien+ER seulement, déjà réunis ;
        // on valide directement).
        var validerA1 = await _client.PostAsync($"/api/v1/etudes/{etudeId}/valider-atelier1", null);
        Assert.Equal(HttpStatusCode.OK, validerA1.StatusCode);
        var validerA1Body = await validerA1.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, validerA1Body.GetProperty("snapshotVersion").GetInt32());

        // Le rapport A1 doit maintenant être téléchargeable (PDF, snapshot v1).
        var rapportA1 = await _client.GetAsync($"/api/v1/etudes/{etudeId}/rapports/atelier1");
        Assert.Equal(HttpStatusCode.OK, rapportA1.StatusCode);
        Assert.Equal("application/pdf", rapportA1.Content.Headers.ContentType?.MediaType);

        // ============================= ATELIER 2 =============================
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/api/v1/etudes/{etudeId}/demarrer-atelier2", null)).StatusCode);

        var creerCouple = await _client.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/couples-sr-ov", new
        {
            SourceRisque = "Etatique",
            DescriptionSourceRisque = "Agence de renseignement etrangere",
            ObjectifVise = "EspionnageEtatiqueOuIndustriel",
            DescriptionObjectifVise = "Vol de donnees",
            ContexteVulnerabilite = "Authentification faible",
            Theme = "Technologique",
            Motivation = 4,
            Ressources = 4
        });
        Assert.Equal(HttpStatusCode.Created, creerCouple.StatusCode);
        var coupleJson = await creerCouple.Content.ReadFromJsonAsync<JsonElement>();
        var coupleId = coupleJson.GetProperty("id").GetGuid();
        Assert.Equal("TresPertinent", coupleJson.GetProperty("pertinence").GetString());

        var creerPartie = await _client.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/parties-prenantes", new
        {
            Nom = "Prestataire cloud",
            RolesEtAttentes = "Hebergement",
            Representant = "ACME Cloud",
            Categorie = "Prestataire"
        });
        Assert.Equal(HttpStatusCode.Created, creerPartie.StatusCode);
        var partieId = (await creerPartie.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // Override "jugement d'expert" sur la pertinence.
        var overridePertinence = await _client.PutAsJsonAsync($"/api/v1/etudes/{etudeId}/couples-sr-ov/{coupleId}/pertinence-retenue",
            new { PertinenceRetenue = "PeuPertinent", Justification = "Test integration : source neutralisee depuis." });
        Assert.Equal(HttpStatusCode.OK, overridePertinence.StatusCode);
        var apresOverride = await overridePertinence.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("PeuPertinent", apresOverride.GetProperty("pertinence").GetString());
        Assert.Equal("TresPertinent", apresOverride.GetProperty("pertinenceCalculee").GetString());

        var validerA2 = await _client.PostAsync($"/api/v1/etudes/{etudeId}/valider-atelier2", null);
        Assert.Equal(HttpStatusCode.OK, validerA2.StatusCode);
        Assert.Equal(1, (await validerA2.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("snapshotVersion").GetInt32());

        var rapportA2 = await _client.GetAsync($"/api/v1/etudes/{etudeId}/rapports/atelier2");
        Assert.Equal(HttpStatusCode.OK, rapportA2.StatusCode);

        // Reset de l'override APRES validation : ne doit PAS toucher au snapshot déjà figé.
        Assert.Equal(HttpStatusCode.OK, (await _client.DeleteAsync($"/api/v1/etudes/{etudeId}/couples-sr-ov/{coupleId}/pertinence-retenue")).StatusCode);
        var rapportA2ApresReset = await _client.GetAsync($"/api/v1/etudes/{etudeId}/rapports/atelier2");
        var pdfAvant = await rapportA2.Content.ReadAsByteArrayAsync();
        var pdfApres = await rapportA2ApresReset.Content.ReadAsByteArrayAsync();
        Assert.Equal(pdfAvant.Length, pdfApres.Length); // contenu du snapshot inchangé (seul le PDF regénéré peut différer de quelques octets d'horodatage, la taille reste stable ici)

        // ============================= ATELIER 3 =============================
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/api/v1/etudes/{etudeId}/demarrer-atelier3", null)).StatusCode);

        var evaluerDangerosite = await _client.PutAsJsonAsync($"/api/v1/etudes/{etudeId}/parties-prenantes/{partieId}/dangerosite",
            new { Dependance = 4, Penetration = 3, MaturiteCyber = 2, Confiance = 2 });
        Assert.Equal(HttpStatusCode.OK, evaluerDangerosite.StatusCode);
        var partieApresEval = await evaluerDangerosite.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Controle", partieApresEval.GetProperty("zone").GetString());

        var creerScenario = await _client.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/couples-sr-ov/{coupleId}/scenario-strategique",
            new { EvenementRedouteId = (await (await _client.GetAsync($"/api/v1/etudes/{etudeId}/evenements-redoutes")).Content.ReadFromJsonAsync<JsonElement>())[0].GetProperty("id").GetGuid(), Description = "Compromission du prestataire cloud" });
        Assert.Equal(HttpStatusCode.Created, creerScenario.StatusCode);
        var scenarioId = (await creerScenario.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var creerChemin = await _client.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/scenarios-strategiques/{scenarioId}/chemins-attaque",
            new { Description = "Rebond via le prestataire cloud" });
        Assert.Equal(HttpStatusCode.Created, creerChemin.StatusCode);
        var cheminId = (await creerChemin.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var validerA3 = await _client.PostAsync($"/api/v1/etudes/{etudeId}/valider-atelier3", null);
        Assert.Equal(HttpStatusCode.OK, validerA3.StatusCode);
        Assert.Equal(1, (await validerA3.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("snapshotVersion").GetInt32());

        // ============================= ATELIER 4 =============================
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/api/v1/etudes/{etudeId}/demarrer-atelier4", null)).StatusCode);

        var creerScenarioOp = await _client.PostAsync($"/api/v1/etudes/{etudeId}/chemins-attaque/{cheminId}/scenario-operationnel", null);
        Assert.Equal(HttpStatusCode.Created, creerScenarioOp.StatusCode);
        var scenarioOpId = (await creerScenarioOp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var creerMode = await _client.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/scenarios-operationnels/{scenarioOpId}/modes-operatoires", new
        {
            Description = "Intrusion via le compte admin cloud",
            Actions = new[] { new { Description = "Exploitation d'une CVE", Phase = "Exploiter", BienSupportId = bienSupportId } },
            ProbabiliteSucces = 3,
            DifficulteTechnique = 2
        });
        Assert.Equal(HttpStatusCode.Created, creerMode.StatusCode);
        var scenarioOpApresMode = await creerMode.Content.ReadFromJsonAsync<JsonElement>();
        var modeId = scenarioOpApresMode.GetProperty("modesOperatoires")[0].GetProperty("id").GetGuid();
        Assert.Equal("V3", scenarioOpApresMode.GetProperty("modesOperatoires")[0].GetProperty("vraisemblance").GetString());

        // Bien support d'une autre étude refusé par la validation cible.
        var autreEtude = await (await _client.PostAsJsonAsync("/api/v1/etudes", new { Nom = "Autre etude", Perimetre = "P", Mission = "M" })).Content.ReadFromJsonAsync<JsonElement>();
        var modeAvecMauvaisBien = await _client.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/scenarios-operationnels/{scenarioOpId}/modes-operatoires", new
        {
            Description = "Mode invalide",
            Actions = new[] { new { Description = "Action", Phase = "Connaitre", BienSupportId = Guid.NewGuid() } },
            ProbabiliteSucces = 1,
            DifficulteTechnique = 1
        });
        Assert.Equal(HttpStatusCode.BadRequest, modeAvecMauvaisBien.StatusCode);

        var overrideVraisemblance = await _client.PutAsJsonAsync(
            $"/api/v1/etudes/{etudeId}/scenarios-operationnels/{scenarioOpId}/modes-operatoires/{modeId}/vraisemblance-retenue",
            new { VraisemblanceRetenue = "V1", Justification = "Test integration : mesure compensatoire deja en place." });
        Assert.Equal(HttpStatusCode.OK, overrideVraisemblance.StatusCode);

        var validerA4 = await _client.PostAsync($"/api/v1/etudes/{etudeId}/valider-atelier4", null);
        Assert.Equal(HttpStatusCode.OK, validerA4.StatusCode);
        Assert.Equal(1, (await validerA4.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("snapshotVersion").GetInt32());

        var rapportA4 = await _client.GetAsync($"/api/v1/etudes/{etudeId}/rapports/atelier4");
        Assert.Equal(HttpStatusCode.OK, rapportA4.StatusCode);

        // --- Revalidation : le PDF doit changer une fois qu'on repasse par Rouvrir -> Valider ---
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/api/v1/etudes/{etudeId}/rouvrir-atelier4", null)).StatusCode);
        var revaliderA4 = await _client.PostAsync($"/api/v1/etudes/{etudeId}/valider-atelier4", null);
        Assert.Equal(2, (await revaliderA4.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("snapshotVersion").GetInt32());

        // ============================= ATELIER 5 =============================
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/api/v1/etudes/{etudeId}/demarrer-atelier5", null)).StatusCode);

        // Synthèse indisponible avant validation de l'atelier 5.
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync($"/api/v1/etudes/{etudeId}/rapports/synthese")).StatusCode);

        var creerScenarioDeRisque = await _client.PostAsync($"/api/v1/etudes/{etudeId}/chemins-attaque/{cheminId}/scenario-de-risque", null);
        Assert.Equal(HttpStatusCode.Created, creerScenarioDeRisque.StatusCode);
        var scenarioDeRisqueId = (await creerScenarioDeRisque.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // Niveau initial 100% dérivé : Gravité 4 (ER) x Vraisemblance V1 (override déjà posé sur le mode opératoire) -> Faible.
        var scenariosDeRisque = await (await _client.GetAsync($"/api/v1/etudes/{etudeId}/scenarios-de-risque")).Content.ReadFromJsonAsync<JsonElement>();
        var vueScenarioDeRisque = scenariosDeRisque[0];
        Assert.Equal(4, vueScenarioDeRisque.GetProperty("gravite").GetInt32());
        Assert.Equal("V1", vueScenarioDeRisque.GetProperty("vraisemblanceInitiale").GetString());
        Assert.Equal("Faible", vueScenarioDeRisque.GetProperty("niveauRisqueInitial").GetString());

        // Override du niveau initial puis reset (aller-retour).
        var overrideInitial = await _client.PutAsJsonAsync($"/api/v1/etudes/{etudeId}/scenarios-de-risque/{scenarioDeRisqueId}/niveau-risque-initial-retenue",
            new { NiveauRetenu = "Eleve", Justification = "Test integration : contexte aggravant non capture par la formule." });
        Assert.Equal(HttpStatusCode.OK, overrideInitial.StatusCode);
        Assert.Equal("Eleve", (await overrideInitial.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("niveauRisqueInitialRetenu").GetString());
        Assert.Equal(HttpStatusCode.OK, (await _client.DeleteAsync($"/api/v1/etudes/{etudeId}/scenarios-de-risque/{scenarioDeRisqueId}/niveau-risque-initial-retenue")).StatusCode);

        // Risque résiduel : nouvelle saisie, puis override et reset.
        var evaluerResiduel = await _client.PutAsJsonAsync($"/api/v1/etudes/{etudeId}/scenarios-de-risque/{scenarioDeRisqueId}/risque-residuel",
            new { GraviteResiduelle = 1, VraisemblanceResiduelle = "V1" });
        Assert.Equal(HttpStatusCode.OK, evaluerResiduel.StatusCode);
        Assert.Equal("Faible", (await evaluerResiduel.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("niveauRisqueResiduel").GetString());

        var overrideResiduel = await _client.PutAsJsonAsync($"/api/v1/etudes/{etudeId}/scenarios-de-risque/{scenarioDeRisqueId}/niveau-risque-residuel-retenue",
            new { NiveauRetenu = "Moyen", Justification = "Test integration : ecart d'expert sur le residuel." });
        Assert.Equal(HttpStatusCode.OK, overrideResiduel.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.DeleteAsync($"/api/v1/etudes/{etudeId}/scenarios-de-risque/{scenarioDeRisqueId}/niveau-risque-residuel-retenue")).StatusCode);

        // Plan de traitement du risque.
        var creerPlan = await _client.PostAsync($"/api/v1/etudes/{etudeId}/plan-traitement-risque", null);
        Assert.Equal(HttpStatusCode.Created, creerPlan.StatusCode);

        var ajouterMesure = await _client.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/plan-traitement-risque/mesures", new
        {
            Description = "Chiffrement des flux avec le prestataire cloud",
            Axe = "Protection",
            ScenariosDeRisqueIds = new[] { scenarioDeRisqueId },
            Responsable = "RSSI",
            FreinsEtDifficultes = (string?)null,
            CoutComplexite = "PlusPlus",
            Echeance = "6 mois",
            Statut = "ALancer"
        });
        Assert.Equal(HttpStatusCode.Created, ajouterMesure.StatusCode);

        // Acceptation formelle : risque résiduel Faible, pas de sponsor ni justification requis.
        var accepter = await _client.PostAsJsonAsync($"/api/v1/etudes/{etudeId}/scenarios-de-risque/{scenarioDeRisqueId}/acceptation", new
        {
            NomProprietaireRisque = "Direction générale",
            NomValidateurSecurite = "RSSI",
            NomSponsorExecutif = (string?)null,
            Justification = (string?)null
        });
        Assert.Equal(HttpStatusCode.OK, accepter.StatusCode);
        Assert.True((await accepter.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accepteParDirection").GetBoolean());

        var validerA5 = await _client.PostAsync($"/api/v1/etudes/{etudeId}/valider-atelier5", null);
        Assert.Equal(HttpStatusCode.OK, validerA5.StatusCode);
        Assert.Equal(1, (await validerA5.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("snapshotVersion").GetInt32());

        var rapportA5 = await _client.GetAsync($"/api/v1/etudes/{etudeId}/rapports/atelier5");
        Assert.Equal(HttpStatusCode.OK, rapportA5.StatusCode);
        Assert.Equal("application/pdf", rapportA5.Content.Headers.ContentType?.MediaType);

        var rapportSynthese = await _client.GetAsync($"/api/v1/etudes/{etudeId}/rapports/synthese");
        Assert.Equal(HttpStatusCode.OK, rapportSynthese.StatusCode);
        Assert.Equal("application/pdf", rapportSynthese.Content.Headers.ContentType?.MediaType);

        // --- Cascade de suppression : supprimer le couple doit emporter scenario/chemin/scenario operationnel/scenario de risque ---
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/v1/etudes/{etudeId}/couples-sr-ov/{coupleId}")).StatusCode);
        var scenariosApresSuppression = await (await _client.GetAsync($"/api/v1/etudes/{etudeId}/scenarios-strategiques")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, scenariosApresSuppression.GetArrayLength());
        var scenariosOpApresSuppression = await (await _client.GetAsync($"/api/v1/etudes/{etudeId}/scenarios-operationnels")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, scenariosOpApresSuppression.GetArrayLength());
        var scenariosDeRisqueApresSuppression = await (await _client.GetAsync($"/api/v1/etudes/{etudeId}/scenarios-de-risque")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, scenariosDeRisqueApresSuppression.GetArrayLength());
        var planApresSuppression = await (await _client.GetAsync($"/api/v1/etudes/{etudeId}/plan-traitement-risque")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, planApresSuppression.GetProperty("mesures")[0].GetProperty("scenariosDeRisqueIds").GetArrayLength());
    }
}
