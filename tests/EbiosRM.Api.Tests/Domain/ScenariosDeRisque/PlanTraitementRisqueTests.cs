using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

namespace EbiosRM.Api.Tests.Domain.ScenariosDeRisque;

public class PlanTraitementRisqueTests
{
    private static readonly Guid EtudeId = Guid.NewGuid();
    private static readonly Guid ScenarioId = Guid.NewGuid();

    [Fact]
    public void Creer_refuse_une_etude_non_rattachee()
    {
        Assert.Throws<ArgumentException>(() => PlanTraitementRisque.Creer(Guid.Empty));
    }

    [Fact]
    public void AjouterMesure_refuse_sans_scenario_de_risque_associe()
    {
        var plan = PlanTraitementRisque.Creer(EtudeId);

        Assert.Throws<ArgumentException>(() => plan.AjouterMesure(
            "Chiffrement des postes", AxeMesure.Protection, new List<Guid>(),
            "RSSI", null, NiveauCoutComplexite.Plus, "6 mois", StatutMesure.ALancer));
    }

    [Fact]
    public void AjouterMesure_avec_donnees_valides_reussit()
    {
        var plan = PlanTraitementRisque.Creer(EtudeId);

        plan.AjouterMesure(
            "Chiffrement des postes", AxeMesure.Protection, new List<Guid> { ScenarioId },
            "RSSI", "Budget limité", NiveauCoutComplexite.PlusPlus, "6 mois", StatutMesure.ALancer);

        Assert.Single(plan.Mesures);
        Assert.Equal("Chiffrement des postes", plan.Mesures[0].Description);
        Assert.Equal(AxeMesure.Protection, plan.Mesures[0].Axe);
        Assert.Contains(ScenarioId, plan.Mesures[0].ScenariosDeRisqueIds);
        Assert.Equal("++", plan.Mesures[0].CoutComplexite.Libelle());
    }

    [Fact]
    public void ModifierMesure_sur_id_inexistant_leve_une_erreur()
    {
        var plan = PlanTraitementRisque.Creer(EtudeId);

        Assert.Throws<ArgumentException>(() => plan.ModifierMesure(
            Guid.NewGuid(), "Description", AxeMesure.Gouvernance, new List<Guid> { ScenarioId },
            "RSSI", null, NiveauCoutComplexite.Plus, null, StatutMesure.ALancer));
    }

    [Fact]
    public void ModifierMesure_remplace_la_liste_des_scenarios()
    {
        var plan = PlanTraitementRisque.Creer(EtudeId);
        plan.AjouterMesure("Mesure", AxeMesure.Defense, new List<Guid> { ScenarioId }, "RSSI", null, NiveauCoutComplexite.Plus, null, StatutMesure.ALancer);
        var autreScenario = Guid.NewGuid();
        var mesureId = plan.Mesures[0].Id;

        plan.ModifierMesure(mesureId, "Mesure modifiée", AxeMesure.Resilience, new List<Guid> { autreScenario }, "DSI", "Freins", NiveauCoutComplexite.PlusPlusPlus, "12 mois", StatutMesure.EnCours);

        var mesure = plan.Mesures[0];
        Assert.Equal("Mesure modifiée", mesure.Description);
        Assert.Equal(AxeMesure.Resilience, mesure.Axe);
        Assert.DoesNotContain(ScenarioId, mesure.ScenariosDeRisqueIds);
        Assert.Contains(autreScenario, mesure.ScenariosDeRisqueIds);
        Assert.Equal(StatutMesure.EnCours, mesure.Statut);
    }

    [Fact]
    public void SupprimerMesure_sur_id_inexistant_leve_une_erreur()
    {
        var plan = PlanTraitementRisque.Creer(EtudeId);

        Assert.Throws<ArgumentException>(() => plan.SupprimerMesure(Guid.NewGuid()));
    }

    [Fact]
    public void SupprimerMesure_retire_de_la_liste()
    {
        var plan = PlanTraitementRisque.Creer(EtudeId);
        plan.AjouterMesure("Mesure", AxeMesure.Gouvernance, new List<Guid> { ScenarioId }, "RSSI", null, NiveauCoutComplexite.Plus, null, StatutMesure.ALancer);
        var mesureId = plan.Mesures[0].Id;

        plan.SupprimerMesure(mesureId);

        Assert.Empty(plan.Mesures);
    }

    [Fact]
    public void RetirerReferenceScenario_retire_la_reference_sans_supprimer_la_mesure()
    {
        var plan = PlanTraitementRisque.Creer(EtudeId);
        var autreScenario = Guid.NewGuid();
        plan.AjouterMesure("Mesure", AxeMesure.Gouvernance, new List<Guid> { ScenarioId, autreScenario }, "RSSI", null, NiveauCoutComplexite.Plus, null, StatutMesure.ALancer);

        plan.RetirerReferenceScenario(ScenarioId);

        Assert.Single(plan.Mesures);
        Assert.DoesNotContain(ScenarioId, plan.Mesures[0].ScenariosDeRisqueIds);
        Assert.Contains(autreScenario, plan.Mesures[0].ScenariosDeRisqueIds);
    }
}
