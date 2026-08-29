using System.Globalization;
using EbiosRM.Api.Modules.Conformite;
using EbiosRM.Api.Modules.Conformite.Domain;
using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

namespace EbiosRM.Api.Modules.Suivi;

/// <summary>
/// Calcule, pour une étude, les métriques transverses réutilisées par la vue
/// portefeuille et par les indicateurs de suivi automatiques : répartition
/// des risques (initiale / résiduelle), avancement du plan de traitement,
/// mesures en retard, taux de couverture NIS2.
/// </summary>
public sealed class ServiceMetriquesEtude
{
    private readonly ServiceAssemblageScenariosDeRisque _assemblage;
    private readonly IPlanTraitementRisqueRepository _plans;
    private readonly ServiceConformite _conformite;

    public ServiceMetriquesEtude(
        ServiceAssemblageScenariosDeRisque assemblage,
        IPlanTraitementRisqueRepository plans,
        ServiceConformite conformite)
    {
        _assemblage = assemblage;
        _plans = plans;
        _conformite = conformite;
    }

    public sealed record MetriquesEtude(
        Dictionary<string, int> RisquesInitiaux,
        Dictionary<string, int> RisquesResiduels,
        int ScenariosDeRisque,
        int RisquesEleveResiduelNonAcceptes,
        int ScenariosRisqueReduit,
        Dictionary<string, int> MesuresParStatut,
        int Mesures,
        int MesuresTerminees,
        int MesuresEnRetard,
        double? TauxCouvertureNis2);

    public async Task<MetriquesEtude> ConstruireAsync(Guid etudeId, CancellationToken ct)
    {
        var scenarios = await _assemblage.ListerAsync(etudeId, ct);
        var plan = await _plans.ObtenirParEtudeAsync(etudeId, ct);
        var mesures = plan?.Mesures.ToList() ?? new List<MesureTraitementRisque>();

        var risquesInitiaux = Repartition(scenarios.Select(s => s.NiveauRisqueInitial?.ToString()));
        var risquesResiduels = Repartition(scenarios.Select(s => s.NiveauRisqueResiduel?.ToString()));

        var risqueReduit = scenarios.Count(s =>
            s.NiveauRisqueInitial is { } i && s.NiveauRisqueResiduel is { } r && (int)r < (int)i);

        var eleveNonAccepte = scenarios.Count(s =>
            s.NiveauRisqueResiduel == NiveauRisque.Eleve && !s.AccepteParDirection);

        var mesuresParStatut = mesures.GroupBy(m => m.Statut.ToString()).ToDictionary(g => g.Key, g => g.Count());
        var terminees = mesures.Count(m => m.Statut == StatutMesure.Termine);
        var enRetard = mesures.Count(m => m.Statut != StatutMesure.Termine && EstEcheanceDepassee(m.Echeance));

        var nis2 = await _conformite.ConstruireAsync(etudeId, ReferentielConformite.Nis2, ct);
        double? tauxNis2 = null;
        if (nis2 is not null)
        {
            var pertinent = nis2.Synthese.Total - nis2.Synthese.NonApplicable;
            tauxNis2 = pertinent == 0 ? 100 : Math.Round(100.0 * (nis2.Synthese.Conforme + nis2.Synthese.Partielle) / pertinent, 1);
        }

        return new MetriquesEtude(
            risquesInitiaux, risquesResiduels, scenarios.Count,
            eleveNonAccepte, risqueReduit,
            mesuresParStatut, mesures.Count, terminees, enRetard,
            tauxNis2);
    }

    private static Dictionary<string, int> Repartition(IEnumerable<string?> niveaux)
    {
        var d = new Dictionary<string, int> { ["Faible"] = 0, ["Moyen"] = 0, ["Eleve"] = 0, ["NonEvalue"] = 0 };
        foreach (var n in niveaux)
            d[n ?? "NonEvalue"] = d.GetValueOrDefault(n ?? "NonEvalue") + 1;
        return d;
    }

    /// <summary>
    /// L'échéance d'une mesure est du texte libre (« 6 mois », « 12/2026 »,
    /// « Q1 2027 »...). On tente quelques formats de date explicites ; une
    /// échéance non datable n'est jamais comptée comme « en retard ».
    /// </summary>
    private static readonly string[] FormatsEcheance =
        { "dd/MM/yyyy", "d/M/yyyy", "MM/yyyy", "M/yyyy", "yyyy-MM-dd", "yyyy-MM" };

    public static bool EstEcheanceDepassee(string? echeance)
    {
        if (string.IsNullOrWhiteSpace(echeance)) return false;
        var texte = echeance.Trim();
        foreach (var format in FormatsEcheance)
            if (DateTime.TryParseExact(texte, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return d.Date < DateTime.UtcNow.Date;
        return false;
    }
}
