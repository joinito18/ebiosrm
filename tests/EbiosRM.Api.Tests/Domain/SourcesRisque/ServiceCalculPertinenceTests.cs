using EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;

namespace EbiosRM.Api.Tests.Domain.SourcesRisque;

public class ServiceCalculPertinenceTests
{
    // Couvre la matrice officielle Motivation x Ressources en entier (4x4 = 16
    // combinaisons) -- ce service est la seule autorité pour dériver la
    // Pertinence, donc chaque case de sa matrice mérite un test explicite.
    [Theory]
    [InlineData(1, 1, NiveauPertinence.PeuPertinent)]
    [InlineData(1, 2, NiveauPertinence.PeuPertinent)]
    [InlineData(1, 3, NiveauPertinence.MoyennementPertinent)]
    [InlineData(1, 4, NiveauPertinence.MoyennementPertinent)]
    [InlineData(2, 1, NiveauPertinence.PeuPertinent)]
    [InlineData(2, 2, NiveauPertinence.MoyennementPertinent)]
    [InlineData(2, 3, NiveauPertinence.PlutotPertinent)]
    [InlineData(2, 4, NiveauPertinence.PlutotPertinent)]
    [InlineData(3, 1, NiveauPertinence.MoyennementPertinent)]
    [InlineData(3, 2, NiveauPertinence.PlutotPertinent)]
    [InlineData(3, 3, NiveauPertinence.PlutotPertinent)]
    [InlineData(3, 4, NiveauPertinence.TresPertinent)]
    [InlineData(4, 1, NiveauPertinence.MoyennementPertinent)]
    [InlineData(4, 2, NiveauPertinence.PlutotPertinent)]
    [InlineData(4, 3, NiveauPertinence.TresPertinent)]
    [InlineData(4, 4, NiveauPertinence.TresPertinent)]
    public void Calculer_respecte_la_matrice_officielle(int motivation, int ressources, NiveauPertinence attendu)
    {
        var resultat = ServiceCalculPertinence.Calculer(motivation, ressources);

        Assert.Equal(attendu, resultat);
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(5, 2)]
    public void Calculer_refuse_une_motivation_hors_echelle(int motivation, int ressources)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ServiceCalculPertinence.Calculer(motivation, ressources));
    }

    [Theory]
    [InlineData(2, 0)]
    [InlineData(2, 5)]
    public void Calculer_refuse_des_ressources_hors_echelle(int motivation, int ressources)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ServiceCalculPertinence.Calculer(motivation, ressources));
    }
}
