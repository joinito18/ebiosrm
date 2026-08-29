using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using static EbiosRM.Api.Tests.Integration.OutilsEtudeDeTest;

namespace EbiosRM.Api.Tests.Integration;

/// <summary>
/// Bibliothèque d'éléments réutilisables : catalogue système (ISO 27002 +
/// hygiène ANSSI, dans le code, non modifiable) fusionné avec les entrées
/// personnelles de l'appelant (persistées, isolées par utilisateur).
/// </summary>
public class BibliothequeTests : IClassFixture<EbiosApiFactory>
{
    private readonly EbiosApiFactory _factory;

    public BibliothequeTests(EbiosApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Le_catalogue_systeme_de_mesures_est_toujours_present_et_non_modifiable()
    {
        var c = await NouveauCompteAsync(_factory, "biblio-cat");

        var toutes = await (await c.GetAsync("/api/v1/bibliotheque/mesures")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(toutes.GetArrayLength() >= 135); // 93 ISO 27002 + 42 hygiène ANSSI

        var iso = await (await c.GetAsync("/api/v1/bibliotheque/mesures?referentiel=Iso27002")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(93, iso.GetArrayLength());
        Assert.All(iso.EnumerateArray(), m => Assert.True(m.GetProperty("systeme").GetBoolean()));

        var anssi = await (await c.GetAsync("/api/v1/bibliotheque/mesures?referentiel=HygieneAnssi&q=sauvegarde")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, anssi.GetArrayLength());
        Assert.Contains("sauvegarde", anssi[0].GetProperty("titre").GetString(), StringComparison.OrdinalIgnoreCase);

        // Impossible de supprimer une entrée du catalogue système.
        var idSysteme = iso[0].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await c.DeleteAsync($"/api/v1/bibliotheque/mesures/{idSysteme}")).StatusCode);
    }

    [Fact]
    public async Task Une_mesure_personnelle_est_ajoutee_visible_puis_retiree()
    {
        var c = await NouveauCompteAsync(_factory, "biblio-perso");

        var ajout = await c.PostAsJsonAsync("/api/v1/bibliotheque/mesures", new
        {
            Titre = "MFA sur tous les acces d'administration",
            Description = "Clé physique obligatoire",
            Categorie = "Protection",
        });
        Assert.Equal(HttpStatusCode.Created, ajout.StatusCode);
        var creee = await ajout.Content.ReadFromJsonAsync<JsonElement>();
        var id = creee.GetProperty("id").GetGuid();
        Assert.False(creee.GetProperty("systeme").GetBoolean());
        Assert.Equal("Libre", creee.GetProperty("referentiel").GetString());

        var apresAjout = await (await c.GetAsync("/api/v1/bibliotheque/mesures?q=administration")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(apresAjout.EnumerateArray(), m => m.GetProperty("id").GetGuid() == id);

        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/v1/bibliotheque/mesures/{id}")).StatusCode);

        var apresRetrait = await (await c.GetAsync("/api/v1/bibliotheque/mesures?q=administration")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.DoesNotContain(apresRetrait.EnumerateArray(), m => m.GetProperty("id").GetGuid() == id);
    }

    [Fact]
    public async Task La_bibliotheque_personnelle_est_isolee_par_utilisateur()
    {
        var alice = await NouveauCompteAsync(_factory, "biblio-alice");
        var bob = await NouveauCompteAsync(_factory, "biblio-bob");

        var id = (await (await alice.PostAsJsonAsync("/api/v1/bibliotheque/mesures", new { Titre = "Mesure privée d'Alice" }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var vueBob = await (await bob.GetAsync("/api/v1/bibliotheque/mesures")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.DoesNotContain(vueBob.EnumerateArray(), m => m.GetProperty("id").GetGuid() == id);

        // Bob ne peut pas non plus la supprimer.
        Assert.Equal(HttpStatusCode.NotFound, (await bob.DeleteAsync($"/api/v1/bibliotheque/mesures/{id}")).StatusCode);

        // Mais Bob voit bien le catalogue système, comme tout le monde.
        var isoBob = await (await bob.GetAsync("/api/v1/bibliotheque/mesures?referentiel=Iso27002")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(93, isoBob.GetArrayLength());
    }

    [Fact]
    public async Task Sources_de_risque_catalogue_plus_perso()
    {
        var c = await NouveauCompteAsync(_factory, "biblio-sr");

        var catalogue = await (await c.GetAsync("/api/v1/bibliotheque/sources-risque")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(catalogue.GetArrayLength() >= 9);
        Assert.All(catalogue.EnumerateArray(), s => Assert.True(s.GetProperty("systeme").GetBoolean()));

        var ajout = await c.PostAsJsonAsync("/api/v1/bibliotheque/sources-risque", new
        {
            SourceRisque = "Vengeur",
            DescriptionSourceRisque = "Administrateur licencié",
            ObjectifVise = "EntraveAuFonctionnement",
            DescriptionObjectifVise = "Sabotage des sauvegardes",
            Theme = "Personnes",
            MotivationTypique = 3,
            RessourcesTypiques = 2,
        });
        Assert.Equal(HttpStatusCode.Created, ajout.StatusCode);
        var id = (await ajout.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var apres = await (await c.GetAsync("/api/v1/bibliotheque/sources-risque?q=licencié")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(apres.EnumerateArray(), s => s.GetProperty("id").GetGuid() == id && !s.GetProperty("systeme").GetBoolean());

        // Catégorie invalide rejetée.
        var mauvais = await c.PostAsJsonAsync("/api/v1/bibliotheque/sources-risque", new
        {
            SourceRisque = "PasUneCategorie",
            DescriptionSourceRisque = "x",
            ObjectifVise = "Lucratif",
            DescriptionObjectifVise = "x",
        });
        Assert.Equal(HttpStatusCode.BadRequest, mauvais.StatusCode);
    }

    [Fact]
    public async Task Parties_prenantes_catalogue_plus_perso_isole()
    {
        var alice = await NouveauCompteAsync(_factory, "biblio-pp-a");
        var bob = await NouveauCompteAsync(_factory, "biblio-pp-b");

        var catalogue = await (await alice.GetAsync("/api/v1/bibliotheque/parties-prenantes")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(catalogue.GetArrayLength() >= 15);
        Assert.All(catalogue.EnumerateArray(), p => Assert.True(p.GetProperty("systeme").GetBoolean()));
        Assert.Contains(catalogue.EnumerateArray(), p => p.GetProperty("nom").GetString() == "Infogéreur");

        var ajout = await alice.PostAsJsonAsync("/api/v1/bibliotheque/parties-prenantes", new
        {
            Nom = "Prestataire de scan de vulnérabilités",
            Categorie = "Prestataire",
            RolesEtAttentes = "Audit technique périodique du SI exposé",
            DependanceTypique = 2,
            PenetrationTypique = 3,
        });
        Assert.Equal(HttpStatusCode.Created, ajout.StatusCode);
        var id = (await ajout.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var vueAlice = await (await alice.GetAsync("/api/v1/bibliotheque/parties-prenantes?q=scan")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(vueAlice.EnumerateArray(), p => p.GetProperty("id").GetGuid() == id && p.GetProperty("penetrationTypique").GetInt32() == 3);

        var vueBob = await (await bob.GetAsync("/api/v1/bibliotheque/parties-prenantes")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.DoesNotContain(vueBob.EnumerateArray(), p => p.GetProperty("id").GetGuid() == id);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.DeleteAsync($"/api/v1/bibliotheque/parties-prenantes/{id}")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await alice.DeleteAsync($"/api/v1/bibliotheque/parties-prenantes/{id}")).StatusCode);
    }

    [Fact]
    public async Task Valeurs_metier_biens_support_evenements_redoutes_catalogues_fournis()
    {
        var c = await NouveauCompteAsync(_factory, "biblio-a1");

        var vm = await (await c.GetAsync("/api/v1/bibliotheque/valeurs-metier")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(vm.GetArrayLength() >= 15);

        var bs = await (await c.GetAsync("/api/v1/bibliotheque/biens-support?type=Reseau")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(bs.GetArrayLength() >= 3);
        Assert.All(bs.EnumerateArray(), b => Assert.Equal("Reseau", b.GetProperty("type").GetString()));

        var er = await (await c.GetAsync("/api/v1/bibliotheque/evenements-redoutes?q=rançongiciel")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, er.GetArrayLength());
        Assert.Equal(4, er[0].GetProperty("graviteIndicative").GetInt32());

        // Ajout perso + gravité indicative hors échelle rejetée.
        var ok = await c.PostAsJsonAsync("/api/v1/bibliotheque/evenements-redoutes", new { Intitule = "Perte du contrat cadre", GraviteIndicative = 3, ImpactsTypes = "Financier" });
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        var ko = await c.PostAsJsonAsync("/api/v1/bibliotheque/evenements-redoutes", new { Intitule = "x", GraviteIndicative = 9 });
        Assert.Equal(HttpStatusCode.BadRequest, ko.StatusCode);

        var vmAjout = await c.PostAsJsonAsync("/api/v1/bibliotheque/valeurs-metier", new { Intitule = "Processus d'homologation", NatureOuFinalite = "Processus" });
        Assert.Equal(HttpStatusCode.Created, vmAjout.StatusCode);
        var vmId = (await vmAjout.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/v1/bibliotheque/valeurs-metier/{vmId}")).StatusCode);
    }

    [Fact]
    public async Task Modes_operatoires_catalogue_avec_actions_et_ajout_perso()
    {
        var c = await NouveauCompteAsync(_factory, "biblio-mo");

        var catalogue = await (await c.GetAsync("/api/v1/bibliotheque/modes-operatoires")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(catalogue.GetArrayLength() >= 5);
        var rancongiciel = catalogue.EnumerateArray().First(m => m.GetProperty("nom").GetString()!.Contains("hameçonnage"));
        var actions = rancongiciel.GetProperty("actions");
        Assert.True(actions.GetArrayLength() >= 4);
        // Les actions sont ordonnées et portent une phase de la séquence EBIOS RM.
        Assert.Equal(1, actions[0].GetProperty("ordre").GetInt32());
        Assert.Contains(actions.EnumerateArray(), a => a.GetProperty("phase").GetString() == "Exploiter");
        Assert.Contains(actions.EnumerateArray(), a => a.GetProperty("techniqueMitre").GetString() == "T1486");

        // Recherche plein texte jusque dans les actions.
        var parMitre = await (await c.GetAsync("/api/v1/bibliotheque/modes-operatoires?q=T1486")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(parMitre.EnumerateArray(), m => m.GetProperty("nom").GetString()!.Contains("hameçonnage"));

        var ajout = await c.PostAsJsonAsync("/api/v1/bibliotheque/modes-operatoires", new
        {
            Nom = "Attaque physique du datacenter",
            Description = "Accès non autorisé aux baies",
            ProbabiliteSuccesTypique = 2,
            DifficulteTechniqueTypique = 3,
            Actions = new[]
            {
                new { Description = "Repérage des accès et des rondes", Phase = "Connaitre", CibleBienSupport = "Salle serveurs", TechniqueMitre = (string?)null },
                new { Description = "Intrusion et branchement d'un implant", Phase = "Exploiter", CibleBienSupport = "Baie de serveurs", TechniqueMitre = (string?)"T1200" },
            },
        });
        Assert.Equal(HttpStatusCode.Created, ajout.StatusCode);
        var cree = await ajout.Content.ReadFromJsonAsync<JsonElement>();
        var id = cree.GetProperty("id").GetGuid();
        Assert.Equal(2, cree.GetProperty("actions").GetArrayLength());
        Assert.False(cree.GetProperty("systeme").GetBoolean());

        // Sans action -> refusé.
        var vide = await c.PostAsJsonAsync("/api/v1/bibliotheque/modes-operatoires", new { Nom = "x", Actions = new object[0] });
        Assert.Equal(HttpStatusCode.BadRequest, vide.StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/v1/bibliotheque/modes-operatoires/{id}")).StatusCode);
        // Le catalogue système reste non supprimable.
        var idSys = rancongiciel.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await c.DeleteAsync($"/api/v1/bibliotheque/modes-operatoires/{idSys}")).StatusCode);
    }
}
