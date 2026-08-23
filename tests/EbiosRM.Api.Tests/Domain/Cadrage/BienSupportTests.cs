using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

namespace EbiosRM.Api.Tests.Domain.Cadrage;

public class BienSupportTests
{
    private static readonly Guid EtudeId = Guid.NewGuid();
    private static readonly Guid ValeurMetierId = Guid.NewGuid();

    [Fact]
    public void Creer_avec_donnees_valides_reussit()
    {
        var bien = BienSupport.Creer(EtudeId, ValeurMetierId, "Serveur de production", TypeBienSupport.SystemeInformation, "DSI");

        Assert.Equal(EtudeId, bien.EtudeId);
        Assert.Equal(ValeurMetierId, bien.ValeurMetierId);
        Assert.Equal("Serveur de production", bien.Description);
        Assert.Equal(TypeBienSupport.SystemeInformation, bien.Type);
        Assert.Equal("DSI", bien.EntiteProprietaire);
    }

    [Fact]
    public void Creer_refuse_une_etude_non_rattachee()
    {
        Assert.Throws<ArgumentException>(
            () => BienSupport.Creer(Guid.Empty, ValeurMetierId, "Description", TypeBienSupport.Reseau, "DSI"));
    }

    [Fact]
    public void Creer_refuse_sans_valeur_metier_associee_INV7()
    {
        Assert.Throws<ArgumentException>(
            () => BienSupport.Creer(EtudeId, Guid.Empty, "Description", TypeBienSupport.Reseau, "DSI"));
    }

    [Fact]
    public void Creer_refuse_une_description_vide()
    {
        Assert.Throws<ArgumentException>(
            () => BienSupport.Creer(EtudeId, ValeurMetierId, "", TypeBienSupport.Local, "DSI"));
    }

    [Fact]
    public void Creer_refuse_une_entite_proprietaire_vide()
    {
        Assert.Throws<ArgumentException>(
            () => BienSupport.Creer(EtudeId, ValeurMetierId, "Description", TypeBienSupport.Local, ""));
    }

    [Fact]
    public void Modifier_met_a_jour_description_type_et_proprietaire()
    {
        var bien = BienSupport.Creer(EtudeId, ValeurMetierId, "Description initiale", TypeBienSupport.Local, "Entité initiale");

        bien.Modifier("Nouvelle description", TypeBienSupport.RessourcesHumaines, "Nouvelle entité");

        Assert.Equal("Nouvelle description", bien.Description);
        Assert.Equal(TypeBienSupport.RessourcesHumaines, bien.Type);
        Assert.Equal("Nouvelle entité", bien.EntiteProprietaire);
    }

    [Fact]
    public void Modifier_ne_change_jamais_la_valeur_metier_rattachee()
    {
        var bien = BienSupport.Creer(EtudeId, ValeurMetierId, "Description", TypeBienSupport.Local, "Entité");

        bien.Modifier("Autre description", TypeBienSupport.Reseau, "Autre entité");

        Assert.Equal(ValeurMetierId, bien.ValeurMetierId);
    }

    [Fact]
    public void Modifier_refuse_une_description_vide()
    {
        var bien = BienSupport.Creer(EtudeId, ValeurMetierId, "Description", TypeBienSupport.Local, "Entité");

        Assert.Throws<ArgumentException>(() => bien.Modifier("", TypeBienSupport.Local, "Entité"));
    }
}
