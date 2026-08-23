using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

namespace EbiosRM.Api.Tests.Domain.ScenariosDeRisque;

public class CheminAttaqueTests
{
    private static readonly Guid EtudeId = Guid.NewGuid();
    private static readonly Guid ScenarioStrategiqueId = Guid.NewGuid();
    private static readonly Guid PartiePrenanteId = Guid.NewGuid();

    [Fact]
    public void Creer_avec_donnees_valides_reussit()
    {
        var chemin = CheminAttaque.Creer(EtudeId, ScenarioStrategiqueId, "Intrusion frontale sur le SI R&D");

        Assert.Equal(EtudeId, chemin.EtudeId);
        Assert.Equal(ScenarioStrategiqueId, chemin.ScenarioStrategiqueId);
        Assert.Equal("Intrusion frontale sur le SI R&D", chemin.Description);
        Assert.Empty(chemin.EvenementsIntermediaires);
    }

    [Fact]
    public void Creer_refuse_une_etude_non_rattachee()
    {
        Assert.Throws<ArgumentException>(() => CheminAttaque.Creer(Guid.Empty, ScenarioStrategiqueId, "Description"));
    }

    [Fact]
    public void Creer_refuse_sans_scenario_strategique_rattache()
    {
        Assert.Throws<ArgumentException>(() => CheminAttaque.Creer(EtudeId, Guid.Empty, "Description"));
    }

    [Fact]
    public void Creer_accepte_zero_partie_prenante_attaque_directe()
    {
        var chemin = CheminAttaque.Creer(EtudeId, ScenarioStrategiqueId, "Canal d'exfiltration direct");

        Assert.Empty(chemin.EvenementsIntermediaires);
    }

    [Fact]
    public void AjouterEvenementIntermediaire_incremente_l_ordre_a_chaque_ajout()
    {
        var chemin = CheminAttaque.Creer(EtudeId, ScenarioStrategiqueId, "Description");

        chemin.AjouterEvenementIntermediaire(PartiePrenanteId, "Premier franchissement");
        chemin.AjouterEvenementIntermediaire(Guid.NewGuid(), "Second franchissement");

        Assert.Equal(2, chemin.EvenementsIntermediaires.Count);
        Assert.Equal(0, chemin.EvenementsIntermediaires[0].Ordre);
        Assert.Equal(1, chemin.EvenementsIntermediaires[1].Ordre);
    }

    [Fact]
    public void AjouterEvenementIntermediaire_refuse_sans_partie_prenante()
    {
        var chemin = CheminAttaque.Creer(EtudeId, ScenarioStrategiqueId, "Description");

        Assert.Throws<ArgumentException>(() => chemin.AjouterEvenementIntermediaire(Guid.Empty, "Description"));
    }

    [Fact]
    public void ModifierEvenementIntermediaire_sur_id_inexistant_leve_une_erreur()
    {
        var chemin = CheminAttaque.Creer(EtudeId, ScenarioStrategiqueId, "Description");
        chemin.AjouterEvenementIntermediaire(PartiePrenanteId, "Description");

        Assert.Throws<ArgumentException>(() => chemin.ModifierEvenementIntermediaire(Guid.NewGuid(), "Autre"));
    }

    [Fact]
    public void ModifierEvenementIntermediaire_met_a_jour_la_description()
    {
        var chemin = CheminAttaque.Creer(EtudeId, ScenarioStrategiqueId, "Description");
        chemin.AjouterEvenementIntermediaire(PartiePrenanteId, "Description initiale");
        var id = chemin.EvenementsIntermediaires[0].Id;

        chemin.ModifierEvenementIntermediaire(id, "Description corrigee");

        Assert.Equal("Description corrigee", chemin.EvenementsIntermediaires[0].Description);
    }

    [Fact]
    public void SupprimerEvenementIntermediaire_sur_id_inexistant_leve_une_erreur()
    {
        var chemin = CheminAttaque.Creer(EtudeId, ScenarioStrategiqueId, "Description");

        Assert.Throws<ArgumentException>(() => chemin.SupprimerEvenementIntermediaire(Guid.NewGuid()));
    }

    [Fact]
    public void SupprimerEvenementIntermediaire_retire_de_la_liste()
    {
        var chemin = CheminAttaque.Creer(EtudeId, ScenarioStrategiqueId, "Description");
        chemin.AjouterEvenementIntermediaire(PartiePrenanteId, "Description");
        var id = chemin.EvenementsIntermediaires[0].Id;

        chemin.SupprimerEvenementIntermediaire(id);

        Assert.Empty(chemin.EvenementsIntermediaires);
    }
}
