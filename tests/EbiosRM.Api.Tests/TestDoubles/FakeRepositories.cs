using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;
using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;
using EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;
using EbiosRM.Api.Modules.Identity.Domain;

namespace EbiosRM.Api.Tests.TestDoubles;

// Doublures en mémoire des repositories, sans librairie de mock (convention
// du projet). Juste assez de comportement pour exercer les Domain Services
// qui orchestrent plusieurs repositories (ServiceValidationCompletudeAtelierN,
// ServiceCreationSnapshotAtelierN) sans base de données réelle.

public sealed class FakeUtilisateurRepository : IUtilisateurRepository
{
    public List<Utilisateur> Items { get; } = new();
    public Task AjouterAsync(Utilisateur u, CancellationToken ct) { Items.Add(u); return Task.CompletedTask; }
    public Task<Utilisateur?> ObtenirParIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Items.FirstOrDefault(u => u.Id == id));
    public Task<Utilisateur?> ObtenirParEmailAsync(string email, CancellationToken ct) => Task.FromResult(Items.FirstOrDefault(u => u.Email == email));
}

public sealed class FakeEtudeRepository : IEtudeRepository
{
    public List<Etude> Etudes { get; } = new();
    public Task AjouterAsync(Etude etude, CancellationToken ct) { Etudes.Add(etude); return Task.CompletedTask; }
    public Task<Etude?> ObtenirParIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Etudes.FirstOrDefault(e => e.Id == id));
    public Task<List<Etude>> ListerAsync(CancellationToken ct) => Task.FromResult(Etudes.ToList());
    public Task<List<Etude>> ListerVisiblesAsync(Guid utilisateurId, CancellationToken ct) =>
        Task.FromResult(Etudes.Where(e => e.ProprietaireId == null || e.ProprietaireId == utilisateurId).ToList());
    public Task MettreAJourAsync(Etude etude, CancellationToken ct) => Task.CompletedTask;
}

public sealed class FakeValeurMetierRepository : IValeurMetierRepository
{
    public List<ValeurMetier> Items { get; } = new();
    public Task AjouterAsync(ValeurMetier v, CancellationToken ct) { Items.Add(v); return Task.CompletedTask; }
    public Task<ValeurMetier?> ObtenirParIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Items.FirstOrDefault(v => v.Id == id));
    public Task<List<ValeurMetier>> ListerParEtudeAsync(Guid etudeId, CancellationToken ct) => Task.FromResult(Items.Where(v => v.EtudeId == etudeId).ToList());
    public Task MettreAJourAsync(ValeurMetier v, CancellationToken ct) => Task.CompletedTask;
    public Task SupprimerAsync(ValeurMetier v, CancellationToken ct) { Items.Remove(v); return Task.CompletedTask; }
}

public sealed class FakeBienSupportRepository : IBienSupportRepository
{
    public List<BienSupport> Items { get; } = new();
    public Task AjouterAsync(BienSupport b, CancellationToken ct) { Items.Add(b); return Task.CompletedTask; }
    public Task<BienSupport?> ObtenirParIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Items.FirstOrDefault(b => b.Id == id));
    public Task<List<BienSupport>> ListerParEtudeAsync(Guid etudeId, CancellationToken ct) => Task.FromResult(Items.Where(b => b.EtudeId == etudeId).ToList());
    public Task<List<BienSupport>> ListerParValeurMetierAsync(Guid valeurMetierId, CancellationToken ct) => Task.FromResult(Items.Where(b => b.ValeurMetierId == valeurMetierId).ToList());
    public Task MettreAJourAsync(BienSupport b, CancellationToken ct) => Task.CompletedTask;
    public Task SupprimerAsync(BienSupport b, CancellationToken ct) { Items.Remove(b); return Task.CompletedTask; }
}

public sealed class FakeEvenementRedouteRepository : IEvenementRedouteRepository
{
    public List<EvenementRedoute> Items { get; } = new();
    public Task AjouterAsync(EvenementRedoute e, CancellationToken ct) { Items.Add(e); return Task.CompletedTask; }
    public Task<EvenementRedoute?> ObtenirParIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Items.FirstOrDefault(e => e.Id == id));
    public Task<List<EvenementRedoute>> ListerParEtudeAsync(Guid etudeId, CancellationToken ct) => Task.FromResult(Items.Where(e => e.EtudeId == etudeId).ToList());
    public Task MettreAJourAsync(EvenementRedoute e, CancellationToken ct) => Task.CompletedTask;
    public Task SupprimerAsync(EvenementRedoute e, CancellationToken ct) { Items.Remove(e); return Task.CompletedTask; }
}

public sealed class FakeSocleSecuriteRepository : ISocleSecuriteRepository
{
    public List<SocleSecurite> Items { get; } = new();
    public Task AjouterAsync(SocleSecurite s, CancellationToken ct) { Items.Add(s); return Task.CompletedTask; }
    public Task<SocleSecurite?> ObtenirParEtudeAsync(Guid etudeId, CancellationToken ct) => Task.FromResult(Items.FirstOrDefault(s => s.EtudeId == etudeId));
    public Task MettreAJourAsync(SocleSecurite s, CancellationToken ct) => Task.CompletedTask;
}

public sealed class FakeCoupleSourceRisqueObjectifViseRepository : ICoupleSourceRisqueObjectifViseRepository
{
    public List<CoupleSourceRisqueObjectifVise> Items { get; } = new();
    public Task AjouterAsync(CoupleSourceRisqueObjectifVise c, CancellationToken ct) { Items.Add(c); return Task.CompletedTask; }
    public Task<CoupleSourceRisqueObjectifVise?> ObtenirParIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Items.FirstOrDefault(c => c.Id == id));
    public Task<List<CoupleSourceRisqueObjectifVise>> ListerParEtudeAsync(Guid etudeId, CancellationToken ct) => Task.FromResult(Items.Where(c => c.EtudeId == etudeId).ToList());
    public Task MettreAJourAsync(CoupleSourceRisqueObjectifVise c, CancellationToken ct) => Task.CompletedTask;
    public Task SupprimerAsync(CoupleSourceRisqueObjectifVise c, CancellationToken ct) { Items.Remove(c); return Task.CompletedTask; }
}

public sealed class FakePartiePrenanteRepository : IPartiePrenanteRepository
{
    public List<PartiePrenante> Items { get; } = new();
    public Task AjouterAsync(PartiePrenante p, CancellationToken ct) { Items.Add(p); return Task.CompletedTask; }
    public Task<PartiePrenante?> ObtenirParIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Items.FirstOrDefault(p => p.Id == id));
    public Task<List<PartiePrenante>> ListerParEtudeAsync(Guid etudeId, CancellationToken ct) => Task.FromResult(Items.Where(p => p.EtudeId == etudeId).ToList());
    public Task MettreAJourAsync(PartiePrenante p, CancellationToken ct) => Task.CompletedTask;
    public Task SupprimerAsync(PartiePrenante p, CancellationToken ct) { Items.Remove(p); return Task.CompletedTask; }
}

public sealed class FakeScenarioStrategiqueRepository : IScenarioStrategiqueRepository
{
    public List<ScenarioStrategique> Items { get; } = new();
    public Task AjouterAsync(ScenarioStrategique s, CancellationToken ct) { Items.Add(s); return Task.CompletedTask; }
    public Task<ScenarioStrategique?> ObtenirParIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Items.FirstOrDefault(s => s.Id == id));
    public Task<ScenarioStrategique?> ObtenirParCoupleIdAsync(Guid coupleId, CancellationToken ct) => Task.FromResult(Items.FirstOrDefault(s => s.CoupleSourceRisqueObjectifViseId == coupleId));
    public Task<List<ScenarioStrategique>> ListerParEtudeAsync(Guid etudeId, CancellationToken ct) => Task.FromResult(Items.Where(s => s.EtudeId == etudeId).ToList());
    public Task MettreAJourAsync(ScenarioStrategique s, CancellationToken ct) => Task.CompletedTask;
    public Task SupprimerAsync(ScenarioStrategique s, CancellationToken ct) { Items.Remove(s); return Task.CompletedTask; }
}

public sealed class FakeCheminAttaqueRepository : ICheminAttaqueRepository
{
    public List<CheminAttaque> Items { get; } = new();
    public Task AjouterAsync(CheminAttaque c, CancellationToken ct) { Items.Add(c); return Task.CompletedTask; }
    public Task<CheminAttaque?> ObtenirParIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Items.FirstOrDefault(c => c.Id == id));
    public Task<List<CheminAttaque>> ListerParEtudeAsync(Guid etudeId, CancellationToken ct) => Task.FromResult(Items.Where(c => c.EtudeId == etudeId).ToList());
    public Task<List<CheminAttaque>> ListerParScenarioAsync(Guid scenarioId, CancellationToken ct) => Task.FromResult(Items.Where(c => c.ScenarioStrategiqueId == scenarioId).ToList());
    public Task MettreAJourAsync(CheminAttaque c, CancellationToken ct) => Task.CompletedTask;
    public Task SupprimerAsync(CheminAttaque c, CancellationToken ct) { Items.Remove(c); return Task.CompletedTask; }
}

public sealed class FakeScenarioOperationnelRepository : IScenarioOperationnelRepository
{
    public List<ScenarioOperationnel> Items { get; } = new();
    public Task AjouterAsync(ScenarioOperationnel s, CancellationToken ct) { Items.Add(s); return Task.CompletedTask; }
    public Task<ScenarioOperationnel?> ObtenirParIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Items.FirstOrDefault(s => s.Id == id));
    public Task<ScenarioOperationnel?> ObtenirParCheminIdAsync(Guid cheminId, CancellationToken ct) => Task.FromResult(Items.FirstOrDefault(s => s.CheminAttaqueId == cheminId));
    public Task<List<ScenarioOperationnel>> ListerParEtudeAsync(Guid etudeId, CancellationToken ct) => Task.FromResult(Items.Where(s => s.EtudeId == etudeId).ToList());
    public Task MettreAJourAsync(ScenarioOperationnel s, CancellationToken ct) => Task.CompletedTask;
    public Task SupprimerAsync(ScenarioOperationnel s, CancellationToken ct) { Items.Remove(s); return Task.CompletedTask; }
}

public sealed class FakeSnapshotAtelierRepository : ISnapshotAtelierRepository
{
    public List<SnapshotAtelier> Items { get; } = new();
    public Task AjouterAsync(SnapshotAtelier s, CancellationToken ct) { Items.Add(s); return Task.CompletedTask; }
    public Task<SnapshotAtelier?> ObtenirDernierParEtudeIdAsync(Guid etudeId, int numeroAtelier, CancellationToken ct) =>
        Task.FromResult(Items.Where(s => s.EtudeId == etudeId && s.NumeroAtelier == numeroAtelier).OrderByDescending(s => s.Version).FirstOrDefault());
    public Task<int> CompterParEtudeIdAsync(Guid etudeId, int numeroAtelier, CancellationToken ct) =>
        Task.FromResult(Items.Count(s => s.EtudeId == etudeId && s.NumeroAtelier == numeroAtelier));
}

public sealed class FakeScenarioDeRisqueRepository : IScenarioDeRisqueRepository
{
    public List<ScenarioDeRisque> Items { get; } = new();
    public Task AjouterAsync(ScenarioDeRisque s, CancellationToken ct) { Items.Add(s); return Task.CompletedTask; }
    public Task<ScenarioDeRisque?> ObtenirParIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Items.FirstOrDefault(s => s.Id == id));
    public Task<ScenarioDeRisque?> ObtenirParCheminIdAsync(Guid cheminId, CancellationToken ct) => Task.FromResult(Items.FirstOrDefault(s => s.CheminAttaqueId == cheminId));
    public Task<List<ScenarioDeRisque>> ListerParEtudeAsync(Guid etudeId, CancellationToken ct) => Task.FromResult(Items.Where(s => s.EtudeId == etudeId).ToList());
    public Task MettreAJourAsync(ScenarioDeRisque s, CancellationToken ct) => Task.CompletedTask;
    public Task SupprimerAsync(ScenarioDeRisque s, CancellationToken ct) { Items.Remove(s); return Task.CompletedTask; }
}

public sealed class FakePlanTraitementRisqueRepository : IPlanTraitementRisqueRepository
{
    public List<PlanTraitementRisque> Items { get; } = new();
    public Task AjouterAsync(PlanTraitementRisque p, CancellationToken ct) { Items.Add(p); return Task.CompletedTask; }
    public Task<PlanTraitementRisque?> ObtenirParEtudeAsync(Guid etudeId, CancellationToken ct) => Task.FromResult(Items.FirstOrDefault(p => p.EtudeId == etudeId));
    public Task MettreAJourAsync(PlanTraitementRisque p, CancellationToken ct) => Task.CompletedTask;
}
