using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;
using EbiosRM.Api.Tests.TestDoubles;

namespace EbiosRM.Api.Tests.Domain.Cadrage;

public class ServiceValidationCompletudeAtelier1Tests
{
    private static readonly Guid EtudeId = Guid.NewGuid();

    private static (ServiceValidationCompletudeAtelier1 Service, FakeValeurMetierRepository Valeurs, FakeBienSupportRepository Biens, FakeEvenementRedouteRepository Evenements) CreerService()
    {
        var valeurs = new FakeValeurMetierRepository();
        var biens = new FakeBienSupportRepository();
        var evenements = new FakeEvenementRedouteRepository();
        var service = new ServiceValidationCompletudeAtelier1(valeurs, biens, evenements);
        return (service, valeurs, biens, evenements);
    }

    [Fact]
    public async Task VerifierAsync_incomplet_signale_les_3_elements_manquants_si_rien_n_existe()
    {
        var (service, _, _, _) = CreerService();

        var resultat = await service.VerifierAsync(EtudeId, CancellationToken.None);

        Assert.False(resultat.EstComplet);
        Assert.Equal(3, resultat.ElementsManquants.Count);
    }

    [Fact]
    public async Task VerifierAsync_complet_quand_les_3_types_de_donnees_existent()
    {
        var (service, valeurs, biens, evenements) = CreerService();
        var valeur = ValeurMetier.Creer(EtudeId, "Description", "Entité");
        valeurs.Items.Add(valeur);
        biens.Items.Add(BienSupport.Creer(EtudeId, valeur.Id, "Description", TypeBienSupport.Local, "Entité"));
        evenements.Items.Add(EvenementRedoute.Creer(EtudeId, valeur.Id, "Description", 2));

        var resultat = await service.VerifierAsync(EtudeId, CancellationToken.None);

        Assert.True(resultat.EstComplet);
        Assert.Empty(resultat.ElementsManquants);
    }

    [Fact]
    public async Task VerifierAsync_incomplet_si_seuls_les_biens_supports_manquent()
    {
        var (service, valeurs, _, evenements) = CreerService();
        var valeur = ValeurMetier.Creer(EtudeId, "Description", "Entité");
        valeurs.Items.Add(valeur);
        evenements.Items.Add(EvenementRedoute.Creer(EtudeId, valeur.Id, "Description", 2));

        var resultat = await service.VerifierAsync(EtudeId, CancellationToken.None);

        Assert.False(resultat.EstComplet);
        Assert.Single(resultat.ElementsManquants);
    }

    [Fact]
    public async Task VerifierAsync_ignore_les_donnees_d_une_autre_etude()
    {
        var (service, valeurs, biens, evenements) = CreerService();
        var autreEtudeId = Guid.NewGuid();
        var valeur = ValeurMetier.Creer(autreEtudeId, "Description", "Entité");
        valeurs.Items.Add(valeur);
        biens.Items.Add(BienSupport.Creer(autreEtudeId, valeur.Id, "Description", TypeBienSupport.Local, "Entité"));
        evenements.Items.Add(EvenementRedoute.Creer(autreEtudeId, valeur.Id, "Description", 2));

        var resultat = await service.VerifierAsync(EtudeId, CancellationToken.None);

        Assert.False(resultat.EstComplet);
    }
}
