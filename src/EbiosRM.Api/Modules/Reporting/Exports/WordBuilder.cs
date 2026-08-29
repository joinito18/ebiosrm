using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace EbiosRM.Api.Modules.Reporting.Exports;

/// <summary>
/// Petit constructeur de document Word (.docx) au-dessus de
/// DocumentFormat.OpenXml : titres, paragraphes et tableaux. Suffisant pour
/// produire un livrable éditable ; ce n'est pas un moteur de mise en page.
/// </summary>
public sealed class WordBuilder : IDisposable
{
    private readonly MemoryStream _flux = new();
    private readonly WordprocessingDocument _doc;
    private readonly Body _corps;

    public WordBuilder()
    {
        _doc = WordprocessingDocument.Create(_flux, WordprocessingDocumentType.Document);
        var main = _doc.AddMainDocumentPart();
        main.Document = new Document(new Body());
        _corps = main.Document.Body!;
    }

    public WordBuilder Titre(string texte, int niveau = 1)
    {
        var taille = niveau switch { 1 => "34", 2 => "28", _ => "24" };
        _corps.AppendChild(new Paragraph(
            new ParagraphProperties(new SpacingBetweenLines { Before = "240", After = "120" }),
            new Run(new RunProperties(new Bold(), new Color { Val = "000091" }, new FontSize { Val = taille }),
                new Text(texte))));
        return this;
    }

    public WordBuilder Paragraphe(string texte, bool italique = false, string? couleur = null)
    {
        var props = new RunProperties();
        if (italique) props.Append(new Italic());
        if (couleur is not null) props.Append(new Color { Val = couleur });
        _corps.AppendChild(new Paragraph(new Run(props, new Text(texte) { Space = SpaceProcessingModeValues.Preserve })));
        return this;
    }

    public WordBuilder Tableau(IReadOnlyList<string> entetes, IEnumerable<IReadOnlyList<string>> lignes)
    {
        var table = new Table(new TableProperties(
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4, Color = "DDDDDD" },
                new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "DDDDDD" },
                new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "DDDDDD" },
                new RightBorder { Val = BorderValues.Single, Size = 4, Color = "DDDDDD" },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "DDDDDD" },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "DDDDDD" }),
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct }));

        table.AppendChild(LigneTableau(entetes, entete: true));
        foreach (var ligne in lignes)
            table.AppendChild(LigneTableau(ligne, entete: false));

        _corps.AppendChild(table);
        _corps.AppendChild(new Paragraph());
        return this;
    }

    private static TableRow LigneTableau(IReadOnlyList<string> cellules, bool entete)
    {
        var row = new TableRow();
        foreach (var valeur in cellules)
        {
            var runProps = new RunProperties(new FontSize { Val = "18" });
            if (entete) { runProps.Append(new Bold()); runProps.Append(new Color { Val = "FFFFFF" }); }

            var cellProps = new TableCellProperties(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center });
            if (entete) cellProps.Append(new Shading { Val = ShadingPatternValues.Clear, Fill = "000091" });

            row.AppendChild(new TableCell(cellProps,
                new Paragraph(new Run(runProps, new Text(valeur ?? "") { Space = SpaceProcessingModeValues.Preserve }))));
        }
        return row;
    }

    public byte[] Terminer()
    {
        _doc.MainDocumentPart!.Document.Save();
        _doc.Dispose();
        return _flux.ToArray();
    }

    public void Dispose()
    {
        _doc.Dispose();
        _flux.Dispose();
    }
}
