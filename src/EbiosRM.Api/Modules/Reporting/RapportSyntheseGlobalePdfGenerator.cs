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
    public byte[] Generer(RapportSyntheseGlobaleData data, bool anglais = false)
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
                    col.Item().PaddingTop(2).Text(T("Synthese globale de l'etude de risque", "Global summary of the risk study")).FontFamily(SerifTitreSemiBold).FontSize(20).FontColor(Encre);
                    col.Item().Text(data.NomEtude).FontFamily(Mono).FontSize(8).FontColor(GrisTexte);
                    col.Item().PaddingTop(10).LineHorizontal(1.4f).LineColor(BleuFrance);
                });

                page.Content().PaddingTop(18).Column(col =>
                {
                    col.Spacing(20);

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, T("Synthese executive", "Executive summary"));

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
                                cc.Item().Text(LibellesRapport.NiveauRisque(pireNiveauExec, anglais)).FontFamily(SerifTitreSemiBold).FontSize(22).FontColor(CouleurNiveau(pireNiveauExec));
                                cc.Item().Text(T("Posture globale (pire risque residuel)", "Overall posture (worst residual risk)")).FontSize(7.5f).FontColor(GrisTexte);
                            });
                            row.RelativeItem().Column(cc =>
                            {
                                cc.Item().Text(pctConformiteExec.HasValue ? pctConformiteExec.Value.ToString("F0", CultureInfo.InvariantCulture) + "%" : "N/A").FontFamily(SerifTitreSemiBold).FontSize(22).FontColor(BleuFrance);
                                cc.Item().Text(T("Conformite du socle de securite", "Security-baseline compliance")).FontSize(7.5f).FontColor(GrisTexte);
                            });
                            row.RelativeItem().Column(cc =>
                            {
                                cc.Item().Text(pctPlanExec.HasValue ? pctPlanExec.Value.ToString("F0", CultureInfo.InvariantCulture) + "%" : "N/A").FontFamily(SerifTitreSemiBold).FontSize(22).FontColor(BleuFrance);
                                cc.Item().Text(T("Plan de traitement termine", "Treatment plan completed")).FontSize(7.5f).FontColor(GrisTexte);
                            });
                            row.RelativeItem().Column(cc =>
                            {
                                cc.Item().Text(nbElevesExec.ToString()).FontFamily(SerifTitreSemiBold).FontSize(22).FontColor(nbElevesExec > 0 ? RougeAlerte : VertConforme);
                                cc.Item().Text(T("Risque(s) residuel(s) eleve(s)", "High residual risk(s)")).FontSize(7.5f).FontColor(GrisTexte);
                            });
                        });
                    });

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, T("Identite de l'etude", "Study identity"));
                        c.Item().PaddingTop(4).Text(t =>
                        {
                            t.Span(T("Perimetre : ", "Scope: ")).FontFamily(SansSemiBold).FontSize(8.5f);
                            t.Span(data.Perimetre).FontSize(8.5f);
                        });
                        c.Item().Text(t =>
                        {
                            t.Span(T("Mission : ", "Mission: ")).FontFamily(SansSemiBold).FontSize(8.5f);
                            t.Span(data.Mission).FontSize(8.5f);
                        });
                        c.Item().PaddingTop(2).Text(T("Synthese generee le ", "Summary generated on ") + data.DateSynthese.ToString(anglais ? "yyyy-MM-dd" : "dd/MM/yyyy")).FontFamily(MonoMedium).FontSize(7.5f).FontColor(GrisTexte);
                    });

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, T("Chiffres cles", "Key figures"));
                        c.Item().PaddingTop(6).Row(row =>
                        {
                            Chiffre(row, data.ChiffresCles.NombreValeursMetier, T("Valeurs metier", "Business values"));
                            Chiffre(row, data.ChiffresCles.NombreBiensSupport, T("Biens support", "Supporting assets"));
                            Chiffre(row, data.ChiffresCles.NombreEvenementsRedoutes, T("Evenements redoutes", "Feared events"));
                        });
                        c.Item().PaddingTop(8).Row(row =>
                        {
                            Chiffre(row, data.ChiffresCles.NombrePartiesPrenantesCritiques, T("Parties prenantes critiques / ", "Critical stakeholders / ") + data.ChiffresCles.NombrePartiesPrenantes);
                            Chiffre(row, data.ChiffresCles.NombreScenariosStrategiques, T("Scenarios strategiques", "Strategic scenarios"));
                            Chiffre(row, data.ChiffresCles.NombreScenariosOperationnels, T("Scenarios operationnels", "Operational scenarios"));
                        });
                        if (data.ChiffresCles.NomsPartiesPrenantesCritiques.Count > 0)
                        {
                            c.Item().PaddingTop(6).Text(t =>
                            {
                                t.Span(T("Parties prenantes critiques (zones controle / danger) : ", "Critical stakeholders (control / danger zones): ")).FontFamily(SansSemiBold).FontSize(7.5f).FontColor(GrisTexte);
                                t.Span(string.Join(", ", data.ChiffresCles.NomsPartiesPrenantesCritiques)).FontSize(7.5f).FontColor(GrisTexte);
                            });
                        }
                    });

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, T("Socle de securite -- conformite ISO/IEC 27001:2022", "Security baseline -- ISO/IEC 27001:2022 compliance"));
                        var socle = data.ConformiteSocle;
                        var totalControles = socle.NombreConforme + socle.NombreNonConforme + socle.NombreNonApplicable;
                        if (totalControles == 0)
                        {
                            c.Item().PaddingTop(4).Text(T("Aucun controle renseigne dans le socle de securite.", "No control recorded in the security baseline.")).FontSize(8.5f).Italic().FontColor(GrisTexte);
                        }
                        else
                        {
                            var pctConforme = 100.0 * socle.NombreConforme / totalControles;
                            c.Item().PaddingTop(6).ShowEntire().Row(row =>
                            {
                                row.ConstantItem(130).Height(130).AlignMiddle().Svg(AnneauMultiSegments(
                                    new List<(double, string)>
                                    {
                                        (socle.NombreConforme, VertConforme),
                                        (socle.NombreNonConforme, RougeAlerte),
                                        (socle.NombreNonApplicable, GrisLigne),
                                    },
                                    pctConforme.ToString("F0", CultureInfo.InvariantCulture) + "%")).FitWidth();
                                row.RelativeItem().PaddingLeft(24).AlignMiddle().Column(cc =>
                                {
                                    cc.Item().Text(T("Socle conforme", "Baseline compliance")).FontFamily(SansSemiBold).FontSize(9).FontColor(Encre);
                                    cc.Item().PaddingTop(6).Row(r => Legende(r, VertConforme, socle.NombreConforme + T(" conforme(s)", " compliant")));
                                    cc.Item().PaddingTop(3).Row(r => Legende(r, RougeAlerte, socle.NombreNonConforme + T(" non conforme(s)", " non-compliant")));
                                    cc.Item().PaddingTop(3).Row(r => Legende(r, GrisLigne, socle.NombreNonApplicable + T(" non applicable(s)", " not applicable")));
                                    cc.Item().PaddingTop(4).Text(totalControles + T(" controle(s) evalue(s) au total.", " control(s) assessed in total.")).FontSize(7.5f).FontColor(GrisTexte);
                                });
                            });
                            // Repartition des controles par etat -- utile des qu'il y a >= 2 etats
                            // representes (sinon l'anneau ci-dessus dit deja tout).
                            var etatsRepresentes = new[] { socle.NombreConforme, socle.NombreNonConforme, socle.NombreNonApplicable }.Count(x => x > 0);
                            if (etatsRepresentes >= 2)
                            {
                                c.Item().PaddingTop(12).ShowEntire().Column(cc =>
                                {
                                    cc.Item().AlignCenter().Text(T("Repartition des controles par etat", "Control breakdown by state")).FontFamily(SansSemiBold).FontSize(8).FontColor(Encre);
                                    cc.Item().PaddingTop(4).AlignCenter().Height(150).Svg(GraphiqueBarres(
                                        new List<(string, double, string)>
                                        {
                                            (T("Conforme", "Compliant"), socle.NombreConforme, VertConforme),
                                            (T("Non conforme", "Non-compliant"), socle.NombreNonConforme, RougeAlerte),
                                            (T("Non applicable", "Not applicable"), socle.NombreNonApplicable, GrisTexte),
                                        },
                                        Math.Max(totalControles, 1), "")).FitHeight();
                                });
                            }
                            // Taux de conformite par theme -- une seule barre n'apprend rien.
                            if (socle.ParTheme.Count >= 2)
                            {
                                c.Item().PaddingTop(12).ShowEntire().Column(cc =>
                                {
                                    cc.Item().AlignCenter().Text(T("Taux de conformite par theme", "Compliance rate by theme")).FontFamily(SansSemiBold).FontSize(8).FontColor(Encre);
                                    cc.Item().PaddingTop(4).AlignCenter().Height(150).Svg(GraphiqueBarres(
                                        socle.ParTheme.Select(t => (t.Theme, t.TauxConformitePct, BleuFrance)).ToList(),
                                        100, "%")).FitHeight();
                                });
                            }
                            if (socle.ControlesNonConformes.Count > 0)
                            {
                                c.Item().PaddingTop(10).Text(T("Controles non conformes a traiter en priorite :", "Non-compliant controls to address first:")).FontFamily(SansSemiBold).FontSize(8).FontColor(Encre);
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
                        SectionTitre(c, T("Grille de determination du niveau de risque", "Risk-level determination grid"));
                        c.Item().PaddingTop(4).Text(anglais ? "Cross of Severity (targeted feared event) x Likelihood (operational scenario), project default thresholds are adjustable. The mapping below shows, for each scenario, the exact calculation that produced its level." : "Croisement Gravite (evenement redoute vise) x Vraisemblance (scenario operationnel), seuils par defaut du projet ajustables. La cartographie ci-dessous indique, pour chaque scenario, le calcul exact qui a produit son niveau.").FontSize(8).Italic().FontColor(GrisTexte);
                        c.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.ConstantColumn(110);
                                cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn();
                            });
                            table.Cell().Background(GrisFond).Padding(5).Text(T("Gravite \\ Vraisemblance", "Severity \\ Likelihood")).FontFamily(MonoMedium).FontSize(6.5f).FontColor(GrisTexte);
                            foreach (var v in new[] { "V1", "V2", "V3", "V4" })
                                EnteteCellule(table.Cell(), v);

                            void LigneRisque(string label, string v1, string v2, string v3, string v4)
                            {
                                table.Cell().Background(BleuFranceClair).Padding(4).Text(label).FontFamily(MonoMedium).FontSize(7f).FontColor(BleuFrance);
                                foreach (var v in new[] { v1, v2, v3, v4 })
                                    table.Cell().BorderBottom(0.6f).BorderColor(GrisLigne).PaddingVertical(4).AlignCenter().Text(LibellesRapport.NiveauRisque(v, anglais)).FontFamily(MonoMedium).FontSize(7.5f).FontColor(CouleurNiveau(v));
                            }
                            LigneRisque("1", "Faible", "Faible", "Moyen", "Moyen");
                            LigneRisque("2", "Faible", "Faible", "Moyen", "Eleve");
                            LigneRisque("3", "Faible", "Moyen", "Eleve", "Eleve");
                            LigneRisque("4", "Faible", "Moyen", "Eleve", "Eleve");
                        });
                    });

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, T("Cartographie des risques -- avant / apres traitement", "Risk mapping -- before / after treatment"));
                        if (data.ScenariosDeRisque.Count == 0)
                        {
                            c.Item().PaddingTop(4).Text(T("Aucun scenario de risque.", "No risk scenario.")).FontSize(8.5f).Italic().FontColor(GrisTexte);
                        }
                        else
                        {
                            CartographieCompleteAvecLegende(c, data.ScenariosDeRisque, anglais, tailleCase: 42);

                            c.Item().PaddingTop(10).Table(table =>
                            {
                                table.ColumnsDefinition(cd =>
                                {
                                    cd.RelativeColumn(2.4f); cd.RelativeColumn(1.6f); cd.RelativeColumn(1.6f); cd.RelativeColumn(1.6f);
                                });
                                EnteteCellule(table.Cell(), "Scenario");
                                EnteteCellule(table.Cell(), T("Initial (G x V)", "Initial (S x L)"));
                                EnteteCellule(table.Cell(), T("Residuel (G x V)", "Residual (S x L)"));
                                EnteteCellule(table.Cell(), T("Classe d'acceptation", "Acceptance class"));

                                foreach (var s in data.ScenariosDeRisque.OrderByDescending(s => s.NiveauRisqueResiduel))
                                {
                                    table.Cell().BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).Text(s.LibelleCouple + " -- " + s.LibelleChemin).FontSize(7.8f);
                                    table.Cell().BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).Column(cc =>
                                    {
                                        cc.Item().AlignCenter().Text("G" + s.Gravite + " x " + (s.VraisemblanceInitiale ?? "?")).FontFamily(Mono).FontSize(7).FontColor(GrisTexte);
                                        cc.Item().AlignCenter().Text(LibellesRapport.NiveauRisque(s.NiveauRisqueInitial, anglais)).FontFamily(MonoMedium).FontSize(8).FontColor(CouleurNiveau(s.NiveauRisqueInitial));
                                        if (s.NiveauInitialEstJugementExpert)
                                            cc.Item().AlignCenter().Text(T("(jugement d'expert)", "(expert judgement)")).FontSize(6).Italic().FontColor(GrisTexte);
                                    });
                                    table.Cell().BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).Column(cc =>
                                    {
                                        if (s.GraviteResiduelle.HasValue)
                                        {
                                            cc.Item().AlignCenter().Text("G" + s.GraviteResiduelle + " x " + (s.VraisemblanceResiduelle ?? "?")).FontFamily(Mono).FontSize(7).FontColor(GrisTexte);
                                            cc.Item().AlignCenter().Text(LibellesRapport.NiveauRisque(s.NiveauRisqueResiduel, anglais)).FontFamily(MonoMedium).FontSize(8).FontColor(CouleurNiveau(s.NiveauRisqueResiduel));
                                            if (s.NiveauResiduelEstJugementExpert)
                                                cc.Item().AlignCenter().Text(T("(jugement d'expert)", "(expert judgement)")).FontSize(6).Italic().FontColor(GrisTexte);
                                        }
                                        else
                                        {
                                            cc.Item().AlignCenter().Text(T("non evalue", "not assessed")).FontSize(7.5f).Italic().FontColor(GrisTexte);
                                        }
                                    });
                                    table.Cell().BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).AlignCenter().Text(LibelleClasse(s.ClasseAcceptationResiduelle, anglais)).FontSize(7.5f).FontColor(GrisTexte);
                                }
                            });
                        }
                    });

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, T("Avancement du plan de traitement du risque", "Risk treatment plan progress"));
                        if (data.Mesures.Count == 0)
                        {
                            c.Item().PaddingTop(4).Text(T("Aucune mesure definie.", "No measure defined.")).FontSize(8.5f).Italic().FontColor(GrisTexte);
                        }
                        else
                        {
                            var termine = data.AvancementPlanParStatut.GetValueOrDefault("Termine", 0);
                            var pctTermine = 100.0 * termine / data.Mesures.Count;
                            c.Item().PaddingTop(6).ShowEntire().Row(row =>
                            {
                                row.ConstantItem(120).Height(120).Svg(AnneauSimple(pctTermine, VertConforme, pctTermine.ToString("F0", CultureInfo.InvariantCulture) + "%")).FitWidth();
                                row.RelativeItem().PaddingLeft(16).AlignMiddle().Row(rr =>
                                {
                                    foreach (var statut in new[] { "ALancer", "EnCours", "Termine" })
                                        Chiffre(rr, data.AvancementPlanParStatut.GetValueOrDefault(statut, 0), LibelleStatutMesure(statut, anglais));
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
                            // Un seul axe -> l'anneau global ci-dessus dit deja tout.
                            if (parAxe.Count >= 2)
                            {
                                c.Item().PaddingTop(14).ShowEntire().Column(cc =>
                                {
                                    cc.Item().AlignCenter().Text(T("Avancement du plan par axe de traitement", "Plan progress by treatment area")).FontFamily(SansSemiBold).FontSize(8).FontColor(Encre);
                                    cc.Item().PaddingTop(4).AlignCenter().Height(150).Svg(GraphiqueBarres(
                                        parAxe.Select(a => (LibellesRapport.Axe(a.Axe, anglais) + " (" + a.Total + ")", a.Pct, VertConforme)).ToList(),
                                        100, "%")).FitHeight();
                                });
                            }
                        }
                    });

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, T("Registre d'acceptation des risques residuels", "Register of residual-risk acceptance"));
                        var acceptes = data.ScenariosDeRisque.Where(s => s.AccepteParDirection).ToList();
                        if (acceptes.Count == 0)
                        {
                            c.Item().PaddingTop(4).Text(T("Aucun risque residuel accepte formellement.", "No residual risk formally accepted.")).FontSize(8.5f).Italic().FontColor(GrisTexte);
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
                                    sc.Item().Text(T("Proprietaire : ", "Owner: ") + s.NomProprietaireRisque + T(" -- Validateur : ", " -- Validator: ") + s.NomValidateurSecurite).FontSize(7.5f).FontColor(GrisTexte);
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
                        row.RelativeItem().Text(T("EBIOS Risk Manager -- Synthese globale", "EBIOS Risk Manager -- Global summary")).FontFamily(Mono).FontSize(7).FontColor(GrisTexte);
                        row.RelativeItem().AlignRight().Text(DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm")).FontFamily(MonoMedium).FontSize(7).FontColor(GrisTexte);
                    });
                });
            });
        });

        return document.GeneratePdf();
    }
}
