using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static EbiosRM.Api.Modules.Reporting.RapportPdfStyle;

namespace EbiosRM.Api.Modules.Reporting;

/// <summary>
/// Cadre de suivi (4e livrable officiel EBIOS RM) : contrairement aux
/// rapports d'atelier et à la synthèse globale, ce document reflète l'état
/// courant du plan de traitement, pas un instantané figé -- régénérable à
/// tout moment pour suivre la progression réelle des mesures.
/// </summary>
public sealed class RapportCadreDeSuiviPdfGenerator
{
    public byte[] Generer(RapportCadreDeSuiviData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2.2f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontFamily(Sans).FontColor(Encre));

                page.Header().Column(col =>
                {
                    col.Item().Text("EBIOS RISK MANAGER").FontFamily(MonoMedium).FontSize(8).FontColor(BleuFrance).LetterSpacing(0.05f);
                    col.Item().PaddingTop(2).Text("Cadre de suivi").FontFamily(SerifTitreSemiBold).FontSize(20).FontColor(Encre);
                    col.Item().Text(data.NomEtude).FontFamily(Mono).FontSize(8).FontColor(GrisTexte);
                    col.Item().PaddingTop(10).LineHorizontal(1.4f).LineColor(BleuFrance);
                });

                page.Content().PaddingTop(18).Column(col =>
                {
                    col.Spacing(20);

                    col.Item().Text("Ce document reflète l'état courant du plan de traitement et des risques résiduels -- contrairement aux rapports d'atelier, il n'est pas figé et peut être régénéré à tout moment pour suivre la progression réelle des mesures de sécurité.").FontSize(8.5f).Italic().FontColor(GrisTexte);

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, "Identite de l'etude");
                        c.Item().PaddingTop(4).Text(t =>
                        {
                            t.Span("Perimetre : ").FontFamily(SansSemiBold).FontSize(8.5f);
                            t.Span(data.Perimetre).FontSize(8.5f);
                        });
                        c.Item().PaddingTop(2).Text("Cadre de suivi genere le " + data.DateGeneration.ToString("dd/MM/yyyy 'a' HH:mm")).FontFamily(MonoMedium).FontSize(7.5f).FontColor(GrisTexte);
                    });

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, "Avancement du plan de traitement");
                        if (data.Mesures.Count == 0)
                        {
                            c.Item().PaddingTop(4).Text("Aucune mesure de traitement definie a ce stade.").FontSize(8.5f).Italic().FontColor(GrisTexte);
                        }
                        else
                        {
                            var termine = data.AvancementParStatut.GetValueOrDefault("Termine", 0);
                            var pctTermine = 100.0 * termine / data.Mesures.Count;
                            c.Item().PaddingTop(6).ShowEntire().Row(row =>
                            {
                                row.ConstantItem(90).Height(90).Svg(AnneauSimple(pctTermine, VertConforme, pctTermine.ToString("F0", CultureInfo.InvariantCulture) + "%")).FitWidth();
                                row.RelativeItem().PaddingLeft(16).AlignMiddle().Row(rr =>
                                {
                                    foreach (var statut in new[] { "ALancer", "EnCours", "Termine" })
                                        Chiffre(rr, data.AvancementParStatut.GetValueOrDefault(statut, 0), LibelleStatutMesure(statut));
                                });
                            });

                            var parAxe = new[] { "Gouvernance", "Protection", "Defense", "Resilience" }
                                .Select(axe =>
                                {
                                    var mesuresAxe = data.Mesures.Where(m => m.Axe == axe).ToList();
                                    var termineAxe = mesuresAxe.Count(m => m.Statut == "Termine");
                                    var pct = mesuresAxe.Count == 0 ? 0.0 : 100.0 * termineAxe / mesuresAxe.Count;
                                    return (Axe: axe, Total: mesuresAxe.Count, Pct: pct);
                                })
                                .Where(x => x.Total > 0)
                                .ToList();
                            if (parAxe.Count > 0)
                            {
                                c.Item().PaddingTop(14).ShowEntire().Column(cc =>
                                {
                                    cc.Item().AlignCenter().Text("Avancement par axe de traitement").FontFamily(SansSemiBold).FontSize(8).FontColor(Encre);
                                    cc.Item().PaddingTop(4).AlignCenter().Width(280).Svg(GraphiqueBarres(
                                        parAxe.Select(a => (a.Axe + " (" + a.Total + ")", a.Pct, VertConforme)).ToList(),
                                        100, "%")).FitWidth();
                                });
                            }
                        }
                    });

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, "Plan de traitement detaille");
                        if (data.Mesures.Count == 0)
                        {
                            c.Item().PaddingTop(4).Text("Aucune mesure de traitement definie a ce stade.").FontSize(8.5f).Italic().FontColor(GrisTexte);
                        }
                        else
                        {
                            c.Item().PaddingTop(4).Table(table =>
                            {
                                table.ColumnsDefinition(cd =>
                                {
                                    cd.RelativeColumn(2.3f); cd.RelativeColumn(1.4f); cd.RelativeColumn(1.3f); cd.RelativeColumn(1.0f); cd.RelativeColumn(0.9f); cd.RelativeColumn(1.0f);
                                });
                                EnteteCellule(table.Cell(), "Mesure de securite");
                                EnteteCellule(table.Cell(), "Scenarios associes");
                                EnteteCellule(table.Cell(), "Responsable");
                                EnteteCellule(table.Cell(), "Cout");
                                EnteteCellule(table.Cell(), "Echeance");
                                EnteteCellule(table.Cell(), "Statut");

                                foreach (var axe in new[] { "Gouvernance", "Protection", "Defense", "Resilience" })
                                {
                                    var mesuresAxe = data.Mesures.Where(m => m.Axe == axe).ToList();
                                    if (mesuresAxe.Count == 0)
                                        continue;

                                    BandeAxe(table, 6, axe);

                                    var alterne = false;
                                    foreach (var m in mesuresAxe)
                                    {
                                        var fond = alterne ? GrisFond : "#FFFFFF";
                                        alterne = !alterne;
                                        table.Cell().Background(fond).BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).Text(m.Description).FontSize(7.8f);
                                        table.Cell().Background(fond).BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).Text(string.Join("; ", m.LibellesScenariosDeRisque)).FontSize(6.8f).FontColor(GrisTexte);
                                        table.Cell().Background(fond).BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).Text(m.Responsable).FontSize(7.5f);
                                        table.Cell().Background(fond).BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).AlignCenter().Text(m.CoutComplexite).FontFamily(MonoMedium).FontSize(7.5f);
                                        table.Cell().Background(fond).BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).Text(m.Echeance ?? "--").FontSize(7.5f);
                                        table.Cell().Background(fond).BorderBottom(0.6f).BorderColor(GrisLigne).Element(e => PastilleStatutMesure(e, m.Statut));
                                    }
                                }
                            });
                        }
                    });

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, "Suivi des risques residuels");
                        if (data.ScenariosDeRisque.Count == 0)
                        {
                            c.Item().PaddingTop(4).Text("Aucun scenario de risque materialise a ce stade.").FontSize(8.5f).Italic().FontColor(GrisTexte);
                        }
                        else
                        {
                            c.Item().PaddingTop(4).Table(table =>
                            {
                                table.ColumnsDefinition(cd =>
                                {
                                    cd.RelativeColumn(2.6f); cd.RelativeColumn(1.2f); cd.RelativeColumn(1.5f); cd.RelativeColumn(1.3f);
                                });
                                EnteteCellule(table.Cell(), "Scenario");
                                EnteteCellule(table.Cell(), "Niveau residuel actuel");
                                EnteteCellule(table.Cell(), "Classe d'acceptation");
                                EnteteCellule(table.Cell(), "Acceptation formelle");

                                var alterne = false;
                                foreach (var s in data.ScenariosDeRisque.OrderByDescending(s => s.NiveauRisqueResiduel))
                                {
                                    var fond = alterne ? GrisFond : "#FFFFFF";
                                    alterne = !alterne;
                                    table.Cell().Background(fond).BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).Text(s.LibelleCouple + " -- " + s.LibelleChemin).FontSize(7.8f);
                                    table.Cell().Background(fond).BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).AlignCenter().Text(s.NiveauRisqueResiduel ?? "non evalue").FontFamily(MonoMedium).FontSize(7.5f).FontColor(CouleurNiveau(s.NiveauRisqueResiduel));
                                    table.Cell().Background(fond).BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).AlignCenter().Text(LibelleClasse(s.ClasseAcceptationResiduelle)).FontSize(7.5f).FontColor(GrisTexte);
                                    table.Cell().Background(fond).BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).AlignCenter().Text(s.AccepteParDirection ? "Acceptee" : "En attente").FontSize(7.5f).FontColor(s.AccepteParDirection ? VertConforme : OrangeAlerte);
                                }
                            });
                        }
                    });
                });

                page.Footer().Column(col =>
                {
                    col.Item().PaddingBottom(4).LineHorizontal(0.6f).LineColor(GrisLigne);
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("EBIOS Risk Manager -- Cadre de suivi").FontFamily(Mono).FontSize(7).FontColor(GrisTexte);
                        row.RelativeItem().AlignRight().Text(DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm")).FontFamily(MonoMedium).FontSize(7).FontColor(GrisTexte);
                    });
                });
            });
        });

        return document.GeneratePdf();
    }
}
