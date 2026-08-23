using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

namespace EbiosRM.Api.Tests.Domain.ScenariosDeRisque;

public class ScenarioStrategiqueTests
{
    private static readonly Guid EtudeId = Guid.NewGuid();
    private static readonly Guid CoupleId = Guid.NewGuid();
    private static readonly Guid EvenementRedouteId = Guid.NewGuid();

    [Fact]
    public void Creer_avec_donnees_valides_reussit()
    {
        var scenario = ScenarioStrategique.Creer(EtudeId, CoupleId, EvenementRedouteId, "Exfiltration de donnees de R&D");

        Assert.Equal(EtudeId, scenario.EtudeId);
        Assert.Equal(CoupleId, scenario.CoupleSourceRisqueObjectifViseId);
        Assert.Equal(EvenementRedouteId, scenario.EvenementRedouteId);
        Assert.Equal("Exfiltration de donnees de R&D", scenario.Description);
    }

    [Fact]
    public void Creer_refuse_une_etude_non_rattachee()
    {
        Assert.Throws<ArgumentException>(
            () => ScenarioStrategique.Creer(Guid.Empty, CoupleId, EvenementRedouteId, "Description"));
    }

    [Fact]
    public void Creer_refuse_sans_couple_source_risque_objectif_vise()
    {
        Assert.Throws<ArgumentException>(
            () => ScenarioStrategique.Creer(EtudeId, Guid.Empty, EvenementRedouteId, "Description"));
    }

    [Fact]
    public void Creer_refuse_sans_evenement_redoute_cible()
    {
        Assert.Throws<ArgumentException>(
            () => ScenarioStrategique.Creer(EtudeId, CoupleId, Guid.Empty, "Description"));
    }

    [Fact]
    public void Creer_refuse_une_description_vide()
    {
        Assert.Throws<ArgumentException>(
            () => ScenarioStrategique.Creer(EtudeId, CoupleId, EvenementRedouteId, ""));
    }

    [Fact]
    public void Modifier_met_a_jour_evenement_redoute_et_description()
    {
        var scenario = ScenarioStrategique.Creer(EtudeId, CoupleId, EvenementRedouteId, "Description initiale");
        var nouvelEvenement = Guid.NewGuid();

        scenario.Modifier(nouvelEvenement, "Nouvelle description");

        Assert.Equal(nouvelEvenement, scenario.EvenementRedouteId);
        Assert.Equal("Nouvelle description", scenario.Description);
    }

    [Fact]
    public void Modifier_ne_change_jamais_le_couple_rattache()
    {
        var scenario = ScenarioStrategique.Creer(EtudeId, CoupleId, EvenementRedouteId, "Description");

        scenario.Modifier(Guid.NewGuid(), "Autre description");

        Assert.Equal(CoupleId, scenario.CoupleSourceRisqueObjectifViseId);
    }

    [Fact]
    public void Modifier_refuse_sans_evenement_redoute()
    {
        var scenario = ScenarioStrategique.Creer(EtudeId, CoupleId, EvenementRedouteId, "Description");

        Assert.Throws<ArgumentException>(() => scenario.Modifier(Guid.Empty, "Description"));
    }
}
