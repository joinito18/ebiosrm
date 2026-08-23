using EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;

namespace EbiosRM.Api.Tests.Domain.SourcesRisque;

public class PartiePrenanteTests
{
    private static readonly Guid EtudeId = Guid.NewGuid();

    private static PartiePrenante CreerPartie() =>
        PartiePrenante.Creer(EtudeId, "Prestataire IT", "Maintenance infra", "M. Dupont", CategoriePartiePrenante.Prestataire);

    [Fact]
    public void NiveauDangerosite_reflete_la_valeur_calculee_par_defaut()
    {
        var partie = CreerPartie();
        var niveau = ServiceCalculNiveauDangerosite.Calculer(3, 3, 2, 2);

        partie.EvaluerDangerosite(3, 3, 2, 2, niveau);

        Assert.Equal(niveau, partie.NiveauDangerosite);
        Assert.Null(partie.NiveauDangerositeRetenu);
    }

    [Fact]
    public void DefinirDangerositeRetenue_devient_la_valeur_effective_sans_effacer_la_calculee()
    {
        var partie = CreerPartie();
        var niveauCalcule = ServiceCalculNiveauDangerosite.Calculer(3, 3, 2, 2);
        partie.EvaluerDangerosite(3, 3, 2, 2, niveauCalcule);

        partie.DefinirDangerositeRetenue(5.0, "Historique d'incidents connu, non capture par la formule.");

        Assert.Equal(5.0, partie.NiveauDangerosite);
        Assert.Equal(niveauCalcule, partie.NiveauDangerositeCalcule);
        Assert.Equal(ZoneDangerosite.Danger, partie.Zone);
    }

    [Fact]
    public void DefinirDangerositeRetenue_refuse_une_justification_vide()
    {
        var partie = CreerPartie();

        Assert.Throws<ArgumentException>(() => partie.DefinirDangerositeRetenue(2.0, ""));
    }

    [Fact]
    public void ReinitialiserDangerosite_revient_a_la_valeur_calculee()
    {
        var partie = CreerPartie();
        var niveauCalcule = ServiceCalculNiveauDangerosite.Calculer(3, 3, 2, 2);
        partie.EvaluerDangerosite(3, 3, 2, 2, niveauCalcule);
        partie.DefinirDangerositeRetenue(5.0, "Justification.");

        partie.ReinitialiserDangerosite();

        Assert.Equal(niveauCalcule, partie.NiveauDangerosite);
        Assert.Null(partie.NiveauDangerositeRetenu);
        Assert.Null(partie.JustificationDangerosite);
    }
}
