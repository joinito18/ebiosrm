using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

namespace EbiosRM.Api.Tests.Domain.Cadrage;

public class SocleSecuriteTests
{
    private static readonly Guid EtudeId = Guid.NewGuid();

    [Fact]
    public void Creer_avec_etude_valide_reussit()
    {
        var socle = SocleSecurite.Creer(EtudeId);

        Assert.Equal(EtudeId, socle.EtudeId);
        Assert.Empty(socle.Referentiels);
    }

    [Fact]
    public void Creer_refuse_une_etude_non_rattachee()
    {
        Assert.Throws<ArgumentException>(() => SocleSecurite.Creer(Guid.Empty));
    }

    [Fact]
    public void AjouterReferentiel_ajoute_a_la_liste_avec_les_champs_optionnels()
    {
        var socle = SocleSecurite.Creer(EtudeId);

        socle.AjouterReferentiel("A.8.1", EtatConformite.Conforme, theme: "Technologique", codeControle: "A.8.1", etatActuel: "Firewall a jour");

        var referentiel = Assert.Single(socle.Referentiels);
        Assert.Equal("A.8.1", referentiel.Nom);
        Assert.Equal(EtatConformite.Conforme, referentiel.Etat);
        Assert.Equal("Technologique", referentiel.Theme);
        Assert.Equal("Firewall a jour", referentiel.EtatActuel);
    }

    [Fact]
    public void AjouterReferentiel_accepte_les_champs_optionnels_absents()
    {
        var socle = SocleSecurite.Creer(EtudeId);

        socle.AjouterReferentiel("PSSI interne", EtatConformite.NonApplicable);

        var referentiel = Assert.Single(socle.Referentiels);
        Assert.Null(referentiel.Theme);
        Assert.Null(referentiel.CodeControle);
        Assert.Null(referentiel.EtatActuel);
    }

    [Fact]
    public void ModifierReferentiel_sur_id_inexistant_leve_une_erreur()
    {
        var socle = SocleSecurite.Creer(EtudeId);
        socle.AjouterReferentiel("A.8.1", EtatConformite.Conforme);

        Assert.Throws<ArgumentException>(
            () => socle.ModifierReferentiel(Guid.NewGuid(), "Autre nom", EtatConformite.NonConforme));
    }

    [Fact]
    public void ModifierReferentiel_met_a_jour_le_referentiel_correspondant()
    {
        var socle = SocleSecurite.Creer(EtudeId);
        socle.AjouterReferentiel("A.8.1", EtatConformite.Conforme);
        var id = socle.Referentiels[0].Id;

        socle.ModifierReferentiel(id, "A.8.1 revise", EtatConformite.NonConforme, etatActuel: "Antivirus expire");

        var referentiel = Assert.Single(socle.Referentiels);
        Assert.Equal("A.8.1 revise", referentiel.Nom);
        Assert.Equal(EtatConformite.NonConforme, referentiel.Etat);
        Assert.Equal("Antivirus expire", referentiel.EtatActuel);
    }

    [Fact]
    public void SupprimerReferentiel_sur_id_inexistant_leve_une_erreur()
    {
        var socle = SocleSecurite.Creer(EtudeId);

        Assert.Throws<ArgumentException>(() => socle.SupprimerReferentiel(Guid.NewGuid()));
    }

    [Fact]
    public void SupprimerReferentiel_retire_le_referentiel_de_la_liste()
    {
        var socle = SocleSecurite.Creer(EtudeId);
        socle.AjouterReferentiel("A.8.1", EtatConformite.Conforme);
        var id = socle.Referentiels[0].Id;

        socle.SupprimerReferentiel(id);

        Assert.Empty(socle.Referentiels);
    }

    [Fact]
    public void ReferentielApplicable_Creer_refuse_un_nom_vide()
    {
        Assert.Throws<ArgumentException>(() => ReferentielApplicable.Creer("", EtatConformite.Conforme));
    }
}
