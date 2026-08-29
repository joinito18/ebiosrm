using System.Reflection;
using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static EbiosRM.Api.Modules.Reporting.RapportPdfStyle;

namespace EbiosRM.Api.Modules.Reporting;

/// <summary>
/// Manuel utilisateur complet en PDF, assemblé à partir des mêmes fichiers
/// Markdown que l'aide en ligne (frontend/src/guides/*.md, embarqués dans
/// l'assembly). Un petit lecteur Markdown maison (mêmes règles que
/// frontend/src/components/shared/Markdown.tsx) convertit chaque guide en
/// blocs QuestPDF.
/// </summary>
public sealed class ManuelPdfGenerator
{
    public byte[] Generer(string? langue = null)
    {
        var en = string.Equals(langue, "en", StringComparison.OrdinalIgnoreCase);
        var guides = ChargerGuides(en ? "en" : "fr");
        var titre = en ? "User manual" : "Manuel d'utilisation";
        var sommaire = en ? "Contents" : "Sommaire";
        var pied = en ? "EBIOS Risk Manager -- User manual" : "EBIOS Risk Manager -- Manuel d'utilisation";

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2.2f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontFamily(Sans).FontColor(Encre).LineHeight(1.35f));

                page.Header().ShowOnce().Column(col =>
                {
                    col.Item().Text("EBIOS RISK MANAGER").FontFamily(MonoMedium).FontSize(8).FontColor(BleuFrance).LetterSpacing(0.05f);
                    col.Item().PaddingTop(2).Text(titre).FontFamily(SerifTitreSemiBold).FontSize(22).FontColor(Encre);
                    col.Item().PaddingTop(10).LineHorizontal(1.4f).LineColor(BleuFrance);
                });

                page.Content().PaddingTop(14).Column(col =>
                {
                    col.Item().PaddingBottom(6).Text(sommaire).FontFamily(SansSemiBold).FontSize(12).FontColor(Encre);
                    foreach (var g in guides)
                        col.Item().PaddingBottom(1.5f).Text("•  " + g.Titre).FontSize(9).FontColor(GrisTexte);

                    foreach (var g in guides)
                    {
                        col.Item().PageBreak();
                        RendreBlocs(col, MarqueurMarkdown.Analyser(g.Contenu));
                    }
                });

                page.Footer().Column(col =>
                {
                    col.Item().PaddingBottom(4).LineHorizontal(0.6f).LineColor(GrisLigne);
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text(pied).FontFamily(Mono).FontSize(7).FontColor(GrisTexte);
                        row.RelativeItem().AlignRight().Text(t =>
                        {
                            t.CurrentPageNumber().FontFamily(MonoMedium).FontSize(7).FontColor(GrisTexte);
                            t.Span(" / ").FontFamily(Mono).FontSize(7).FontColor(GrisTexte);
                            t.TotalPages().FontFamily(Mono).FontSize(7).FontColor(GrisTexte);
                        });
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    // --- Chargement des guides embarques -----------------------------------

    private sealed record Guide(string Titre, string Contenu);

    private static List<Guide> ChargerGuides(string langue)
    {
        var asm = Assembly.GetExecutingAssembly();
        var prefixe = $"Guides.{langue}.";
        var noms = asm.GetManifestResourceNames()
            .Where(n => n.StartsWith(prefixe, StringComparison.Ordinal) && n.EndsWith(".md", StringComparison.Ordinal))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        var guides = new List<Guide>();
        foreach (var nom in noms)
        {
            using var flux = asm.GetManifestResourceStream(nom);
            if (flux is null) continue;
            using var lecteur = new StreamReader(flux);
            var contenu = lecteur.ReadToEnd();
            var titre = PremierTitre(contenu) ?? nom;
            guides.Add(new Guide(titre, contenu));
        }
        return guides;
    }

    private static string? PremierTitre(string markdown)
    {
        foreach (var ligne in markdown.Split('\n'))
        {
            var m = Regex.Match(ligne, @"^#\s+(.*)$");
            if (m.Success) return m.Groups[1].Value.Trim();
        }
        return null;
    }

    // --- Rendu des blocs --------------------------------------------------

    private static void RendreBlocs(ColumnDescriptor col, IReadOnlyList<MarqueurMarkdown.Bloc> blocs)
    {
        foreach (var bloc in blocs)
        {
            switch (bloc)
            {
                case MarqueurMarkdown.Titre t:
                    var (taille, couleur, famille, haut) = t.Niveau switch
                    {
                        1 => (18f, Encre, SerifTitreSemiBold, 0f),
                        2 => (13f, Encre, SansSemiBold, 12f),
                        3 => (9f, BleuFrance, MonoMedium, 10f),
                        _ => (9.5f, Encre, SansSemiBold, 6f),
                    };
                    col.Item().PaddingTop(haut).PaddingBottom(3).Text(t.Niveau == 3 ? t.Texte.ToUpperInvariant() : t.Texte)
                        .FontFamily(famille).FontSize(taille).FontColor(couleur);
                    break;

                case MarqueurMarkdown.Paragraphe p:
                    col.Item().PaddingBottom(5).Text(txt => AppliquerInline(txt, p.Texte));
                    break;

                case MarqueurMarkdown.Citation q:
                    col.Item().PaddingBottom(5).BorderLeft(2).BorderColor(BleuFranceClair).PaddingLeft(8)
                        .Text(txt => { AppliquerInline(txt, q.Texte); txt.DefaultTextStyle(s => s.FontColor(GrisTexte).Italic()); });
                    break;

                case MarqueurMarkdown.Liste l:
                    col.Item().PaddingBottom(5).Column(c =>
                    {
                        var i = 1;
                        foreach (var item in l.Items)
                        {
                            var puce = l.Ordonnee ? (i++ + ".") : "•";
                            c.Item().PaddingLeft(item.SousNiveau ? 20 : 6).PaddingBottom(1.5f).Row(row =>
                            {
                                row.ConstantItem(l.Ordonnee ? 16 : 10).Text(item.SousNiveau ? "–" : puce).FontSize(9).FontColor(GrisTexte);
                                row.RelativeItem().Text(txt => AppliquerInline(txt, item.Texte));
                            });
                        }
                    });
                    break;

                case MarqueurMarkdown.Code cb:
                    col.Item().PaddingBottom(5).Background(GrisFond).Border(0.7f).BorderColor(GrisLigne).Padding(6)
                        .Text(cb.Texte).FontFamily(Mono).FontSize(8).FontColor(Encre);
                    break;

                case MarqueurMarkdown.Regle:
                    col.Item().PaddingVertical(6).LineHorizontal(0.6f).LineColor(GrisLigne);
                    break;

                case MarqueurMarkdown.Tableau tb:
                    col.Item().PaddingBottom(6).Table(table =>
                    {
                        table.ColumnsDefinition(cd => { foreach (var _ in tb.Entetes) cd.RelativeColumn(); });
                        foreach (var e in tb.Entetes)
                            table.Cell().Element(c => EnteteCellule(c, e));
                        var pair = false;
                        foreach (var ligne in tb.Lignes)
                        {
                            var fond = pair ? GrisFond : "#FFFFFF";
                            pair = !pair;
                            foreach (var cellule in ligne)
                                table.Cell().Background(fond).Padding(4).Text(txt => { AppliquerInline(txt, cellule); txt.DefaultTextStyle(s => s.FontSize(8)); });
                        }
                    });
                    break;
            }
        }
    }

    private static void AppliquerInline(TextDescriptor txt, string texte)
    {
        // Decoupe sur **gras**, *italique*, `code`, [lien](url).
        var motif = new Regex(@"(\*\*[^*]+\*\*|\*[^*\n]+\*|`[^`]+`|\[[^\]]+\]\([^)]+\))");
        var position = 0;
        foreach (Match m in motif.Matches(texte))
        {
            if (m.Index > position)
                txt.Span(texte[position..m.Index]);

            var jeton = m.Value;
            if (jeton.StartsWith("**"))
                txt.Span(jeton[2..^2]).FontFamily(SansSemiBold);
            else if (jeton.StartsWith("*"))
                txt.Span(jeton[1..^1]).Italic();
            else if (jeton.StartsWith("`"))
                txt.Span(jeton[1..^1]).FontFamily(Mono).FontSize(8.5f).FontColor(BleuFrance);
            else
            {
                var lien = Regex.Match(jeton, @"^\[([^\]]+)\]\(([^)]+)\)$");
                var libelle = lien.Groups[1].Value;
                var url = lien.Groups[2].Value;
                if (url.StartsWith("http"))
                    txt.Hyperlink(libelle, url).FontColor(BleuFrance).Underline();
                else
                    txt.Span(libelle).FontColor(BleuFrance);
            }
            position = m.Index + m.Length;
        }
        if (position < texte.Length)
            txt.Span(texte[position..]);
    }
}
