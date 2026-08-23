using System.Text.Json;
using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;
using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;
using EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;
using EbiosRM.Api.Tests.TestDoubles;

namespace EbiosRM.Api.Tests.Domain.ScenariosDeRisque;

public class ServiceCreationSnapshotAtelier3Tests
{
    private static (ServiceCreationSnapshotAtelier3 Service, FakeEtudeRepository Etudes, FakePartiePrenanteRepository Parties,
        FakeScenarioStrategiqueRepository Scenarios, FakeCoupleSourceRisqueObjectifViseRepository Couples,
        FakeEvenementRedouteRepository Evenements, FakeValeurMetierRepository Valeurs, FakeCheminAttaqueRepository Chemins,
        FakeSnapshotAtelierRepository Snapshots) CreerService()
    {
        var etudes = new FakeEtudeRepository();
        var parties = new FakePartiePrenanteRepository();
        var scenarios = new FakeScenarioStrategiqueRepository();
        var couples = new FakeCoupleSourceRisqueObjectifViseRepository();
        var evenements = new FakeEvenementRedouteRepository();
        var valeurs = new FakeValeurMetierRepository();
        var chemins = new FakeCheminAttaqueRepository();
        var snapshots = new FakeSnapshotAtelierRepository();
        var service = new ServiceCreationSnapshotAtelier3(etudes, parties, scenarios, couples, evenements, valeurs, chemins, snapshots);
        return (service, etudes, parties, scenarios, couples, evenements, valeurs, chemins, snapshots);
    }

    private static Etude CreerEtudeAvecAtelier3Valide()
    {
        var etude = Etude.Creer("Etude test", "Perimetre", "Mission");
        etude.DemarrerAtelier1();
        etude.ValiderAtelier1();
        etude.DemarrerAtelier2();
        etude.ValiderAtelier2();
        etude.DemarrerAtelier3();
        etude.ValiderAtelier3();
        return etude;
    }

    [Fact]
    public async Task CreerAsync_refuse_si_atelier_3_non_valide()
    {
        var (service, etudes, _, _, _, _, _, _, _) = CreerService();
        var etude = Etude.Creer("Etude test", "Perimetre", "Mission");
        etudes.Etudes.Add(etude);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreerAsync(etude.Id, CancellationToken.None));
    }

    [Fact]
    public async Task CreerAsync_fige_la_cartographie_complete_avec_dangerosite_et_chemins()
    {
        var (service, etudes, parties, scenarios, couples, evenements, valeurs, chemins, _) = CreerService();
        var etude = CreerEtudeAvecAtelier3Valide();
        etudes.Etudes.Add(etude);

        var valeur = ValeurMetier.Creer(etude.Id, "R&D", "Direction");
        valeurs.Items.Add(valeur);
        var evenementRedoute = EvenementRedoute.Creer(etude.Id, valeur.Id, "Vol de propriete intellectuelle", 4);
        evenements.Items.Add(evenementRedoute);

        var partie = PartiePrenante.Creer(etude.Id, "Prestataire cloud", "Hebergement", "ACME", CategoriePartiePrenante.Prestataire);
        var niveau = ServiceCalculNiveauDangerosite.Calculer(4, 3, 2, 2);
        partie.EvaluerDangerosite(4, 3, 2, 2, niveau);
        parties.Items.Add(partie);

        var couple = CoupleSourceRisqueObjectifVise.Creer(
            etude.Id, CategorieSourceRisque.Etatique, "Description SR", CategorieObjectifVise.Lucratif, "Description OV",
            "Contexte", "Technologique", 4, 4, ServiceCalculPertinence.Calculer(4, 4));
        couples.Items.Add(couple);

        var scenario = ScenarioStrategique.Creer(etude.Id, couple.Id, evenementRedoute.Id, "Compromission du prestataire");
        scenarios.Items.Add(scenario);

        var chemin = CheminAttaque.Creer(etude.Id, scenario.Id, "Rebond via le prestataire cloud");
        chemin.AjouterEvenementIntermediaire(partie.Id, "Compromission du compte admin cloud");
        chemins.Items.Add(chemin);

        var snapshot = await service.CreerAsync(etude.Id, CancellationToken.None);

        var contenu = JsonSerializer.Deserialize<EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque.SnapshotAtelier3Content>(snapshot.ContenuJson);
        Assert.NotNull(contenu);
        var partieSnapshot = Assert.Single(contenu!.PartiesPrenantes);
        Assert.Equal(niveau, partieSnapshot.NiveauDangerosite);
        Assert.Equal("Controle", partieSnapshot.Zone);

        var scenarioSnapshot = Assert.Single(contenu.ScenariosStrategiques);
        var cheminSnapshot = Assert.Single(scenarioSnapshot.CheminsAttaque);
        var eiSnapshot = Assert.Single(cheminSnapshot.EvenementsIntermediaires);
        Assert.Equal(partie.Id, eiSnapshot.PartiePrenanteId);
    }
}
