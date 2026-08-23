using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

namespace EbiosRM.Api.Tests.Domain.ScenariosDeRisque;

public class ScenarioDeRisqueTests
{
    private static readonly Guid EtudeId = Guid.NewGuid();
    private static readonly Guid CheminAttaqueId = Guid.NewGuid();

    [Fact]
    public void Creer_avec_donnees_valides_reussit()
    {
        var scenario = ScenarioDeRisque.Creer(EtudeId, CheminAttaqueId);

        Assert.Equal(EtudeId, scenario.EtudeId);
        Assert.Equal(CheminAttaqueId, scenario.CheminAttaqueId);
        Assert.Null(scenario.NiveauRisqueResiduel);
        Assert.False(scenario.AccepteParDirection);
    }

    [Fact]
    public void Creer_refuse_une_etude_non_rattachee()
    {
        Assert.Throws<ArgumentException>(() => ScenarioDeRisque.Creer(Guid.Empty, CheminAttaqueId));
    }

    [Fact]
    public void Creer_refuse_sans_chemin_attaque_rattache()
    {
        Assert.Throws<ArgumentException>(() => ScenarioDeRisque.Creer(EtudeId, Guid.Empty));
    }

    [Fact]
    public void DefinirNiveauRisqueInitialRetenu_refuse_sans_justification()
    {
        var scenario = ScenarioDeRisque.Creer(EtudeId, CheminAttaqueId);

        Assert.Throws<ArgumentException>(() => scenario.DefinirNiveauRisqueInitialRetenu(NiveauRisque.Eleve, ""));
    }

    [Fact]
    public void DefinirNiveauRisqueInitialRetenu_puis_ReinitialiserNiveauRisqueInitial_effectue_un_aller_retour()
    {
        var scenario = ScenarioDeRisque.Creer(EtudeId, CheminAttaqueId);

        scenario.DefinirNiveauRisqueInitialRetenu(NiveauRisque.Eleve, "Contexte non capturé par la formule.");
        Assert.Equal(NiveauRisque.Eleve, scenario.NiveauRisqueInitialRetenu);
        Assert.Equal("Contexte non capturé par la formule.", scenario.JustificationNiveauRisqueInitial);

        scenario.ReinitialiserNiveauRisqueInitial();
        Assert.Null(scenario.NiveauRisqueInitialRetenu);
        Assert.Null(scenario.JustificationNiveauRisqueInitial);
    }

    [Fact]
    public void EvaluerRisqueResiduel_refuse_une_gravite_hors_echelle()
    {
        var scenario = ScenarioDeRisque.Creer(EtudeId, CheminAttaqueId);

        Assert.Throws<ArgumentOutOfRangeException>(() => scenario.EvaluerRisqueResiduel(5, NiveauVraisemblance.V2, NiveauRisque.Moyen));
    }

    [Fact]
    public void EvaluerRisqueResiduel_enregistre_les_entrees_et_le_calcul()
    {
        var scenario = ScenarioDeRisque.Creer(EtudeId, CheminAttaqueId);

        scenario.EvaluerRisqueResiduel(2, NiveauVraisemblance.V2, NiveauRisque.Faible);

        Assert.Equal(2, scenario.GraviteResiduelle);
        Assert.Equal(NiveauVraisemblance.V2, scenario.VraisemblanceResiduelle);
        Assert.Equal(NiveauRisque.Faible, scenario.NiveauRisqueResiduelCalcule);
        Assert.Equal(NiveauRisque.Faible, scenario.NiveauRisqueResiduel);
        Assert.Equal(ClasseAcceptation.AcceptableEnLEtat, scenario.ClasseAcceptationResiduelle);
    }

    [Fact]
    public void EvaluerRisqueResiduel_ne_touche_pas_a_l_override_deja_enregistre()
    {
        var scenario = ScenarioDeRisque.Creer(EtudeId, CheminAttaqueId);
        scenario.EvaluerRisqueResiduel(2, NiveauVraisemblance.V2, NiveauRisque.Faible);
        scenario.DefinirNiveauRisqueResiduelRetenu(NiveauRisque.Moyen, "Jugement d'expert.");

        scenario.EvaluerRisqueResiduel(1, NiveauVraisemblance.V1, NiveauRisque.Faible);

        Assert.Equal(NiveauRisque.Moyen, scenario.NiveauRisqueResiduelRetenu);
        Assert.Equal(NiveauRisque.Moyen, scenario.NiveauRisqueResiduel);
    }

    [Fact]
    public void DefinirNiveauRisqueResiduelRetenu_puis_ReinitialiserNiveauRisqueResiduel_effectue_un_aller_retour()
    {
        var scenario = ScenarioDeRisque.Creer(EtudeId, CheminAttaqueId);
        scenario.EvaluerRisqueResiduel(2, NiveauVraisemblance.V2, NiveauRisque.Faible);

        scenario.DefinirNiveauRisqueResiduelRetenu(NiveauRisque.Eleve, "Contexte aggravant.");
        Assert.Equal(NiveauRisque.Eleve, scenario.NiveauRisqueResiduel);

        scenario.ReinitialiserNiveauRisqueResiduel();
        Assert.Equal(NiveauRisque.Faible, scenario.NiveauRisqueResiduel);
    }

    [Fact]
    public void AccepterRisqueResiduel_refuse_sans_risque_residuel_evalue()
    {
        var scenario = ScenarioDeRisque.Creer(EtudeId, CheminAttaqueId);

        Assert.Throws<InvalidOperationException>(() => scenario.AccepterRisqueResiduel("Direction", "RSSI", null, null));
    }

    [Fact]
    public void AccepterRisqueResiduel_cas_nominal_faible_sans_sponsor_ni_justification()
    {
        var scenario = ScenarioDeRisque.Creer(EtudeId, CheminAttaqueId);
        scenario.EvaluerRisqueResiduel(1, NiveauVraisemblance.V1, NiveauRisque.Faible);

        scenario.AccepterRisqueResiduel("Direction générale", "RSSI", null, null);

        Assert.True(scenario.AccepteParDirection);
        Assert.Equal("Direction générale", scenario.NomProprietaireRisque);
        Assert.Equal("RSSI", scenario.NomValidateurSecurite);
        Assert.Null(scenario.NomSponsorExecutif);
        Assert.NotNull(scenario.DateAcceptationUtc);
    }

    [Fact]
    public void AccepterRisqueResiduel_refuse_un_risque_eleve_sans_sponsor_ni_justification()
    {
        var scenario = ScenarioDeRisque.Creer(EtudeId, CheminAttaqueId);
        scenario.EvaluerRisqueResiduel(4, NiveauVraisemblance.V4, NiveauRisque.Eleve);

        Assert.Throws<ArgumentException>(() => scenario.AccepterRisqueResiduel("Direction générale", "RSSI", null, null));
    }

    [Fact]
    public void AccepterRisqueResiduel_accepte_un_risque_eleve_avec_sponsor_et_justification()
    {
        var scenario = ScenarioDeRisque.Creer(EtudeId, CheminAttaqueId);
        scenario.EvaluerRisqueResiduel(4, NiveauVraisemblance.V4, NiveauRisque.Eleve);

        scenario.AccepterRisqueResiduel("Direction générale", "RSSI", "PDG", "Risque maintenu élevé, surveillance renforcée.");

        Assert.True(scenario.AccepteParDirection);
        Assert.Equal("PDG", scenario.NomSponsorExecutif);
        Assert.Equal("Risque maintenu élevé, surveillance renforcée.", scenario.JustificationAcceptation);
    }

    [Fact]
    public void RetirerAcceptation_efface_tous_les_champs_d_acceptation()
    {
        var scenario = ScenarioDeRisque.Creer(EtudeId, CheminAttaqueId);
        scenario.EvaluerRisqueResiduel(1, NiveauVraisemblance.V1, NiveauRisque.Faible);
        scenario.AccepterRisqueResiduel("Direction générale", "RSSI", null, null);

        scenario.RetirerAcceptation();

        Assert.False(scenario.AccepteParDirection);
        Assert.Null(scenario.NomProprietaireRisque);
        Assert.Null(scenario.NomValidateurSecurite);
        Assert.Null(scenario.DateAcceptationUtc);
    }
}
