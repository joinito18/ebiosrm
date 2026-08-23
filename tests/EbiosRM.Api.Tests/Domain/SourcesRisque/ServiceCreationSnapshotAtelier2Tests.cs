using System.Text.Json;
using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;
using EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;
using EbiosRM.Api.Tests.TestDoubles;

namespace EbiosRM.Api.Tests.Domain.SourcesRisque;

public class ServiceCreationSnapshotAtelier2Tests
{
    private static (ServiceCreationSnapshotAtelier2 Service, FakeEtudeRepository Etudes, FakePartiePrenanteRepository Parties,
        FakeCoupleSourceRisqueObjectifViseRepository Couples, FakeSnapshotAtelierRepository Snapshots) CreerService()
    {
        var etudes = new FakeEtudeRepository();
        var parties = new FakePartiePrenanteRepository();
        var couples = new FakeCoupleSourceRisqueObjectifViseRepository();
        var snapshots = new FakeSnapshotAtelierRepository();
        var service = new ServiceCreationSnapshotAtelier2(etudes, parties, couples, snapshots);
        return (service, etudes, parties, couples, snapshots);
    }

    private static Etude CreerEtudeAvecAtelier2Valide()
    {
        var etude = Etude.Creer("Etude test", "Perimetre", "Mission");
        etude.DemarrerAtelier1();
        etude.ValiderAtelier1();
        etude.DemarrerAtelier2();
        etude.ValiderAtelier2();
        return etude;
    }

    [Fact]
    public async Task CreerAsync_refuse_si_atelier_2_non_valide()
    {
        var (service, etudes, _, _, _) = CreerService();
        var etude = Etude.Creer("Etude test", "Perimetre", "Mission");
        etude.DemarrerAtelier1();
        etude.ValiderAtelier1(); // StatutAtelier2 reste Brouillon
        etudes.Etudes.Add(etude);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreerAsync(etude.Id, CancellationToken.None));
    }

    [Fact]
    public async Task CreerAsync_fige_le_contenu_y_compris_le_jugement_d_expert()
    {
        var (service, etudes, parties, couples, _) = CreerService();
        var etude = CreerEtudeAvecAtelier2Valide();
        etudes.Etudes.Add(etude);
        parties.Items.Add(PartiePrenante.Creer(etude.Id, "Prestataire IT", "Maintenance", "M. Dupont", CategoriePartiePrenante.Prestataire));
        var couple = CoupleSourceRisqueObjectifVise.Creer(
            etude.Id, CategorieSourceRisque.Etatique, "Description SR", CategorieObjectifVise.Lucratif, "Description OV",
            "Contexte", "Technologique", 4, 4, ServiceCalculPertinence.Calculer(4, 4));
        couple.DefinirPertinenceRetenue(NiveauPertinence.PeuPertinent, "Jugement d'expert de test.");
        couples.Items.Add(couple);

        var snapshot = await service.CreerAsync(etude.Id, CancellationToken.None);

        var contenu = JsonSerializer.Deserialize<SnapshotAtelier2Content>(snapshot.ContenuJson);
        Assert.NotNull(contenu);
        Assert.Single(contenu!.PartiesPrenantes);
        var coupleSnapshot = Assert.Single(contenu.Couples);
        Assert.Equal("PeuPertinent", coupleSnapshot.Pertinence);
        Assert.True(coupleSnapshot.PertinenceEstJugementExpert);
        Assert.Equal("Jugement d'expert de test.", coupleSnapshot.JustificationPertinence);
    }
}
