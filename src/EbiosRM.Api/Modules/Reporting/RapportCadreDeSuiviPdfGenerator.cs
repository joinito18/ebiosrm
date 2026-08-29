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
    public byte[] Generer(RapportCadreDeSuiviData data, bool anglais = false)
    {
        string T(string fr, string en) => anglais ? en : fr;
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
                    col.Item().PaddingTop(2).Text(T("Cadre de suivi", "Monitoring framework")).FontFamily(SerifTitreSemiBold).FontSize(20).FontColor(Encre);
                    col.Item().Text(data.NomEtude).FontFamily(Mono).FontSize(8).FontColor(GrisTexte);
                    col.Item().PaddingTop(10).LineHorizontal(1.4f).LineColor(BleuFrance);
                });

                page.Content().PaddingTop(18).Column(col =>
                {
                    col.Spacing(20);

                    col.Item().Text(anglais ? "This document reflects the current state of the treatment plan and residual risks -- unlike the workshop reports, it is not frozen and can be regenerated at any time to track the actual progress of the security measures." : "Ce document reflète l'état courant du plan de traitement et des risques résiduels -- contrairement aux rapports d'atelier, il n'est pas figé et peut être régénéré à tout moment pour suivre la progression réelle des mesures de sécurité.").FontSize(8.5f).Italic().FontColor(GrisTexte);

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, T("Identite de l'etude", "Study identity"));
                        c.Item().PaddingTop(4).Text(t =>
                        {
                            t.Span(T("Perimetre : ", "Scope: ")).FontFamily(SansSemiBold).FontSize(8.5f);
                            t.Span(data.Perimetre).FontSize(8.5f);
                        });
                        c.Item().PaddingTop(2).Text(T("Cadre de suivi genere le ", "Monitoring framework generated on ") + data.DateGeneration.ToString(anglais ? "yyyy-MM-dd HH:mm" : "dd/MM/yyyy 'a' HH:mm")).FontFamily(MonoMedium).FontSize(7.5f).FontColor(GrisTexte);
                    });

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, T("Avancement du plan de traitement", "Treatment plan progress"));
                        if (data.Mesures.Count == 0)
                        {
                            c.Item().PaddingTop(4).Text(T("Aucune mesure de traitement definie a ce stade.", "No treatment measure defined at this stage.")).FontSize(8.5f).Italic().FontColor(GrisTexte);
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
                                        Chiffre(rr, data.AvancementParStatut.GetValueOrDefault(statut, 0), LibelleStatutMesure(statut, anglais));
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
                            if (parAxe.Count >= 2)
                            {
                                c.Item().PaddingTop(14).ShowEntire().Column(cc =>
                                {
                                    cc.Item().AlignCenter().Text(T("Avancement par axe de traitement", "Progress by treatment area")).FontFamily(SansSemiBold).FontSize(8).FontColor(Encre);
                                    cc.Item().PaddingTop(4).AlignCenter().Height(150).Svg(GraphiqueBarres(
                                        parAxe.Select(a => (LibellesRapport.Axe(a.Axe, anglais) + " (" + a.Total + ")", a.Pct, VertConforme)).ToList(),
                                        100, "%")).FitHeight();
                                });
                            }
                        }
                    });

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, T("Plan de traitement detaille", "Detailed treatment plan"));
                        if (data.Mesures.Count == 0)
                        {
                            c.Item().PaddingTop(4).Text(T("Aucune mesure de traitement definie a ce stade.", "No treatment measure defined at this stage.")).FontSize(8.5f).Italic().FontColor(GrisTexte);
                        }
                        else
                        {
                            c.Item().PaddingTop(4).Table(table =>
                            {
                                table.ColumnsDefinition(cd =>
                                {
                                    cd.RelativeColumn(2.3f); cd.RelativeColumn(1.4f); cd.RelativeColumn(1.3f); cd.RelativeColumn(1.0f); cd.RelativeColumn(0.9f); cd.RelativeColumn(1.0f);
                                });
                                EnteteCellule(table.Cell(), T("Mesure de securite", "Security measure"));
                                EnteteCellule(table.Cell(), T("Scenarios associes", "Associated scenarios"));
                                EnteteCellule(table.Cell(), T("Responsable", "Owner"));
                                EnteteCellule(table.Cell(), T("Cout", "Cost"));
                                EnteteCellule(table.Cell(), T("Echeance", "Deadline"));
                                EnteteCellule(table.Cell(), T("Statut", "Status"));

                                foreach (var axe in new[] { "Gouvernance", "Protection", "Defense", "Resilience" })
                                {
                                    var mesuresAxe = data.Mesures.Where(m => m.Axe == axe).ToList();
                                    if (mesuresAxe.Count == 0)
                                        continue;

                                    BandeAxe(table, 6, axe, anglais);

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
                                        table.Cell().Background(fond).BorderBottom(0.6f).BorderColor(GrisLigne).Element(e => PastilleStatutMesure(e, m.Statut, anglais));
                                    }
                                }
                            });
                        }
                    });

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, T("Suivi des risques residuels", "Residual-risk tracking"));
                        if (data.ScenariosDeRisque.Count == 0)
                        {
                            c.Item().PaddingTop(4).Text(T("Aucun scenario de risque materialise a ce stade.", "No risk scenario materialised at this stage.")).FontSize(8.5f).Italic().FontColor(GrisTexte);
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
                                EnteteCellule(table.Cell(), T("Niveau residuel actuel", "Current residual level"));
                                EnteteCellule(table.Cell(), T("Classe d'acceptation", "Acceptance class"));
                                EnteteCellule(table.Cell(), T("Acceptation formelle", "Formal acceptance"));

                                var alterne = false;
                                foreach (var s in data.ScenariosDeRisque.OrderByDescending(s => s.NiveauRisqueResiduel))
                                {
                                    var fond = alterne ? GrisFond : "#FFFFFF";
                                    alterne = !alterne;
                                    table.Cell().Background(fond).BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).Text(s.LibelleCouple + " -- " + s.LibelleChemin).FontSize(7.8f);
                                    table.Cell().Background(fond).BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).AlignCenter().Text(s.NiveauRisqueResiduel is null ? T("non evalue", "not assessed") : LibellesRapport.NiveauRisque(s.NiveauRisqueResiduel, anglais)).FontFamily(MonoMedium).FontSize(7.5f).FontColor(CouleurNiveau(s.NiveauRisqueResiduel));
                                    table.Cell().Background(fond).BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).AlignCenter().Text(LibelleClasse(s.ClasseAcceptationResiduelle, anglais)).FontSize(7.5f).FontColor(GrisTexte);
                                    table.Cell().Background(fond).BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).AlignCenter().Text(s.AccepteParDirection ? T("Acceptee", "Accepted") : T("En attente", "Pending")).FontSize(7.5f).FontColor(s.AccepteParDirection ? VertConforme : OrangeAlerte);
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
                        row.RelativeItem().Text(T("EBIOS Risk Manager -- Cadre de suivi", "EBIOS Risk Manager -- Monitoring framework")).FontFamily(Mono).FontSize(7).FontColor(GrisTexte);
                        row.RelativeItem().AlignRight().Text(DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm")).FontFamily(MonoMedium).FontSize(7).FontColor(GrisTexte);
                    });
                });
            });
        });

        return document.GeneratePdf();
    }
}
