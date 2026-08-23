using EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;
using EbiosRM.Api.Tests.TestDoubles;

namespace EbiosRM.Api.Tests.Domain.SourcesRisque;

public class ServiceValidationCompletudeAtelier2Tests
{
    private static readonly Guid EtudeId = Guid.NewGuid();

    [Fact]
    public async Task VerifierAsync_incomplet_si_aucun_couple()
    {
        var couples = new FakeCoupleSourceRisqueObjectifViseRepository();
        var service = new ServiceValidationCompletudeAtelier2(couples);

        var resultat = await service.VerifierAsync(EtudeId, CancellationToken.None);

        Assert.False(resultat.EstComplet);
        Assert.Single(resultat.ElementsManquants);
    }

    [Fact]
    public async Task VerifierAsync_complet_des_qu_un_couple_existe()
    {
        var couples = new FakeCoupleSourceRisqueObjectifViseRepository();
        var service = new ServiceValidationCompletudeAtelier2(couples);
        var pertinence = ServiceCalculPertinence.Calculer(2, 2);
        couples.Items.Add(CoupleSourceRisqueObjectifVise.Creer(
            EtudeId, CategorieSourceRisque.Amateur, "Description SR", CategorieObjectifVise.DefiAmusement, "Description OV",
            "Contexte", "Technologique", 2, 2, pertinence));

        var resultat = await service.VerifierAsync(EtudeId, CancellationToken.None);

        Assert.True(resultat.EstComplet);
        Assert.Empty(resultat.ElementsManquants);
    }
}
