using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

namespace EbiosRM.Api.Tests.Domain.Cadrage;

public class EtudeTests
{
    [Fact]
    public void Creer_avec_donnees_valides_produit_une_etude_en_brouillon()
    {
        var etude = Etude.Creer("Étude Banque X", "Périmètre SI cœur", "Analyser les risques cyber");

        Assert.NotEqual(Guid.Empty, etude.Id);
        Assert.Equal("Étude Banque X", etude.Nom);
        Assert.Equal(StatutEtude.Brouillon, etude.Statut);
    }

    [Theory]
    [InlineData("", "Périmètre", "Mission")]
    [InlineData(" ", "Périmètre", "Mission")]
    [InlineData(null, "Périmètre", "Mission")]
    public void Creer_refuse_un_nom_vide(string? nom, string perimetre, string mission)
    {
        Assert.Throws<ArgumentException>(() => Etude.Creer(nom!, perimetre, mission));
    }

    [Fact]
    public void Creer_refuse_un_perimetre_vide()
    {
        Assert.Throws<ArgumentException>(() => Etude.Creer("Nom", "", "Mission"));
    }

    [Fact]
    public void Creer_refuse_une_mission_vide()
    {
        Assert.Throws<ArgumentException>(() => Etude.Creer("Nom", "Périmètre", ""));
    }

    [Fact]
    public void Creer_nettoie_les_espaces_superflus()
    {
        var etude = Etude.Creer("  Nom  ", "  Périmètre  ", "  Mission  ");

        Assert.Equal("Nom", etude.Nom);
        Assert.Equal("Périmètre", etude.Perimetre);
        Assert.Equal("Mission", etude.Mission);
    }

    [Fact]
    public void DemarrerAtelier1_depuis_Brouillon_passe_a_EnCours()
    {
        var etude = Etude.Creer("Nom", "Périmètre", "Mission");

        etude.DemarrerAtelier1();

        Assert.Equal(StatutEtude.EnCours, etude.Statut);
    }

    [Fact]
    public void DemarrerAtelier1_refuse_si_deja_demarre()
    {
        var etude = Etude.Creer("Nom", "Périmètre", "Mission");
        etude.DemarrerAtelier1();

        Assert.Throws<InvalidOperationException>(() => etude.DemarrerAtelier1());
    }

    [Fact]
    public void ValiderAtelier1_depuis_EnCours_passe_a_Validee()
    {
        var etude = Etude.Creer("Nom", "Périmètre", "Mission");
        etude.DemarrerAtelier1();

        etude.ValiderAtelier1();

        Assert.Equal(StatutEtude.Validee, etude.Statut);
    }

    [Fact]
    public void ValiderAtelier1_refuse_depuis_Brouillon()
    {
        var etude = Etude.Creer("Nom", "Périmètre", "Mission");

        Assert.Throws<InvalidOperationException>(() => etude.ValiderAtelier1());
    }

    [Fact]
    public void ValiderAtelier1_refuse_si_deja_validee()
    {
        var etude = Etude.Creer("Nom", "Périmètre", "Mission");
        etude.DemarrerAtelier1();
        etude.ValiderAtelier1();

        Assert.Throws<InvalidOperationException>(() => etude.ValiderAtelier1());
    }

    [Fact]
    public void RouvrirAtelier1_depuis_Validee_repasse_a_EnCours()
    {
        var etude = Etude.Creer("Nom", "Périmètre", "Mission");
        etude.DemarrerAtelier1();
        etude.ValiderAtelier1();

        etude.RouvrirAtelier1();

        Assert.Equal(StatutEtude.EnCours, etude.Statut);
    }

    [Fact]
    public void RouvrirAtelier1_refuse_si_pas_encore_validee()
    {
        var etude = Etude.Creer("Nom", "Périmètre", "Mission");
        etude.DemarrerAtelier1();

        Assert.Throws<InvalidOperationException>(() => etude.RouvrirAtelier1());
    }

    [Fact]
    public void Cycle_complet_demarrer_valider_rouvrir_revalider_est_possible()
    {
        var etude = Etude.Creer("Nom", "Périmètre", "Mission");

        etude.DemarrerAtelier1();
        etude.ValiderAtelier1();
        etude.RouvrirAtelier1();
        etude.ValiderAtelier1();

        Assert.Equal(StatutEtude.Validee, etude.Statut);
    }
}
