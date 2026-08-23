using EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;

namespace EbiosRM.Api.Tests.Domain.SourcesRisque;

public class ServiceCalculNiveauDangerositeTests
{
    // Formule officielle : (Dependance x Penetration) / (Maturite cyber x Confiance).
    [Theory]
    [InlineData(1, 1, 1, 1, 1.0)]
    [InlineData(4, 4, 1, 1, 16.0)]
    [InlineData(1, 1, 4, 4, 0.06)]
    [InlineData(4, 3, 2, 2, 3.0)]
    [InlineData(3, 2, 2, 2, 1.5)]
    [InlineData(3, 3, 1, 2, 4.5)]
    public void Calculer_respecte_la_formule_officielle(int dependance, int penetration, int maturiteCyber, int confiance, double attendu)
    {
        var resultat = ServiceCalculNiveauDangerosite.Calculer(dependance, penetration, maturiteCyber, confiance);

        Assert.Equal(attendu, resultat, precision: 2);
    }

    [Theory]
    [InlineData(0, 2, 2, 2)]
    [InlineData(5, 2, 2, 2)]
    [InlineData(2, 0, 2, 2)]
    [InlineData(2, 5, 2, 2)]
    [InlineData(2, 2, 0, 2)]
    [InlineData(2, 2, 5, 2)]
    [InlineData(2, 2, 2, 0)]
    [InlineData(2, 2, 2, 5)]
    public void Calculer_refuse_toute_valeur_hors_echelle_1_a_4(int dependance, int penetration, int maturiteCyber, int confiance)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ServiceCalculNiveauDangerosite.Calculer(dependance, penetration, maturiteCyber, confiance));
    }

    [Theory]
    [InlineData(0.06, ZoneDangerosite.Veille)]
    [InlineData(0.99, ZoneDangerosite.Veille)]
    [InlineData(1.0, ZoneDangerosite.Controle)]
    [InlineData(3.99, ZoneDangerosite.Controle)]
    [InlineData(4.0, ZoneDangerosite.Danger)]
    [InlineData(16.0, ZoneDangerosite.Danger)]
    public void DeterminerZone_respecte_les_seuils(double niveau, ZoneDangerosite attendu)
    {
        var zone = ServiceCalculNiveauDangerosite.DeterminerZone(niveau);

        Assert.Equal(attendu, zone);
    }
}
