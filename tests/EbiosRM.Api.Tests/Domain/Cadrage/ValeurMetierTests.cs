using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

namespace EbiosRM.Api.Tests.Domain.Cadrage;

public class ValeurMetierTests
{
    private static readonly Guid EtudeId = Guid.NewGuid();

    [Fact]
    public void Creer_avec_donnees_valides_reussit()
    {
        var valeur = ValeurMetier.Creer(EtudeId, "Continuité de service", "Direction des opérations");

        Assert.Equal(EtudeId, valeur.EtudeId);
        Assert.Equal("Continuité de service", valeur.Description);
    }

    [Fact]
    public void Creer_refuse_une_etude_non_rattachee()
    {
        Assert.Throws<ArgumentException>(
            () => ValeurMetier.Creer(Guid.Empty, "Description", "Entité"));
    }

    [Fact]
    public void Creer_refuse_une_description_vide()
    {
        Assert.Throws<ArgumentException>(
            () => ValeurMetier.Creer(EtudeId, "", "Entité"));
    }

    [Fact]
    public void Modifier_met_a_jour_description_et_entite_proprietaire()
    {
        var valeur = ValeurMetier.Creer(EtudeId, "Description initiale", "Entité initiale");

        valeur.Modifier("Nouvelle description", "Nouvelle entité");

        Assert.Equal("Nouvelle description", valeur.Description);
        Assert.Equal("Nouvelle entité", valeur.EntiteProprietaire);
    }

    [Fact]
    public void Modifier_refuse_une_description_vide()
    {
        var valeur = ValeurMetier.Creer(EtudeId, "Description", "Entité");

        Assert.Throws<ArgumentException>(() => valeur.Modifier("", "Entité"));
    }
}
