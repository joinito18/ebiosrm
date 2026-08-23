using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

namespace EbiosRM.Api.Tests.Domain.ScenariosDeRisque;

public class ScenarioOperationnelTests
{
    private static readonly Guid EtudeId = Guid.NewGuid();
    private static readonly Guid CheminAttaqueId = Guid.NewGuid();

    [Fact]
    public void AjouterModeOperatoire_refuse_une_liste_d_actions_vide()
    {
        var scenario = ScenarioOperationnel.Creer(EtudeId, CheminAttaqueId);

        Assert.Throws<ArgumentException>(() =>
            scenario.AjouterModeOperatoire("Mode test", Array.Empty<ActionElementaireEntree>(), probabiliteSucces: 2, difficulteTechnique: 2));
    }

    [Fact]
    public void AjouterModeOperatoire_accepte_une_action_elementaire_et_l_expose()
    {
        var scenario = ScenarioOperationnel.Creer(EtudeId, CheminAttaqueId);
        var bienSupportId = Guid.NewGuid();
        var actions = new[] { new ActionElementaireEntree("Reconnaissance externe", PhaseActionElementaire.Connaitre, bienSupportId) };

        scenario.AjouterModeOperatoire("Mode test", actions, probabiliteSucces: 2, difficulteTechnique: 2);

        var mode = Assert.Single(scenario.ModesOperatoires);
        var action = Assert.Single(mode.ActionsElementaires);
        Assert.Equal(PhaseActionElementaire.Connaitre, action.Phase);
        Assert.Equal(bienSupportId, action.BienSupportId);
    }

    private static ScenarioOperationnel CreerScenarioAvecMode(int probabiliteSucces, int difficulteTechnique)
    {
        var scenario = ScenarioOperationnel.Creer(EtudeId, CheminAttaqueId);
        var actions = new[] { new ActionElementaireEntree("Action", PhaseActionElementaire.Connaitre, Guid.NewGuid()) };
        scenario.AjouterModeOperatoire("Mode test", actions, probabiliteSucces, difficulteTechnique);
        return scenario;
    }

    [Fact]
    public void DefinirVraisemblanceRetenueModeOperatoire_devient_la_valeur_effective_sans_effacer_la_calculee()
    {
        var scenario = CreerScenarioAvecMode(probabiliteSucces: 1, difficulteTechnique: 4);
        var mode = scenario.ModesOperatoires[0];
        var calculeeInitiale = mode.VraisemblanceCalculee;

        scenario.DefinirVraisemblanceRetenueModeOperatoire(mode.Id, NiveauVraisemblance.V4, "Contexte non capture par la grille.");

        Assert.Equal(NiveauVraisemblance.V4, mode.Vraisemblance);
        Assert.Equal(calculeeInitiale, mode.VraisemblanceCalculee);
        Assert.Equal(NiveauVraisemblance.V4, scenario.VraisemblanceGlobale);
    }

    [Fact]
    public void ReinitialiserVraisemblanceModeOperatoire_revient_a_la_valeur_calculee()
    {
        var scenario = CreerScenarioAvecMode(probabiliteSucces: 1, difficulteTechnique: 4);
        var mode = scenario.ModesOperatoires[0];
        var calculeeInitiale = mode.VraisemblanceCalculee;
        scenario.DefinirVraisemblanceRetenueModeOperatoire(mode.Id, NiveauVraisemblance.V4, "Justification.");

        scenario.ReinitialiserVraisemblanceModeOperatoire(mode.Id);

        Assert.Equal(calculeeInitiale, mode.Vraisemblance);
        Assert.Null(mode.VraisemblanceRetenue);
        Assert.Null(mode.JustificationVraisemblance);
    }
}
