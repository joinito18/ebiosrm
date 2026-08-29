using ClosedXML.Excel;
using EbiosRM.Api.Modules.Conformite;
using EbiosRM.Api.Modules.Conformite.Domain;
using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;
using EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;
using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

namespace EbiosRM.Api.Modules.Reporting.Exports;

/// <summary>
/// Export Excel (.xlsx) du registre des risques d'une étude : un classeur
/// multi-feuilles reprenant l'état COURANT (pas un snapshot figé) des scénarios
/// de risque, du plan de traitement, de l'écosystème et de la conformité --
/// pour que l'analyste retravaille le livrable dans un tableur.
/// </summary>
public sealed class RegistreRisquesExcelGenerator
{
    private readonly IEtudeRepository _etudes;
    private readonly ServiceAssemblageScenariosDeRisque _assemblage;
    private readonly IPlanTraitementRisqueRepository _plans;
    private readonly IPartiePrenanteRepository _parties;
    private readonly ServiceConformite _conformite;

    public RegistreRisquesExcelGenerator(
        IEtudeRepository etudes,
        ServiceAssemblageScenariosDeRisque assemblage,
        IPlanTraitementRisqueRepository plans,
        IPartiePrenanteRepository parties,
        ServiceConformite conformite)
    {
        _etudes = etudes;
        _assemblage = assemblage;
        _plans = plans;
        _parties = parties;
        _conformite = conformite;
    }

    public async Task<byte[]?> GenererAsync(Guid etudeId, CancellationToken ct)
    {
        var etude = await _etudes.ObtenirParIdAsync(etudeId, ct);
        if (etude is null) return null;

        var scenarios = await _assemblage.ListerAsync(etudeId, ct);
        var plan = await _plans.ObtenirParEtudeAsync(etudeId, ct);
        var mesures = plan?.Mesures.ToList() ?? new List<MesureTraitementRisque>();
        var parties = await _parties.ListerParEtudeAsync(etudeId, ct);
        var libelleScenario = scenarios.ToDictionary(s => s.Id, s => $"{s.LibelleCouple} — {s.LibelleChemin}");

        using var classeur = new XLWorkbook();

        // --- Feuille : synthèse ---
        var synthese = classeur.Worksheets.Add("Synthèse");
        synthese.Cell(1, 1).Value = "Registre des risques — EBIOS Risk Manager";
        synthese.Cell(1, 1).Style.Font.Bold = true;
        synthese.Cell(1, 1).Style.Font.FontSize = 14;
        synthese.Cell(3, 1).Value = "Étude"; synthese.Cell(3, 2).Value = etude.Nom;
        synthese.Cell(4, 1).Value = "Périmètre"; synthese.Cell(4, 2).Value = etude.Perimetre;
        synthese.Cell(5, 1).Value = "Mission"; synthese.Cell(5, 2).Value = etude.Mission;
        synthese.Cell(6, 1).Value = "Export généré le"; synthese.Cell(6, 2).Value = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm 'UTC'");
        synthese.Cell(8, 1).Value = "Scénarios de risque"; synthese.Cell(8, 2).Value = scenarios.Count;
        synthese.Cell(9, 1).Value = "dont résiduel élevé"; synthese.Cell(9, 2).Value = scenarios.Count(s => s.NiveauRisqueResiduel == NiveauRisque.Eleve);
        synthese.Cell(10, 1).Value = "Mesures de traitement"; synthese.Cell(10, 2).Value = mesures.Count;
        synthese.Cell(11, 1).Value = "dont terminées"; synthese.Cell(11, 2).Value = mesures.Count(m => m.Statut == StatutMesure.Termine);
        synthese.Column(1).Style.Font.Bold = true;
        synthese.Columns().AdjustToContents();

        // --- Feuille : scénarios de risque ---
        var fs = classeur.Worksheets.Add("Scénarios de risque");
        EnteteLigne(fs, "Source de risque / objectif visé", "Chemin d'attaque", "Gravité",
            "Vraisemblance initiale", "Niveau de risque initial", "Gravité résiduelle",
            "Vraisemblance résiduelle", "Niveau de risque résiduel", "Classe d'acceptation résiduelle",
            "Accepté par la direction", "Propriétaire du risque", "Validateur sécurité", "Date d'acceptation");
        var r = 2;
        foreach (var s in scenarios)
        {
            fs.Cell(r, 1).Value = s.LibelleCouple;
            fs.Cell(r, 2).Value = s.LibelleChemin;
            fs.Cell(r, 3).Value = s.Gravite;
            fs.Cell(r, 4).Value = s.VraisemblanceInitiale?.ToString() ?? "";
            fs.Cell(r, 5).Value = s.NiveauRisqueInitial?.ToString() ?? "";
            fs.Cell(r, 6).Value = s.GraviteResiduelle?.ToString() ?? "";
            fs.Cell(r, 7).Value = s.VraisemblanceResiduelle?.ToString() ?? "";
            fs.Cell(r, 8).Value = s.NiveauRisqueResiduel?.ToString() ?? "";
            fs.Cell(r, 9).Value = s.ClasseAcceptationResiduelle?.ToString() ?? "";
            fs.Cell(r, 10).Value = s.AccepteParDirection ? "Oui" : "Non";
            fs.Cell(r, 11).Value = s.NomProprietaireRisque ?? "";
            fs.Cell(r, 12).Value = s.NomValidateurSecurite ?? "";
            fs.Cell(r, 13).Value = s.DateAcceptationUtc?.ToString("dd/MM/yyyy") ?? "";
            ColorerNiveau(fs.Cell(r, 5), s.NiveauRisqueInitial?.ToString());
            ColorerNiveau(fs.Cell(r, 8), s.NiveauRisqueResiduel?.ToString());
            r++;
        }
        Finaliser(fs, r);

        // --- Feuille : plan de traitement ---
        var fp = classeur.Worksheets.Add("Plan de traitement");
        EnteteLigne(fp, "Mesure", "Axe", "Scénarios de risque couverts", "Responsable",
            "Coût / complexité", "Échéance", "Statut", "Freins et difficultés", "Conformité couverte");
        r = 2;
        foreach (var m in mesures)
        {
            fp.Cell(r, 1).Value = m.Description;
            fp.Cell(r, 2).Value = m.Axe.ToString();
            fp.Cell(r, 3).Value = string.Join(" ; ", m.ScenariosDeRisqueIds.Select(id => libelleScenario.GetValueOrDefault(id, "(scénario supprimé)")));
            fp.Cell(r, 4).Value = m.Responsable;
            fp.Cell(r, 5).Value = m.CoutComplexite.LibelleAvecMot();
            fp.Cell(r, 6).Value = m.Echeance ?? "";
            fp.Cell(r, 7).Value = LibelleStatut(m.Statut);
            fp.Cell(r, 8).Value = m.FreinsEtDifficultes ?? "";
            fp.Cell(r, 9).Value = string.Join(", ", m.CodesConformite);
            r++;
        }
        Finaliser(fp, r);

        // --- Feuille : écosystème ---
        var fe = classeur.Worksheets.Add("Écosystème");
        EnteteLigne(fe, "Partie prenante", "Catégorie", "Représentant", "Dépendance", "Pénétration",
            "Maturité cyber", "Confiance", "Niveau de dangerosité", "Zone",
            "Niveau résiduel", "Zone résiduelle", "Mesures sur l'écosystème");
        r = 2;
        foreach (var p in parties)
        {
            fe.Cell(r, 1).Value = p.Nom;
            fe.Cell(r, 2).Value = LibellesCategoriePartiePrenante(p.Categorie);
            fe.Cell(r, 3).Value = p.Representant;
            fe.Cell(r, 4).Value = p.Dependance?.ToString() ?? "";
            fe.Cell(r, 5).Value = p.Penetration?.ToString() ?? "";
            fe.Cell(r, 6).Value = p.MaturiteCyber?.ToString() ?? "";
            fe.Cell(r, 7).Value = p.Confiance?.ToString() ?? "";
            fe.Cell(r, 8).Value = p.NiveauDangerosite?.ToString("0.##") ?? "";
            fe.Cell(r, 9).Value = p.Zone?.ToString() ?? "";
            fe.Cell(r, 10).Value = p.NiveauDangerositeResiduel?.ToString("0.##") ?? "";
            fe.Cell(r, 11).Value = p.ZoneResiduelle?.ToString() ?? "";
            fe.Cell(r, 12).Value = string.Join(" ; ", p.Mesures.Select(x => x.Description));
            r++;
        }
        Finaliser(fe, r);

        // --- Feuille : conformité ---
        var fc = classeur.Worksheets.Add("Conformité");
        EnteteLigne(fc, "Référentiel", "Code", "Exigence", "Catégorie", "Couverture", "Socle", "Mesures");
        r = 2;
        foreach (var referentiel in new[] { ReferentielConformite.Iso27001, ReferentielConformite.Nis2 })
        {
            var rapport = await _conformite.ConstruireAsync(etudeId, referentiel, ct);
            if (rapport is null) continue;
            foreach (var l in rapport.Lignes)
            {
                fc.Cell(r, 1).Value = referentiel == ReferentielConformite.Nis2 ? "NIS2" : "ISO 27001";
                fc.Cell(r, 2).Value = l.Code;
                fc.Cell(r, 3).Value = l.Titre;
                fc.Cell(r, 4).Value = l.Categorie;
                fc.Cell(r, 5).Value = LibelleCouverture(l.Couverture);
                fc.Cell(r, 6).Value = l.EtatSocle ?? "";
                fc.Cell(r, 7).Value = string.Join(" ; ", l.Mesures.Select(x => x.Description));
                r++;
            }
        }
        Finaliser(fc, r);

        using var flux = new MemoryStream();
        classeur.SaveAs(flux);
        return flux.ToArray();
    }

    private static void EnteteLigne(IXLWorksheet ws, params string[] titres)
    {
        for (var i = 0; i < titres.Length; i++)
        {
            var c = ws.Cell(1, i + 1);
            c.Value = titres[i];
            c.Style.Font.Bold = true;
            c.Style.Fill.BackgroundColor = XLColor.FromHtml("#000091");
            c.Style.Font.FontColor = XLColor.White;
        }
    }

    private static void Finaliser(IXLWorksheet ws, int derniereLigne)
    {
        if (derniereLigne > 2)
            ws.Range(1, 1, derniereLigne - 1, ws.LastColumnUsed()?.ColumnNumber() ?? 1).SetAutoFilter();
        ws.SheetView.FreezeRows(1);
        ws.Columns().AdjustToContents(15, 55);
    }

    private static void ColorerNiveau(IXLCell cell, string? niveau)
    {
        var couleur = niveau switch
        {
            "Eleve" => "#F6D9CC",
            "Moyen" => "#F6E7CC",
            "Faible" => "#D9EFD9",
            _ => null,
        };
        if (couleur is not null) cell.Style.Fill.BackgroundColor = XLColor.FromHtml(couleur);
    }

    private static string LibelleStatut(StatutMesure s) => s switch
    {
        StatutMesure.ALancer => "À lancer",
        StatutMesure.EnCours => "En cours",
        StatutMesure.Termine => "Terminé",
        _ => s.ToString(),
    };

    private static string LibelleCouverture(ServiceConformite.Couverture c) => c switch
    {
        ServiceConformite.Couverture.Conforme => "Conforme",
        ServiceConformite.Couverture.Partielle => "Partielle",
        ServiceConformite.Couverture.NonApplicable => "Non applicable",
        _ => "Non couverte",
    };

    private static string LibellesCategoriePartiePrenante(CategoriePartiePrenante c) => c.ToString();
}
