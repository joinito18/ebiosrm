using System.Text.Json;
using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;
using EbiosRM.Api.Tests.TestDoubles;

namespace EbiosRM.Api.Tests.Domain.Cadrage;

public class ServiceCreationSnapshotAtelier1Tests
{
    private static (ServiceCreationSnapshotAtelier1 Service, FakeEtudeRepository Etudes, FakeValeurMetierRepository Valeurs,
        FakeBienSupportRepository Biens, FakeEvenementRedouteRepository Evenements, FakeSocleSecuriteRepository Socles,
        FakeSnapshotAtelierRepository Snapshots) CreerService()
    {
        var etudes = new FakeEtudeRepository();
        var valeurs = new FakeValeurMetierRepository();
        var biens = new FakeBienSupportRepository();
        var evenements = new FakeEvenementRedouteRepository();
        var socles = new FakeSocleSecuriteRepository();
        var snapshots = new FakeSnapshotAtelierRepository();
        var service = new ServiceCreationSnapshotAtelier1(etudes, valeurs, biens, evenements, socles, snapshots);
        return (service, etudes, valeurs, biens, evenements, socles, snapshots);
    }

    private static Etude CreerEtudeValidee()
    {
        var etude = Etude.Creer("Etude test", "Perimetre", "Mission");
        etude.DemarrerAtelier1();
        etude.ValiderAtelier1();
        return etude;
    }

    [Fact]
    public async Task CreerAsync_refuse_une_etude_introuvable()
    {
        var (service, _, _, _, _, _, _) = CreerService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreerAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task CreerAsync_refuse_une_etude_non_validee()
    {
        var (service, etudes, _, _, _, _, _) = CreerService();
        var etude = Etude.Creer("Etude test", "Perimetre", "Mission"); // reste en Brouillon
        etudes.Etudes.Add(etude);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreerAsync(etude.Id, CancellationToken.None));
    }

    [Fact]
    public async Task CreerAsync_fige_correctement_le_contenu_en_version_1()
    {
        var (service, etudes, valeurs, biens, _, _, _) = CreerService();
        var etude = CreerEtudeValidee();
        etudes.Etudes.Add(etude);
        var valeur = ValeurMetier.Creer(etude.Id, "R&D", "Direction scientifique");
        valeurs.Items.Add(valeur);
        biens.Items.Add(BienSupport.Creer(etude.Id, valeur.Id, "Serveur", TypeBienSupport.SystemeInformation, "DSI"));

        var snapshot = await service.CreerAsync(etude.Id, CancellationToken.None);

        Assert.Equal(1, snapshot.NumeroAtelier);
        Assert.Equal(1, snapshot.Version);
        var contenu = JsonSerializer.Deserialize<SnapshotAtelier1Content>(snapshot.ContenuJson);
        Assert.NotNull(contenu);
        Assert.Single(contenu!.ValeursMetier);
        Assert.Single(contenu.BiensSupport);
        Assert.Equal("R&D", contenu.ValeursMetier[0].Description);
    }

    [Fact]
    public async Task CreerAsync_incremente_la_version_a_chaque_appel()
    {
        var (service, etudes, _, _, _, _, snapshots) = CreerService();
        var etude = CreerEtudeValidee();
        etudes.Etudes.Add(etude);

        var premier = await service.CreerAsync(etude.Id, CancellationToken.None);
        var second = await service.CreerAsync(etude.Id, CancellationToken.None);

        Assert.Equal(1, premier.Version);
        Assert.Equal(2, second.Version);
        Assert.Equal(2, snapshots.Items.Count);
    }
}
