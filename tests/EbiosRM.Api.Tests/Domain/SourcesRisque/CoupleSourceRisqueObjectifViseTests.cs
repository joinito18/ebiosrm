using EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;

namespace EbiosRM.Api.Tests.Domain.SourcesRisque;

public class CoupleSourceRisqueObjectifViseTests
{
    private static readonly Guid EtudeId = Guid.NewGuid();

    private static CoupleSourceRisqueObjectifVise CreerCouple() =>
        CoupleSourceRisqueObjectifVise.Creer(
            EtudeId, CategorieSourceRisque.Etatique, "Description SR", CategorieObjectifVise.Lucratif, "Description OV",
            "Contexte", "Technologique", motivation: 2, ressources: 2,
            pertinenceCalculee: ServiceCalculPertinence.Calculer(2, 2));

    [Fact]
    public void Pertinence_reflete_la_valeur_calculee_par_defaut()
    {
        var couple = CreerCouple();

        Assert.Equal(couple.PertinenceCalculee, couple.Pertinence);
        Assert.Null(couple.PertinenceRetenue);
    }

    [Fact]
    public void DefinirPertinenceRetenue_devient_la_valeur_effective_sans_effacer_la_calculee()
    {
        var couple = CreerCouple();
        var calculeeInitiale = couple.PertinenceCalculee;

        couple.DefinirPertinenceRetenue(NiveauPertinence.TresPertinent, "Contexte metier non capture par la formule.");

        Assert.Equal(NiveauPertinence.TresPertinent, couple.Pertinence);
        Assert.Equal(calculeeInitiale, couple.PertinenceCalculee);
        Assert.Equal("Contexte metier non capture par la formule.", couple.JustificationPertinence);
    }

    [Fact]
    public void DefinirPertinenceRetenue_refuse_une_justification_vide()
    {
        var couple = CreerCouple();

        Assert.Throws<ArgumentException>(() => couple.DefinirPertinenceRetenue(NiveauPertinence.TresPertinent, ""));
    }

    [Fact]
    public void ReinitialiserPertinence_revient_a_la_valeur_calculee()
    {
        var couple = CreerCouple();
        couple.DefinirPertinenceRetenue(NiveauPertinence.TresPertinent, "Justification.");

        couple.ReinitialiserPertinence();

        Assert.Equal(couple.PertinenceCalculee, couple.Pertinence);
        Assert.Null(couple.PertinenceRetenue);
        Assert.Null(couple.JustificationPertinence);
    }
}
