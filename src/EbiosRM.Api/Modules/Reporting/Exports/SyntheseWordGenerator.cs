namespace EbiosRM.Api.Modules.Reporting.Exports;

/// <summary>
/// Export Word (.docx) : synthèse de l'étude (identité, chiffres clés,
/// registre des risques, plan de traitement) sous forme de tableaux natifs
/// éditables -- point de départ pour un rapport retravaillé dans Word.
/// Reprend l'état COURANT (même source que le cadre de suivi).
/// </summary>
public sealed class SyntheseWordGenerator
{
    private readonly RapportCadreDeSuiviService _service;

    public SyntheseWordGenerator(RapportCadreDeSuiviService service)
    {
        _service = service;
    }

    public async Task<byte[]?> GenererAsync(Guid etudeId, CancellationToken ct)
    {
        var data = await _service.ConstruireAsync(etudeId, ct);
        if (data is null) return null;

        using var w = new WordBuilder();

        w.Titre("EBIOS Risk Manager — Synthèse de l'étude", 1);
        w.Titre(data.NomEtude, 2);
        w.Paragraphe($"Périmètre : {data.Perimetre}");
        w.Paragraphe($"Document généré le {data.DateGeneration:dd/MM/yyyy} — état courant de l'étude, à compléter.", italique: true, couleur: "3A3A3A");

        w.Titre("Chiffres clés", 2);
        var eleveResiduel = data.ScenariosDeRisque.Count(s => s.NiveauRisqueResiduel == "Eleve");
        w.Tableau(
            new[] { "Indicateur", "Valeur" },
            new[]
            {
                new[] { "Scénarios de risque", data.ScenariosDeRisque.Count.ToString() },
                new[] { "Scénarios au risque résiduel élevé", eleveResiduel.ToString() },
                new[] { "Mesures de traitement", data.Mesures.Count.ToString() },
                new[] { "Mesures terminées", data.AvancementParStatut.GetValueOrDefault("Termine", 0).ToString() },
                new[] { "Mesures en cours", data.AvancementParStatut.GetValueOrDefault("EnCours", 0).ToString() },
                new[] { "Mesures à lancer", data.AvancementParStatut.GetValueOrDefault("ALancer", 0).ToString() },
            });

        w.Titre("Registre des risques", 2);
        w.Tableau(
            new[] { "Source de risque / objectif visé", "Chemin d'attaque", "Gravité", "Niveau initial", "Niveau résiduel", "Accepté" },
            data.ScenariosDeRisque.Select(s => (IReadOnlyList<string>)new[]
            {
                s.LibelleCouple, s.LibelleChemin, s.Gravite.ToString(),
                s.NiveauRisqueInitial ?? "—", s.NiveauRisqueResiduel ?? "—",
                s.AccepteParDirection ? "Oui" : "Non",
            }).ToList());

        w.Titre("Plan de traitement du risque", 2);
        w.Tableau(
            new[] { "Mesure", "Axe", "Responsable", "Échéance", "Statut", "Scénarios couverts" },
            data.Mesures.Select(m => (IReadOnlyList<string>)new[]
            {
                m.Description, m.Axe, m.Responsable, m.Echeance ?? "—",
                LibelleStatut(m.Statut), string.Join(" ; ", m.LibellesScenariosDeRisque),
            }).ToList());

        return w.Terminer();
    }

    private static string LibelleStatut(string statut) => statut switch
    {
        "ALancer" => "À lancer",
        "EnCours" => "En cours",
        "Termine" => "Terminé",
        _ => statut,
    };
}
