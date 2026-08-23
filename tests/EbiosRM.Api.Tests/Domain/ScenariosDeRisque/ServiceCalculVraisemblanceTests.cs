using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

namespace EbiosRM.Api.Tests.Domain.ScenariosDeRisque;

public class ServiceCalculVraisemblanceTests
{
    // Couvre la grille officielle Probabilite de succes x Difficulte technique
    // en entier (4x4 = 16 combinaisons) -- seule autorite pour deriver la
    // Vraisemblance, meme principe que ServiceCalculPertinenceTests.
    [Theory]
    [InlineData(1, 1, NiveauVraisemblance.V2)]
    [InlineData(1, 2, NiveauVraisemblance.V2)]
    [InlineData(1, 3, NiveauVraisemblance.V1)]
    [InlineData(1, 4, NiveauVraisemblance.V1)]
    [InlineData(2, 1, NiveauVraisemblance.V3)]
    [InlineData(2, 2, NiveauVraisemblance.V2)]
    [InlineData(2, 3, NiveauVraisemblance.V2)]
    [InlineData(2, 4, NiveauVraisemblance.V1)]
    [InlineData(3, 1, NiveauVraisemblance.V3)]
    [InlineData(3, 2, NiveauVraisemblance.V3)]
    [InlineData(3, 3, NiveauVraisemblance.V2)]
    [InlineData(3, 4, NiveauVraisemblance.V2)]
    [InlineData(4, 1, NiveauVraisemblance.V4)]
    [InlineData(4, 2, NiveauVraisemblance.V3)]
    [InlineData(4, 3, NiveauVraisemblance.V3)]
    [InlineData(4, 4, NiveauVraisemblance.V2)]
    public void Calculer_respecte_la_grille_officielle(int probabiliteSucces, int difficulteTechnique, NiveauVraisemblance attendu)
    {
        var resultat = ServiceCalculVraisemblance.Calculer(probabiliteSucces, difficulteTechnique);

        Assert.Equal(attendu, resultat);
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(5, 2)]
    public void Calculer_refuse_une_probabilite_hors_echelle(int probabiliteSucces, int difficulteTechnique)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ServiceCalculVraisemblance.Calculer(probabiliteSucces, difficulteTechnique));
    }

    [Theory]
    [InlineData(2, 0)]
    [InlineData(2, 5)]
    public void Calculer_refuse_une_difficulte_hors_echelle(int probabiliteSucces, int difficulteTechnique)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ServiceCalculVraisemblance.Calculer(probabiliteSucces, difficulteTechnique));
    }
}
