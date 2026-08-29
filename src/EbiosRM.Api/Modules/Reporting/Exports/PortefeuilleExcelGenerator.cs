using ClosedXML.Excel;
using EbiosRM.Api.Modules.Suivi;

namespace EbiosRM.Api.Modules.Reporting.Exports;

/// <summary>Export Excel de la vue portefeuille : une ligne par étude visible.</summary>
public sealed class PortefeuilleExcelGenerator
{
    private readonly ServicePortefeuille _portefeuille;

    public PortefeuilleExcelGenerator(ServicePortefeuille portefeuille)
    {
        _portefeuille = portefeuille;
    }

    public async Task<byte[]> GenererAsync(Guid utilisateurId, CancellationToken ct)
    {
        var lignes = await _portefeuille.ConstruireAsync(utilisateurId, ct);

        using var classeur = new XLWorkbook();
        var ws = classeur.Worksheets.Add("Portefeuille");

        var titres = new[]
        {
            "Étude", "Statut", "Atelier 5", "Scénarios de risque",
            "Résiduel faible", "Résiduel moyen", "Résiduel élevé",
            "Élevés non acceptés", "Mesures", "Mesures terminées", "Mesures en retard", "Couverture NIS2 (%)",
        };
        for (var i = 0; i < titres.Length; i++)
        {
            var c = ws.Cell(1, i + 1);
            c.Value = titres[i];
            c.Style.Font.Bold = true;
            c.Style.Fill.BackgroundColor = XLColor.FromHtml("#000091");
            c.Style.Font.FontColor = XLColor.White;
        }

        var r = 2;
        foreach (var l in lignes)
        {
            ws.Cell(r, 1).Value = l.Nom;
            ws.Cell(r, 2).Value = l.Statut;
            ws.Cell(r, 3).Value = l.StatutAtelier5;
            ws.Cell(r, 4).Value = l.ScenariosDeRisque;
            ws.Cell(r, 5).Value = l.RisquesResiduels.GetValueOrDefault("Faible");
            ws.Cell(r, 6).Value = l.RisquesResiduels.GetValueOrDefault("Moyen");
            ws.Cell(r, 7).Value = l.RisquesResiduels.GetValueOrDefault("Eleve");
            ws.Cell(r, 8).Value = l.RisquesEleveResiduelNonAcceptes;
            ws.Cell(r, 9).Value = l.Mesures;
            ws.Cell(r, 10).Value = l.MesuresTerminees;
            ws.Cell(r, 11).Value = l.MesuresEnRetard;
            if (l.TauxCouvertureNis2 is { } taux) ws.Cell(r, 12).Value = taux;
            if (l.RisquesResiduels.GetValueOrDefault("Eleve") > 0)
                ws.Cell(r, 7).Style.Fill.BackgroundColor = XLColor.FromHtml("#F6D9CC");
            r++;
        }

        if (r > 2) ws.Range(1, 1, r - 1, titres.Length).SetAutoFilter();
        ws.SheetView.FreezeRows(1);
        ws.Columns().AdjustToContents(12, 45);

        using var flux = new MemoryStream();
        classeur.SaveAs(flux);
        return flux.ToArray();
    }
}
