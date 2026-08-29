using System.Text.Json;
using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;
using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

namespace EbiosRM.Api.Modules.Suivi;

/// <summary>
/// Compare les deux dernières versions du snapshot de l'Atelier 5 (créées à
/// chaque (re)validation) pour restituer l'évolution du risque résiduel et de
/// l'avancement du plan entre deux revues -- la ré-évaluation N / N-1.
/// </summary>
public sealed class ServiceEvolutionEtude
{
    private readonly ISnapshotAtelierRepository _snapshots;

    public ServiceEvolutionEtude(ISnapshotAtelierRepository snapshots)
    {
        _snapshots = snapshots;
    }

    public enum Tendance { Amelioration, Stable, Degradation, Nouveau }

    public sealed record Campagne(int Version, DateTime DateUtc, string? Libelle);

    public sealed record LigneEvolutionScenario(
        string Libelle, string? NiveauResiduelPrecedent, string? NiveauResiduelCourant, Tendance Tendance);

    public sealed record DeltaMesures(
        int Total, int TotalPrecedent, int Terminees, int TermineesPrecedent, int Ajoutees, int Retirees);

    public sealed record EvolutionEtude(
        Campagne Courante,
        Campagne? Precedente,
        IReadOnlyList<LigneEvolutionScenario> Scenarios,
        DeltaMesures Mesures);

    public async Task<EvolutionEtude?> ConstruireAsync(Guid etudeId, CancellationToken ct)
    {
        var versions = await _snapshots.ListerParEtudeIdAsync(etudeId, numeroAtelier: 5, ct); // desc
        if (versions.Count == 0) return null;

        var courant = Deserialiser(versions[0]);
        var precedent = versions.Count >= 2 ? Deserialiser(versions[1]) : null;

        var campagneCourante = new Campagne(versions[0].Version, versions[0].DateCreationUtc, versions[0].Libelle);
        var campagnePrecedente = versions.Count >= 2
            ? new Campagne(versions[1].Version, versions[1].DateCreationUtc, versions[1].Libelle)
            : null;

        var residuelPrecedentParId = precedent?.ScenariosDeRisque
            .ToDictionary(s => s.Id, s => s.NiveauRisqueResiduel)
            ?? new Dictionary<Guid, NiveauRisque?>();

        var lignes = courant.ScenariosDeRisque.Select(s =>
        {
            var courantNiveau = s.NiveauRisqueResiduel;
            if (!residuelPrecedentParId.TryGetValue(s.Id, out var precedentNiveau))
                return new LigneEvolutionScenario(Libelle(s), null, courantNiveau?.ToString(), Tendance.Nouveau);

            var tendance = (precedentNiveau, courantNiveau) switch
            {
                ({ } p, { } c) when (int)c < (int)p => Tendance.Amelioration,
                ({ } p, { } c) when (int)c > (int)p => Tendance.Degradation,
                _ => Tendance.Stable,
            };
            return new LigneEvolutionScenario(Libelle(s), precedentNiveau?.ToString(), courantNiveau?.ToString(), tendance);
        }).ToList();

        var idsPrecedent = precedent?.Mesures.Select(m => m.Id).ToHashSet() ?? new HashSet<Guid>();
        var idsCourant = courant.Mesures.Select(m => m.Id).ToHashSet();
        var delta = new DeltaMesures(
            courant.Mesures.Count,
            precedent?.Mesures.Count ?? 0,
            courant.Mesures.Count(m => m.Statut == StatutMesure.Termine),
            precedent?.Mesures.Count(m => m.Statut == StatutMesure.Termine) ?? 0,
            idsCourant.Except(idsPrecedent).Count(),
            idsPrecedent.Except(idsCourant).Count());

        return new EvolutionEtude(campagneCourante, campagnePrecedente, lignes, delta);
    }

    private static string Libelle(ScenarioDeRisqueSnapshot s) => $"{s.LibelleCouple} -- {s.LibelleChemin}";

    private static SnapshotAtelier5Content Deserialiser(SnapshotAtelier snapshot)
        => JsonSerializer.Deserialize<SnapshotAtelier5Content>(snapshot.ContenuJson)
           ?? throw new InvalidOperationException("Snapshot Atelier 5 illisible.");
}
