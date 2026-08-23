using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;
using EbiosRM.Api.Tests.TestDoubles;

namespace EbiosRM.Api.Tests.Domain.ScenariosDeRisque;

public class ServiceValidationCompletudeAtelier4Tests
{
    private static readonly Guid EtudeId = Guid.NewGuid();

    [Fact]
    public async Task VerifierAsync_incomplet_si_aucun_scenario_operationnel()
    {
        var scenarios = new FakeScenarioOperationnelRepository();
        var service = new ServiceValidationCompletudeAtelier4(scenarios);

        var resultat = await service.VerifierAsync(EtudeId, CancellationToken.None);

        Assert.False(resultat.EstComplet);
        Assert.Single(resultat.ElementsManquants);
    }

    [Fact]
    public async Task VerifierAsync_incomplet_si_scenario_operationnel_sans_mode_operatoire()
    {
        var scenarios = new FakeScenarioOperationnelRepository();
        var service = new ServiceValidationCompletudeAtelier4(scenarios);
        scenarios.Items.Add(ScenarioOperationnel.Creer(EtudeId, Guid.NewGuid()));

        var resultat = await service.VerifierAsync(EtudeId, CancellationToken.None);

        Assert.False(resultat.EstComplet);
        Assert.Contains("mode opératoire", resultat.ElementsManquants[0]);
    }

    [Fact]
    public async Task VerifierAsync_complet_des_qu_un_mode_operatoire_existe()
    {
        var scenarios = new FakeScenarioOperationnelRepository();
        var service = new ServiceValidationCompletudeAtelier4(scenarios);
        var scenario = ScenarioOperationnel.Creer(EtudeId, Guid.NewGuid());
        var actions = new[] { new ActionElementaireEntree("Action", PhaseActionElementaire.Connaitre, Guid.NewGuid()) };
        scenario.AjouterModeOperatoire("Mode", actions, 2, 2);
        scenarios.Items.Add(scenario);

        var resultat = await service.VerifierAsync(EtudeId, CancellationToken.None);

        Assert.True(resultat.EstComplet);
    }
}
