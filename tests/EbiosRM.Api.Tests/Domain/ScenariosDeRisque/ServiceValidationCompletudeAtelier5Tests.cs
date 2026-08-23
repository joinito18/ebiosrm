using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;
using EbiosRM.Api.Tests.TestDoubles;

namespace EbiosRM.Api.Tests.Domain.ScenariosDeRisque;

public class ServiceValidationCompletudeAtelier5Tests
{
    private static readonly Guid EtudeId = Guid.NewGuid();

    [Fact]
    public async Task VerifierAsync_incomplet_si_aucun_scenario_de_risque()
    {
        var scenarios = new FakeScenarioDeRisqueRepository();
        var service = new ServiceValidationCompletudeAtelier5(scenarios);

        var resultat = await service.VerifierAsync(EtudeId, CancellationToken.None);

        Assert.False(resultat.EstComplet);
        Assert.Single(resultat.ElementsManquants);
    }

    [Fact]
    public async Task VerifierAsync_incomplet_si_risque_residuel_non_evalue()
    {
        var scenarios = new FakeScenarioDeRisqueRepository();
        var service = new ServiceValidationCompletudeAtelier5(scenarios);
        scenarios.Items.Add(ScenarioDeRisque.Creer(EtudeId, Guid.NewGuid()));

        var resultat = await service.VerifierAsync(EtudeId, CancellationToken.None);

        Assert.False(resultat.EstComplet);
        Assert.Contains("résiduel", resultat.ElementsManquants[0]);
    }

    [Fact]
    public async Task VerifierAsync_complet_des_que_le_risque_residuel_est_evalue()
    {
        var scenarios = new FakeScenarioDeRisqueRepository();
        var service = new ServiceValidationCompletudeAtelier5(scenarios);
        var scenario = ScenarioDeRisque.Creer(EtudeId, Guid.NewGuid());
        scenario.EvaluerRisqueResiduel(1, NiveauVraisemblance.V1, NiveauRisque.Faible);
        scenarios.Items.Add(scenario);

        var resultat = await service.VerifierAsync(EtudeId, CancellationToken.None);

        Assert.True(resultat.EstComplet);
    }
}
