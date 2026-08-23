using System.Text.Json;
using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;
using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;
using EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;
using EbiosRM.Api.Tests.TestDoubles;

namespace EbiosRM.Api.Tests.Domain.ScenariosDeRisque;

public class ServiceCreationSnapshotAtelier5Tests
{
    private static (ServiceCreationSnapshotAtelier5 Service, FakeEtudeRepository Etudes, FakeScenarioDeRisqueRepository Scenarios,
        FakeCheminAttaqueRepository Chemins, FakeScenarioStrategiqueRepository ScenariosStrategiques,
        FakeEvenementRedouteRepository EvenementsRedoutes, FakeScenarioOperationnelRepository ScenariosOp,
        FakeCoupleSourceRisqueObjectifViseRepository Couples, FakePlanTraitementRisqueRepository Plans,
        FakeSnapshotAtelierRepository Snapshots) CreerService()
    {
        var etudes = new FakeEtudeRepository();
        var scenarios = new FakeScenarioDeRisqueRepository();
        var chemins = new FakeCheminAttaqueRepository();
        var scenariosStrategiques = new FakeScenarioStrategiqueRepository();
        var evenementsRedoutes = new FakeEvenementRedouteRepository();
        var scenariosOp = new FakeScenarioOperationnelRepository();
        var couples = new FakeCoupleSourceRisqueObjectifViseRepository();
        var plans = new FakePlanTraitementRisqueRepository();
        var snapshots = new FakeSnapshotAtelierRepository();
        var assemblage = new ServiceAssemblageScenariosDeRisque(scenarios, chemins, scenariosStrategiques, evenementsRedoutes, scenariosOp, couples);
        var service = new ServiceCreationSnapshotAtelier5(etudes, assemblage, plans, snapshots);
        return (service, etudes, scenarios, chemins, scenariosStrategiques, evenementsRedoutes, scenariosOp, couples, plans, snapshots);
    }

    private static Etude CreerEtudeAvecAtelier5Valide()
    {
        var etude = Etude.Creer("Etude test", "Perimetre", "Mission");
        etude.DemarrerAtelier1();
        etude.ValiderAtelier1();
        etude.DemarrerAtelier2();
        etude.ValiderAtelier2();
        etude.DemarrerAtelier3();
        etude.ValiderAtelier3();
        etude.DemarrerAtelier4();
        etude.ValiderAtelier4();
        etude.DemarrerAtelier5();
        etude.ValiderAtelier5();
        return etude;
    }

    [Fact]
    public async Task CreerAsync_refuse_si_atelier_5_non_valide()
    {
        var (service, etudes, _, _, _, _, _, _, _, _) = CreerService();
        var etude = Etude.Creer("Etude test", "Perimetre", "Mission");
        etudes.Etudes.Add(etude);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreerAsync(etude.Id, CancellationToken.None));
    }

    [Fact]
    public async Task CreerAsync_fige_le_niveau_de_risque_et_le_plan_de_traitement()
    {
        var (service, etudes, scenarios, chemins, scenariosStrategiques, evenementsRedoutes, scenariosOp, couples, plans, _) = CreerService();
        var etude = CreerEtudeAvecAtelier5Valide();
        etudes.Etudes.Add(etude);

        var er = EvenementRedoute.Creer(etude.Id, Guid.NewGuid(), "Perte de confidentialité", gravite: 3);
        evenementsRedoutes.Items.Add(er);

        var couple = CoupleSourceRisqueObjectifVise.Creer(
            etude.Id, CategorieSourceRisque.Etatique, "Description SR", CategorieObjectifVise.Lucratif, "Description OV",
            "Contexte", "Technologique", 4, 4, ServiceCalculPertinence.Calculer(4, 4));
        couples.Items.Add(couple);

        var scenarioStrat = ScenarioStrategique.Creer(etude.Id, couple.Id, er.Id, "Compromission");
        scenariosStrategiques.Items.Add(scenarioStrat);

        var chemin = CheminAttaque.Creer(etude.Id, scenarioStrat.Id, "Intrusion frontale");
        chemins.Items.Add(chemin);

        var scenarioOp = ScenarioOperationnel.Creer(etude.Id, chemin.Id);
        var actions = new[] { new ActionElementaireEntree("Exploitation d'une CVE", PhaseActionElementaire.Exploiter, Guid.NewGuid()) };
        scenarioOp.AjouterModeOperatoire("Mode test", actions, probabiliteSucces: 3, difficulteTechnique: 2);
        scenariosOp.Items.Add(scenarioOp);

        var scenarioDeRisque = ScenarioDeRisque.Creer(etude.Id, chemin.Id);
        scenarioDeRisque.EvaluerRisqueResiduel(1, EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque.NiveauVraisemblance.V1, NiveauRisque.Faible);
        scenarios.Items.Add(scenarioDeRisque);

        var plan = PlanTraitementRisque.Creer(etude.Id);
        plan.AjouterMesure("Mesure", AxeMesure.Protection, new List<Guid> { scenarioDeRisque.Id }, "RSSI", null, NiveauCoutComplexite.Plus, "6 mois", StatutMesure.ALancer);
        plans.Items.Add(plan);

        var snapshot = await service.CreerAsync(etude.Id, CancellationToken.None);

        var contenu = JsonSerializer.Deserialize<SnapshotAtelier5Content>(snapshot.ContenuJson);
        Assert.NotNull(contenu);
        var scenarioSnapshot = Assert.Single(contenu!.ScenariosDeRisque);
        Assert.Equal(3, scenarioSnapshot.Gravite);
        Assert.Equal(NiveauRisque.Eleve, scenarioSnapshot.NiveauRisqueInitial);
        Assert.Equal(NiveauRisque.Faible, scenarioSnapshot.NiveauRisqueResiduel);
        var mesureSnapshot = Assert.Single(contenu.Mesures);
        Assert.Contains(scenarioDeRisque.Id, mesureSnapshot.ScenariosDeRisqueIds);
    }
}
