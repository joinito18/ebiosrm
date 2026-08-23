using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;
using EbiosRM.Api.Tests.TestDoubles;

namespace EbiosRM.Api.Tests.Domain.ScenariosDeRisque;

public class ServiceValidationCompletudeAtelier3Tests
{
    private static readonly Guid EtudeId = Guid.NewGuid();

    [Fact]
    public async Task VerifierAsync_incomplet_si_aucun_scenario_strategique()
    {
        var scenarios = new FakeScenarioStrategiqueRepository();
        var service = new ServiceValidationCompletudeAtelier3(scenarios);

        var resultat = await service.VerifierAsync(EtudeId, CancellationToken.None);

        Assert.False(resultat.EstComplet);
        Assert.Single(resultat.ElementsManquants);
    }

    [Fact]
    public async Task VerifierAsync_complet_des_qu_un_scenario_strategique_existe()
    {
        var scenarios = new FakeScenarioStrategiqueRepository();
        var service = new ServiceValidationCompletudeAtelier3(scenarios);
        scenarios.Items.Add(ScenarioStrategique.Creer(EtudeId, Guid.NewGuid(), Guid.NewGuid(), "Description"));

        var resultat = await service.VerifierAsync(EtudeId, CancellationToken.None);

        Assert.True(resultat.EstComplet);
    }
}
