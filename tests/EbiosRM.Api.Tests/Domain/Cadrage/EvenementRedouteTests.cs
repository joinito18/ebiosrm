using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

namespace EbiosRM.Api.Tests.Domain.Cadrage;

public class EvenementRedouteTests
{
    private static readonly Guid EtudeId = Guid.NewGuid();
    private static readonly Guid ValeurMetierId = Guid.NewGuid();

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Creer_accepte_toute_gravite_dans_l_echelle_1_a_4(int gravite)
    {
        var er = EvenementRedoute.Creer(EtudeId, ValeurMetierId, "Fuite de données", gravite);

        Assert.Equal(gravite, er.Gravite);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(-1)]
    public void Creer_refuse_une_gravite_hors_echelle(int gravite)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => EvenementRedoute.Creer(EtudeId, ValeurMetierId, "Fuite de données", gravite));
    }

    [Fact]
    public void Creer_refuse_une_etude_non_rattachee()
    {
        Assert.Throws<ArgumentException>(
            () => EvenementRedoute.Creer(Guid.Empty, ValeurMetierId, "Fuite de données", 3));
    }

    [Fact]
    public void RecoterGravite_accepte_une_nouvelle_valeur_dans_l_echelle()
    {
        var er = EvenementRedoute.Creer(EtudeId, ValeurMetierId, "Fuite de données", 2);

        er.RecoterGravite(4);

        Assert.Equal(4, er.Gravite);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void RecoterGravite_refuse_une_valeur_hors_echelle(int nouvelleGravite)
    {
        var er = EvenementRedoute.Creer(EtudeId, ValeurMetierId, "Fuite de données", 2);

        Assert.Throws<ArgumentOutOfRangeException>(() => er.RecoterGravite(nouvelleGravite));
    }
}
