using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static EbiosRM.Api.Modules.Reporting.RapportPdfStyle;

namespace EbiosRM.Api.Modules.Reporting;

/// <summary>
/// Document autonome distinct du rapport d'Atelier 5 -- destiné à la
/// Direction, consolide les 5 ateliers d'une étude validée.
/// </summary>
public sealed class RapportSyntheseGlobalePdfGenerator
{
    public byte[] Generer(RapportSyntheseGlobaleData data)
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
                    col.Item().PaddingTop(2).Text("Synthese globale de l'etude de risque").FontFamily(SerifTitreSemiBold).FontSize(20).FontColor(Encre);
                    col.Item().Text(data.NomEtude).FontFamily(Mono).FontSize(8).FontColor(GrisTexte);
                    col.Item().PaddingTop(10).LineHorizontal(1.4f).LineColor(BleuFrance);
                });

                page.Content().PaddingTop(18).Column(col =>
                {
                    col.Spacing(20);

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, "Synthese executive");

                        var totalControlesExec = data.ConformiteSocle.NombreConforme + data.ConformiteSocle.NombreNonConforme + data.ConformiteSocle.NombreNonApplicable;
                        double? pctConformiteExec = totalControlesExec == 0 ? null : 100.0 * data.ConformiteSocle.NombreConforme / totalControlesExec;
                        var termineExec = data.AvancementPlanParStatut.GetValueOrDefault("Termine", 0);
                        double? pctPlanExec = data.Mesures.Count == 0 ? null : 100.0 * termineExec / data.Mesures.Count;
                        int RangNiveau(string? n) => n switch { "Eleve" => 3, "Moyen" => 2, "Faible" => 1, _ => 0 };
                        var pireNiveauExec = data.ScenariosDeRisque.Count == 0 ? null : data.ScenariosDeRisque.Select(s => s.NiveauRisqueResiduel).OrderByDescending(RangNiveau).First();
                        var nbElevesExec = data.ScenariosDeRisque.Count(s => s.NiveauRisqueResiduel == "Eleve");

                        c.Item().PaddingTop(6).Row(row =>
                        {
                            row.RelativeItem().Column(cc =>
                            {
                                cc.Item().Text(pireNiveauExec ?? "--").FontFamily(SerifTitreSemiBold).FontSize(22).FontColor(CouleurNiveau(pireNiveauExec));
                                cc.Item().Text("Posture globale (pire risque residuel)").FontSize(7.5f).FontColor(GrisTexte);
                            });
                            row.RelativeItem().Column(cc =>
                            {
                                cc.Item().Text(pctConformiteExec.HasValue ? pctConformiteExec.Value.ToString("F0", CultureInfo.InvariantCulture) + "%" : "N/A").FontFamily(SerifTitreSemiBold).FontSize(22).FontColor(BleuFrance);
                                cc.Item().Text("Conformite du socle de securite").FontSize(7.5f).FontColor(GrisTexte);
                            });
                            row.RelativeItem().Column(cc =>
                            {
                                cc.Item().Text(pctPlanExec.HasValue ? pctPlanExec.Value.ToString("F0", CultureInfo.InvariantCulture) + "%" : "N/A").FontFamily(SerifTitreSemiBold).FontSize(22).FontColor(BleuFrance);
                                cc.Item().Text("Plan de traitement termine").FontSize(7.5f).FontColor(GrisTexte);
                            });
                            row.RelativeItem().Column(cc =>
                            {
                                cc.Item().Text(nbElevesExec.ToString()).FontFamily(SerifTitreSemiBold).FontSize(22).FontColor(nbElevesExec > 0 ? RougeAlerte : VertConforme);
                                cc.Item().Text("Risque(s) residuel(s) eleve(s)").FontSize(7.5f).FontColor(GrisTexte);
                            });
                        });
                    });

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, "Identite de l'etude");
                        c.Item().PaddingTop(4).Text(t =>
                        {
                            t.Span("Perimetre : ").FontFamily(SansSemiBold).FontSize(8.5f);
                            t.Span(data.Perimetre).FontSize(8.5f);
                        });
                        c.Item().Text(t =>
                        {
                            t.Span("Mission : ").FontFamily(SansSemiBold).FontSize(8.5f);
                            t.Span(data.Mission).FontSize(8.5f);
                        });
                        c.Item().PaddingTop(2).Text("Synthese generee le " + data.DateSynthese.ToString("dd/MM/yyyy")).FontFamily(MonoMedium).FontSize(7.5f).FontColor(GrisTexte);
                    });

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, "Chiffres cles");
                        c.Item().PaddingTop(6).Row(row =>
                        {
                            Chiffre(row, data.ChiffresCles.NombreValeursMetier, "Valeurs metier");
                            Chiffre(row, data.ChiffresCles.NombreBiensSupport, "Biens support");
                            Chiffre(row, data.ChiffresCles.NombreEvenementsRedoutes, "Evenements redoutes");
                        });
                        c.Item().PaddingTop(8).Row(row =>
                        {
                            Chiffre(row, data.ChiffresCles.NombrePartiesPrenantesCritiques, "Parties prenantes critiques / " + data.ChiffresCles.NombrePartiesPrenantes);
                            Chiffre(row, data.ChiffresCles.NombreScenariosStrategiques, "Scenarios strategiques");
                            Chiffre(row, data.ChiffresCles.NombreScenariosOperationnels, "Scenarios operationnels");
                        });
                        if (data.ChiffresCles.NomsPartiesPrenantesCritiques.Count > 0)
                        {
                            c.Item().PaddingTop(6).Text(t =>
                            {
                                t.Span("Parties prenantes critiques (zones controle / danger) : ").FontFamily(SansSemiBold).FontSize(7.5f).FontColor(GrisTexte);
                                t.Span(string.Join(", ", data.ChiffresCles.NomsPartiesPrenantesCritiques)).FontSize(7.5f).FontColor(GrisTexte);
                            });
                        }
                    });

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, "Socle de securite -- conformite ISO/IEC 27001:2022");
                        var socle = data.ConformiteSocle;
                        var totalControles = socle.NombreConforme + socle.NombreNonConforme + socle.NombreNonApplicable;
                        if (totalControles == 0)
                        {
                            c.Item().PaddingTop(4).Text("Aucun controle renseigne dans le socle de securite.").FontSize(8.5f).Italic().FontColor(GrisTexte);
                        }
                        else
                        {
                            var pctConforme = 100.0 * socle.NombreConforme / totalControles;
                            c.Item().PaddingTop(6).ShowEntire().Row(row =>
                            {
                                row.ConstantItem(100).Height(100).Svg(AnneauMultiSegments(
                                    new List<(double, string)>
                                    {
                                        (socle.NombreConforme, VertConforme),
                                        (socle.NombreNonConforme, RougeAlerte),
                                        (socle.NombreNonApplicable, GrisLigne),
                                    },
                                    pctConforme.ToString("F0", CultureInfo.InvariantCulture) + "%")).FitWidth();
                                row.RelativeItem().PaddingLeft(16).Column(cc =>
                                {
                                    cc.Item().Row(r => Legende(r, VertConforme, socle.NombreConforme + " conforme(s)"));
                                    cc.Item().PaddingTop(3).Row(r => Legende(r, RougeAlerte, socle.NombreNonConforme + " non conforme(s)"));
                                    cc.Item().PaddingTop(3).Row(r => Legende(r, GrisLigne, socle.NombreNonApplicable + " non applicable(s)"));
                                    cc.Item().PaddingTop(3).Text(totalControles + " controle(s) evalue(s) au total.").FontSize(7.5f).FontColor(GrisTexte);
                                });
                            });
                            if (socle.ParTheme.Count > 0)
                            {
                                c.Item().PaddingTop(14).ShowEntire().Row(row =>
                                {
                                    row.RelativeItem().Column(cc =>
                                    {
                                        cc.Item().AlignCenter().Text("Taux de conformite par theme").FontFamily(SansSemiBold).FontSize(8).FontColor(Encre);
                                        cc.Item().PaddingTop(4).Svg(GraphiqueBarres(
                                            socle.ParTheme.Select(t => (t.Theme, t.TauxConformitePct, BleuFrance)).ToList(),
                                            100, "%")).FitWidth();
                                    });
                                    row.RelativeItem().Column(cc =>
                                    {
                                        cc.Item().AlignCenter().Text("Cartographie de conformite par theme").FontFamily(SansSemiBold).FontSize(8).FontColor(Encre);
                                        cc.Item().PaddingTop(4).Svg(GraphiqueRadar(
                                            socle.ParTheme.Select(t => (t.Theme, t.TauxConformitePct)).ToList(),
                                            BleuFrance)).FitWidth();
                                    });
                                });
                                c.Item().PaddingTop(10).ShowEntire().Column(cc =>
                                {
                                    cc.Item().AlignCenter().Text("Repartition des controles par etat").FontFamily(SansSemiBold).FontSize(8).FontColor(Encre);
                                    cc.Item().PaddingTop(4).AlignCenter().Width(220).Svg(GraphiqueBarres(
                                        new List<(string, double, string)>
                                        {
                                            ("Conforme", socle.NombreConforme, VertConforme),
                                            ("Non conforme", socle.NombreNonConforme, RougeAlerte),
                                            ("Non applicable", socle.NombreNonApplicable, GrisTexte),
                                        },
                                        Math.Max(totalControles, 1), "")).FitWidth();
                                });
                            }
                            if (socle.ControlesNonConformes.Count > 0)
                            {
                                c.Item().PaddingTop(10).Text("Controles non conformes a traiter en priorite :").FontFamily(SansSemiBold).FontSize(8).FontColor(Encre);
                                c.Item().PaddingTop(3).Column(cc =>
                                {
                                    foreach (var ctrl in socle.ControlesNonConformes)
                                    {
                                        cc.Item().PaddingTop(2).Text(t =>
                                        {
                                            if (!string.IsNullOrWhiteSpace(ctrl.CodeControle))
                                                t.Span(ctrl.CodeControle + " -- ").FontFamily(Mono).FontSize(7.5f).FontColor(RougeAlerte);
                                            t.Span(ctrl.Nom).FontSize(8).FontColor(GrisTexte);
                                            if (!string.IsNullOrWhiteSpace(ctrl.EtatActuel))
                                                t.Span("  (" + ctrl.EtatActuel + ")").FontSize(7.5f).Italic().FontColor(GrisTexte);
                                        });
                                    }
                                });
                            }
                        }
                    });

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, "Grille de determination du niveau de risque");
                        c.Item().PaddingTop(4).Text("Croisement Gravite (evenement redoute vise) x Vraisemblance (scenario operationnel), seuils par defaut du projet ajustables. La cartographie ci-dessous indique, pour chaque scenario, le calcul exact qui a produit son niveau.").FontSize(8).Italic().FontColor(GrisTexte);
                        c.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.ConstantColumn(110);
                                cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn();
                            });
                            table.Cell().Background(GrisFond).Padding(5).Text("Gravite \\ Vraisemblance").FontFamily(MonoMedium).FontSize(6.5f).FontColor(GrisTexte);
                            foreach (var v in new[] { "V1", "V2", "V3", "V4" })
                                EnteteCellule(table.Cell(), v);

                            void LigneRisque(string label, string v1, string v2, string v3, string v4)
                            {
                                table.Cell().Background(BleuFranceClair).Padding(4).Text(label).FontFamily(MonoMedium).FontSize(7f).FontColor(BleuFrance);
                                foreach (var v in new[] { v1, v2, v3, v4 })
                                    table.Cell().BorderBottom(0.6f).BorderColor(GrisLigne).PaddingVertical(4).AlignCenter().Text(v).FontFamily(MonoMedium).FontSize(7.5f).FontColor(CouleurNiveau(v));
                            }
                            LigneRisque("1", "Faible", "Faible", "Moyen", "Moyen");
                            LigneRisque("2", "Faible", "Faible", "Moyen", "Eleve");
                            LigneRisque("3", "Faible", "Moyen", "Eleve", "Eleve");
                            LigneRisque("4", "Faible", "Moyen", "Eleve", "Eleve");
                        });
                    });

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, "Cartographie des risques -- avant / apres traitement");
                        if (data.ScenariosDeRisque.Count == 0)
                        {
                            c.Item().PaddingTop(4).Text("Aucun scenario de risque.").FontSize(8.5f).Italic().FontColor(GrisTexte);
                        }
                        else
                        {
                            CartographieCompleteAvecLegende(c, data.ScenariosDeRisque);

                            c.Item().PaddingTop(10).Table(table =>
                            {
                                table.ColumnsDefinition(cd =>
                                {
                                    cd.RelativeColumn(2.4f); cd.RelativeColumn(1.6f); cd.RelativeColumn(1.6f); cd.RelativeColumn(1.6f);
                                });
                                EnteteCellule(table.Cell(), "Scenario");
                                EnteteCellule(table.Cell(), "Initial (G x V)");
                                EnteteCellule(table.Cell(), "Residuel (G x V)");
                                EnteteCellule(table.Cell(), "Classe d'acceptation");

                                foreach (var s in data.ScenariosDeRisque.OrderByDescending(s => s.NiveauRisqueResiduel))
                                {
                                    table.Cell().BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).Text(s.LibelleCouple + " -- " + s.LibelleChemin).FontSize(7.8f);
                                    table.Cell().BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).Column(cc =>
                                    {
                                        cc.Item().AlignCenter().Text("G" + s.Gravite + " x " + (s.VraisemblanceInitiale ?? "?")).FontFamily(Mono).FontSize(7).FontColor(GrisTexte);
                                        cc.Item().AlignCenter().Text(s.NiveauRisqueInitial ?? "--").FontFamily(MonoMedium).FontSize(8).FontColor(CouleurNiveau(s.NiveauRisqueInitial));
                                        if (s.NiveauInitialEstJugementExpert)
                                            cc.Item().AlignCenter().Text("(jugement d'expert)").FontSize(6).Italic().FontColor(GrisTexte);
                                    });
                                    table.Cell().BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).Column(cc =>
                                    {
                                        if (s.GraviteResiduelle.HasValue)
                                        {
                                            cc.Item().AlignCenter().Text("G" + s.GraviteResiduelle + " x " + (s.VraisemblanceResiduelle ?? "?")).FontFamily(Mono).FontSize(7).FontColor(GrisTexte);
                                            cc.Item().AlignCenter().Text(s.NiveauRisqueResiduel ?? "--").FontFamily(MonoMedium).FontSize(8).FontColor(CouleurNiveau(s.NiveauRisqueResiduel));
                                            if (s.NiveauResiduelEstJugementExpert)
                                                cc.Item().AlignCenter().Text("(jugement d'expert)").FontSize(6).Italic().FontColor(GrisTexte);
                                        }
                                        else
                                        {
                                            cc.Item().AlignCenter().Text("non evalue").FontSize(7.5f).Italic().FontColor(GrisTexte);
                                        }
                                    });
                                    table.Cell().BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).AlignCenter().Text(LibelleClasse(s.ClasseAcceptationResiduelle)).FontSize(7.5f).FontColor(GrisTexte);
                                }
                            });
                        }
                    });

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, "Avancement du plan de traitement du risque");
                        if (data.Mesures.Count == 0)
                        {
                            c.Item().PaddingTop(4).Text("Aucune mesure definie.").FontSize(8.5f).Italic().FontColor(GrisTexte);
                        }
                        else
                        {
                            var termine = data.AvancementPlanParStatut.GetValueOrDefault("Termine", 0);
                            var pctTermine = 100.0 * termine / data.Mesures.Count;
                            c.Item().PaddingTop(6).ShowEntire().Row(row =>
                            {
                                row.ConstantItem(90).Height(90).Svg(AnneauSimple(pctTermine, VertConforme, pctTermine.ToString("F0", CultureInfo.InvariantCulture) + "%")).FitWidth();
                                row.RelativeItem().PaddingLeft(16).AlignMiddle().Row(rr =>
                                {
                                    foreach (var statut in new[] { "ALancer", "EnCours", "Termine" })
                                        Chiffre(rr, data.AvancementPlanParStatut.GetValueOrDefault(statut, 0), LibelleStatutMesure(statut));
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
                                    cc.Item().AlignCenter().Text("Avancement du plan par axe de traitement").FontFamily(SansSemiBold).FontSize(8).FontColor(Encre);
                                    cc.Item().PaddingTop(4).AlignCenter().Width(280).Svg(GraphiqueBarres(
                                        parAxe.Select(a => (a.Axe + " (" + a.Total + ")", a.Pct, VertConforme)).ToList(),
                                        100, "%")).FitWidth();
                                });
                            }
                        }
                    });

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, "Registre d'acceptation des risques residuels");
                        var acceptes = data.ScenariosDeRisque.Where(s => s.AccepteParDirection).ToList();
                        if (acceptes.Count == 0)
                        {
                            c.Item().PaddingTop(4).Text("Aucun risque residuel accepte formellement.").FontSize(8.5f).Italic().FontColor(GrisTexte);
                        }
                        else
                        {
                            foreach (var s in acceptes)
                            {
                                c.Item().PaddingTop(8).Column(sc =>
                                {
                                    sc.Item().Row(row =>
                                    {
                                        row.RelativeItem().Text(s.LibelleCouple + " -- " + s.LibelleChemin).FontFamily(SansSemiBold).FontSize(8.5f);
                                        row.ConstantItem(70).AlignRight().Text(s.NiveauRisqueResiduel ?? "--").FontFamily(MonoMedium).FontSize(8).FontColor(CouleurNiveau(s.NiveauRisqueResiduel));
                                    });
                                    sc.Item().Text("Proprietaire : " + s.NomProprietaireRisque + " -- Validateur : " + s.NomValidateurSecurite).FontSize(7.5f).FontColor(GrisTexte);
                                    if (s.JustificationAcceptation is not null)
                                        sc.Item().Text(s.JustificationAcceptation).FontSize(7.5f).Italic().FontColor(GrisTexte);
                                });
                            }
                        }
                    });
                });

                page.Footer().Column(col =>
                {
                    col.Item().PaddingBottom(4).LineHorizontal(0.6f).LineColor(GrisLigne);
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("EBIOS Risk Manager -- Synthese globale").FontFamily(Mono).FontSize(7).FontColor(GrisTexte);
                        row.RelativeItem().AlignRight().Text(DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm")).FontFamily(MonoMedium).FontSize(7).FontColor(GrisTexte);
                    });
                });
            });
        });

        return document.GeneratePdf();
    }
}
