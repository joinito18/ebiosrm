using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

namespace EbiosRM.Api.Modules.Reporting;

public sealed record RapportSyntheseGlobaleData(
    string NomEtude,
    string Perimetre,
    string Mission,
    DateTime DateSynthese,
    ChiffresClesData ChiffresCles,
    List<ScenarioDeRisqueData> ScenariosDeRisque,
    List<MesureTraitementRisqueData> Mesures,
    Dictionary<string, int> AvancementPlanParStatut,
    ConformiteSocleData ConformiteSocle);

public sealed record ConformiteSocleData(
    int NombreConforme,
    int NombreNonConforme,
    int NombreNonApplicable,
    List<ControleNonConformeData> ControlesNonConformes,
    List<ConformiteThemeData> ParTheme)
{
    /// <summary>Source unique de calcul, utilisee par RapportSyntheseGlobaleService et RapportAtelier5Service pour ne pas diverger.</summary>
    public static ConformiteSocleData DepuisReferentiels(List<ReferentielApplicableSnapshot> referentiels)
    {
        var parTheme = referentiels
            .GroupBy(r => string.IsNullOrWhiteSpace(r.Theme) ? "Autre" : r.Theme!)
            .Select(g => new ConformiteThemeData(
                g.Key,
                g.Count(r => r.EtatConformite == "Conforme"),
                g.Count(r => r.EtatConformite == "NonConforme"),
                g.Count(r => r.EtatConformite == "NonApplicable")))
            .OrderBy(t => t.Theme == "Autre" ? 1 : 0).ThenBy(t => t.Theme)
            .ToList();

        return new ConformiteSocleData(
            referentiels.Count(r => r.EtatConformite == "Conforme"),
            referentiels.Count(r => r.EtatConformite == "NonConforme"),
            referentiels.Count(r => r.EtatConformite == "NonApplicable"),
            referentiels.Where(r => r.EtatConformite == "NonConforme")
                .Select(r => new ControleNonConformeData(r.CodeControle, r.Nom, r.EtatActuel))
                .ToList(),
            parTheme);
    }
}

public sealed record ControleNonConformeData(string? CodeControle, string Nom, string? EtatActuel);

/// <summary>Repartition de la conformite par theme ISO 27001 (Organisationnel/Personnes/Physique/Technologique) -- alimente le graphique en barres et le radar du rapport.</summary>
public sealed record ConformiteThemeData(string Theme, int NombreConforme, int NombreNonConforme, int NombreNonApplicable)
{
    public int Total => NombreConforme + NombreNonConforme + NombreNonApplicable;
    public double TauxConformitePct => Total == 0 ? 0 : 100.0 * NombreConforme / Total;
}

public sealed record ChiffresClesData(
    int NombreValeursMetier,
    int NombreBiensSupport,
    int NombreEvenementsRedoutes,
    int NombrePartiesPrenantes,
    int NombrePartiesPrenantesCritiques,
    int NombreScenariosStrategiques,
    int NombreScenariosOperationnels,
    List<string> NomsPartiesPrenantesCritiques);
