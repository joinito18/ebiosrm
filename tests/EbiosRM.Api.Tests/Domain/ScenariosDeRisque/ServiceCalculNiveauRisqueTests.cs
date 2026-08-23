using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

namespace EbiosRM.Api.Tests.Domain.ScenariosDeRisque;

public class ServiceCalculNiveauRisqueTests
{
    [Theory]
    [InlineData(1, NiveauVraisemblance.V1, NiveauRisque.Faible)]
    [InlineData(1, NiveauVraisemblance.V2, NiveauRisque.Faible)]
    [InlineData(1, NiveauVraisemblance.V3, NiveauRisque.Moyen)]
    [InlineData(1, NiveauVraisemblance.V4, NiveauRisque.Moyen)]
    [InlineData(2, NiveauVraisemblance.V1, NiveauRisque.Faible)]
    [InlineData(2, NiveauVraisemblance.V2, NiveauRisque.Faible)]
    [InlineData(2, NiveauVraisemblance.V3, NiveauRisque.Moyen)]
    [InlineData(2, NiveauVraisemblance.V4, NiveauRisque.Eleve)]
    [InlineData(3, NiveauVraisemblance.V1, NiveauRisque.Faible)]
    [InlineData(3, NiveauVraisemblance.V2, NiveauRisque.Moyen)]
    [InlineData(3, NiveauVraisemblance.V3, NiveauRisque.Eleve)]
    [InlineData(3, NiveauVraisemblance.V4, NiveauRisque.Eleve)]
    [InlineData(4, NiveauVraisemblance.V1, NiveauRisque.Faible)]
    [InlineData(4, NiveauVraisemblance.V2, NiveauRisque.Moyen)]
    [InlineData(4, NiveauVraisemblance.V3, NiveauRisque.Eleve)]
    [InlineData(4, NiveauVraisemblance.V4, NiveauRisque.Eleve)]
    public void Calculer_respecte_la_grille_officielle(int gravite, NiveauVraisemblance vraisemblance, NiveauRisque attendu)
    {
        Assert.Equal(attendu, ServiceCalculNiveauRisque.Calculer(gravite, vraisemblance));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void Calculer_refuse_une_gravite_hors_echelle(int gravite)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ServiceCalculNiveauRisque.Calculer(gravite, NiveauVraisemblance.V1));
    }

    [Theory]
    [InlineData(NiveauRisque.Faible, ClasseAcceptation.AcceptableEnLEtat)]
    [InlineData(NiveauRisque.Moyen, ClasseAcceptation.TolerableSousControle)]
    [InlineData(NiveauRisque.Eleve, ClasseAcceptation.Inacceptable)]
    public void DeterminerClasseAcceptation_respecte_la_correspondance_officielle(NiveauRisque niveau, ClasseAcceptation attendu)
    {
        Assert.Equal(attendu, ServiceCalculNiveauRisque.DeterminerClasseAcceptation(niveau));
    }
}
