using System.Text.Json;
using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;
using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;
using EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;
using EbiosRM.Api.Tests.TestDoubles;

namespace EbiosRM.Api.Tests.Domain.ScenariosDeRisque;

public class ServiceCreationSnapshotAtelier4Tests
{
    private static (ServiceCreationSnapshotAtelier4 Service, FakeEtudeRepository Etudes, FakeScenarioOperationnelRepository ScenariosOp,
        FakeCheminAttaqueRepository Chemins, FakeScenarioStrategiqueRepository Scenarios, FakeCoupleSourceRisqueObjectifViseRepository Couples,
        FakeBienSupportRepository Biens, FakeSnapshotAtelierRepository Snapshots) CreerService()
    {
        var etudes = new FakeEtudeRepository();
        var scenariosOp = new FakeScenarioOperationnelRepository();
        var chemins = new FakeCheminAttaqueRepository();
        var scenarios = new FakeScenarioStrategiqueRepository();
        var couples = new FakeCoupleSourceRisqueObjectifViseRepository();
        var biens = new FakeBienSupportRepository();
        var snapshots = new FakeSnapshotAtelierRepository();
        var service = new ServiceCreationSnapshotAtelier4(etudes, scenariosOp, chemins, scenarios, couples, biens, snapshots);
        return (service, etudes, scenariosOp, chemins, scenarios, couples, biens, snapshots);
    }

    private static Etude CreerEtudeAvecAtelier4Valide()
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
        return etude;
    }

    [Fact]
    public async Task CreerAsync_refuse_si_atelier_4_non_valide()
    {
        var (service, etudes, _, _, _, _, _, _) = CreerService();
        var etude = Etude.Creer("Etude test", "Perimetre", "Mission");
        etudes.Etudes.Add(etude);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreerAsync(etude.Id, CancellationToken.None));
    }

    [Fact]
    public async Task CreerAsync_fige_les_scenarios_operationnels_et_actions_elementaires()
    {
        var (service, etudes, scenariosOp, chemins, scenarios, couples, biens, _) = CreerService();
        var etude = CreerEtudeAvecAtelier4Valide();
        etudes.Etudes.Add(etude);

        var bien = BienSupport.Creer(etude.Id, Guid.NewGuid(), "Serveur R&D", TypeBienSupport.SystemeInformation, "DSI");
        biens.Items.Add(bien);

        var couple = CoupleSourceRisqueObjectifVise.Creer(
            etude.Id, CategorieSourceRisque.Etatique, "Description SR", CategorieObjectifVise.Lucratif, "Description OV",
            "Contexte", "Technologique", 4, 4, ServiceCalculPertinence.Calculer(4, 4));
        couples.Items.Add(couple);

        var scenario = ScenarioStrategique.Creer(etude.Id, couple.Id, Guid.NewGuid(), "Compromission");
        scenarios.Items.Add(scenario);

        var chemin = CheminAttaque.Creer(etude.Id, scenario.Id, "Intrusion frontale");
        chemins.Items.Add(chemin);

        var scenarioOp = ScenarioOperationnel.Creer(etude.Id, chemin.Id);
        var actions = new[] { new ActionElementaireEntree("Exploitation d'une CVE", PhaseActionElementaire.Exploiter, bien.Id) };
        scenarioOp.AjouterModeOperatoire("Mode test", actions, probabiliteSucces: 3, difficulteTechnique: 2);
        scenariosOp.Items.Add(scenarioOp);

        var snapshot = await service.CreerAsync(etude.Id, CancellationToken.None);

        var contenu = JsonSerializer.Deserialize<SnapshotAtelier4Content>(snapshot.ContenuJson);
        Assert.NotNull(contenu);
        var scenarioSnapshot = Assert.Single(contenu!.ScenariosOperationnels);
        var modeSnapshot = Assert.Single(scenarioSnapshot.ModesOperatoires);
        var actionSnapshot = Assert.Single(modeSnapshot.ActionsElementaires);
        Assert.Equal(bien.Id, actionSnapshot.BienSupportId);
        Assert.Equal(PhaseActionElementaire.Exploiter, actionSnapshot.Phase);
        Assert.Equal(NiveauVraisemblance.V3, modeSnapshot.Vraisemblance);
    }
}
