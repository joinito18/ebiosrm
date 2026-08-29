namespace EbiosRM.Api.Modules.Suivi;

/// <summary>
/// Indicateurs de suivi <b>calculés automatiquement</b> à partir de l'état
/// courant d'une étude (jamais persistés). Complètent les indicateurs saisis
/// manuellement par l'analyste (<see cref="Domain.IndicateurSuivi"/>).
/// </summary>
public sealed class ServiceIndicateursAuto
{
    private readonly ServiceMetriquesEtude _metriques;

    public ServiceIndicateursAuto(ServiceMetriquesEtude metriques)
    {
        _metriques = metriques;
    }

    public sealed record IndicateurAuto(
        string Nom, string Categorie, double Valeur, string Unite, double? Cible, string Sens);

    public async Task<List<IndicateurAuto>> ConstruireAsync(Guid etudeId, CancellationToken ct)
    {
        var m = await _metriques.ConstruireAsync(etudeId, ct);

        var pctTermine = m.Mesures == 0 ? 0 : Math.Round(100.0 * m.MesuresTerminees / m.Mesures, 1);
        var pctReduit = m.ScenariosDeRisque == 0 ? 0 : Math.Round(100.0 * m.ScenariosRisqueReduit / m.ScenariosDeRisque, 1);

        return new List<IndicateurAuto>
        {
            new("Avancement du plan de traitement", "Traitement", pctTermine, "%", 100, "Hausse"),
            new("Mesures en retard", "Traitement", m.MesuresEnRetard, "", 0, "Baisse"),
            new("Risques résiduels élevés non acceptés", "Risque", m.RisquesEleveResiduelNonAcceptes, "", 0, "Baisse"),
            new("Scénarios dont le risque a été réduit", "Risque", pctReduit, "%", null, "Hausse"),
            new("Couverture NIS2 (indicative)", "Conformité", m.TauxCouvertureNis2 ?? 0, "%", 100, "Hausse"),
        };
    }
}
