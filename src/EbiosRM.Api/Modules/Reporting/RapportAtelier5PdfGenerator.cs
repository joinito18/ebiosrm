using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static EbiosRM.Api.Modules.Reporting.RapportPdfStyle;

namespace EbiosRM.Api.Modules.Reporting;

public sealed class RapportAtelier5PdfGenerator
{
    public byte[] Generer(RapportAtelier5Data data, bool anglais = false)
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
                    col.Item().PaddingTop(2).Text(T("Atelier 5 -- Traitement du risque", "Workshop 5 -- Risk treatment")).FontFamily(SerifTitreSemiBold).FontSize(19).FontColor(Encre);
                    col.Item().Text(data.NomEtude).FontFamily(Mono).FontSize(8).FontColor(GrisTexte);
                    col.Item().PaddingTop(10).LineHorizontal(1.4f).LineColor(BleuFrance);
                });

                page.Content().PaddingTop(18).Column(col =>
                {
                    col.Spacing(20);

                    col.Item().Text(anglais ? "This document describes the initial and residual risk level of each risk scenario, the associated risk treatment plan, and the register of formal acceptance of residual risks by management." : "Ce document decrit le niveau de risque initial et residuel de chaque scenario de risque, le plan de traitement du risque associe, et le registre d'acceptation formelle des risques residuels par la Direction.").FontSize(9.5f);

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, T("Resume des ateliers precedents", "Summary of previous workshops"));
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
                        c.Item().PaddingTop(8).Row(row =>
                        {
                            Chiffre(row, data.ChiffresCles.NombreValeursMetier, T("Valeurs metier (A1)", "Business values (W1)"));
                            Chiffre(row, data.ChiffresCles.NombreBiensSupport, T("Biens support (A1)", "Supporting assets (W1)"));
                            Chiffre(row, data.ChiffresCles.NombreEvenementsRedoutes, T("Evenements redoutes (A1)", "Feared events (W1)"));
                        });
                        c.Item().PaddingTop(8).Row(row =>
                        {
                            Chiffre(row, data.ChiffresCles.NombrePartiesPrenantesCritiques, T("Parties prenantes critiques (A3) / ", "Critical stakeholders (W3) / ") + data.ChiffresCles.NombrePartiesPrenantes);
                            Chiffre(row, data.ChiffresCles.NombreScenariosStrategiques, T("Scenarios strategiques (A3)", "Strategic scenarios (W3)"));
                            Chiffre(row, data.ChiffresCles.NombreScenariosOperationnels, T("Scenarios operationnels (A4)", "Operational scenarios (W4)"));
                        });

                        var totalControles = data.ConformiteSocle.NombreConforme + data.ConformiteSocle.NombreNonConforme + data.ConformiteSocle.NombreNonApplicable;
                        if (totalControles > 0)
                        {
                            var pctConforme = 100.0 * data.ConformiteSocle.NombreConforme / totalControles;
                            c.Item().PaddingTop(10).ShowEntire().Row(row =>
                            {
                                row.ConstantItem(80).Height(80).Svg(AnneauMultiSegments(
                                    new List<(double, string)>
                                    {
                                        (data.ConformiteSocle.NombreConforme, VertConforme),
                                        (data.ConformiteSocle.NombreNonConforme, RougeAlerte),
                                        (data.ConformiteSocle.NombreNonApplicable, GrisLigne),
                                    },
                                    pctConforme.ToString("F0", CultureInfo.InvariantCulture) + "%")).FitWidth();
                                row.RelativeItem().PaddingLeft(16).AlignMiddle().Column(cc =>
                                {
                                    cc.Item().Text(T("Conformite du socle de securite (A1)", "Security-baseline compliance (W1)")).FontFamily(SansSemiBold).FontSize(8).FontColor(Encre);
                                    cc.Item().PaddingTop(2).Row(r => Legende(r, VertConforme, data.ConformiteSocle.NombreConforme + T(" conforme(s)", " compliant")));
                                    cc.Item().PaddingTop(2).Row(r => Legende(r, RougeAlerte, data.ConformiteSocle.NombreNonConforme + T(" non conforme(s)", " non-compliant")));
                                });
                            });
                        }
                    });

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, T("Grille officielle de determination du niveau de risque", "Official risk-level determination grid"));
                        c.Item().PaddingTop(4).Text(anglais ? "Cross of Severity (targeted feared event) x Likelihood (operational scenario), project default thresholds are adjustable." : "Croisement Gravite (evenement redoute vise) x Vraisemblance (scenario operationnel), seuils par defaut du projet ajustables.").FontSize(8.5f).Italic().FontColor(GrisTexte);
                        c.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.ConstantColumn(150);
                                cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn();
                            });
                            table.Cell().Background(GrisFond).Padding(5).Text(T("Gravite \\ Vraisemblance", "Severity \\ Likelihood")).FontFamily(MonoMedium).FontSize(7).FontColor(GrisTexte);
                            foreach (var v in new[] { "V1", "V2", "V3", "V4" })
                                EnteteCellule(table.Cell(), v);

                            LigneRisque(table, "1", "Faible", "Faible", "Moyen", "Moyen", anglais);
                            LigneRisque(table, "2", "Faible", "Faible", "Moyen", "Eleve", anglais);
                            LigneRisque(table, "3", "Faible", "Moyen", "Eleve", "Eleve", anglais);
                            LigneRisque(table, "4", "Faible", "Moyen", "Eleve", "Eleve", anglais);
                        });
                    });

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, T("Cartographie des risques -- avant / apres traitement", "Risk mapping -- before / after treatment"));
                        if (data.ScenariosDeRisque.Count == 0)
                        {
                            c.Item().PaddingTop(4).Text(T("Aucun scenario de risque cree a ce stade.", "No risk scenario created at this stage.")).FontSize(8.5f).Italic().FontColor(GrisTexte);
                        }
                        else
                        {
                            CartographieCompleteAvecLegende(c, data.ScenariosDeRisque, anglais);

                            c.Item().PaddingTop(10).Table(table =>
                            {
                                table.ColumnsDefinition(cd =>
                                {
                                    cd.RelativeColumn(2.4f); cd.RelativeColumn(1.6f); cd.RelativeColumn(1.6f); cd.RelativeColumn(1.6f);
                                });
                                EnteteCellule(table.Cell(), T("Scenario", "Scenario"));
                                EnteteCellule(table.Cell(), T("Initial (G x V)", "Initial (S x L)"));
                                EnteteCellule(table.Cell(), T("Residuel (G x V)", "Residual (S x L)"));
                                EnteteCellule(table.Cell(), T("Classe d'acceptation", "Acceptance class"));

                                foreach (var s in data.ScenariosDeRisque.OrderByDescending(s => s.NiveauRisqueResiduel))
                                {
                                    table.Cell().BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).Column(cc =>
                                    {
                                        cc.Item().Text(s.LibelleCouple).FontFamily(SansSemiBold).FontSize(8);
                                        cc.Item().Text(s.LibelleChemin).FontSize(7.5f).FontColor(GrisTexte);
                                    });
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
                        SectionTitre(c, T("Plan de traitement du risque", "Risk treatment plan"));
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
                                    cd.RelativeColumn(2.3f); cd.RelativeColumn(1.4f); cd.RelativeColumn(1.3f); cd.RelativeColumn(1.3f); cd.RelativeColumn(1.0f); cd.RelativeColumn(0.9f); cd.RelativeColumn(1.0f);
                                });
                                EnteteCellule(table.Cell(), T("Mesure de securite", "Security measure"));
                                EnteteCellule(table.Cell(), T("Scenarios associes", "Associated scenarios"));
                                EnteteCellule(table.Cell(), T("Responsable", "Owner"));
                                EnteteCellule(table.Cell(), T("Freins et difficultes", "Obstacles and difficulties"));
                                EnteteCellule(table.Cell(), T("Cout", "Cost"));
                                EnteteCellule(table.Cell(), T("Echeance", "Deadline"));
                                EnteteCellule(table.Cell(), T("Statut", "Status"));

                                foreach (var axe in new[] { "Gouvernance", "Protection", "Defense", "Resilience" })
                                {
                                    var mesuresAxe = data.Mesures.Where(m => m.Axe == axe).ToList();
                                    if (mesuresAxe.Count == 0)
                                        continue;

                                    BandeAxe(table, 7, axe, anglais);

                                    var alterne = false;
                                    foreach (var m in mesuresAxe)
                                    {
                                        var fond = alterne ? GrisFond : "#FFFFFF";
                                        alterne = !alterne;
                                        table.Cell().Background(fond).BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).Text(m.Description).FontSize(7.8f);
                                        table.Cell().Background(fond).BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).Text(string.Join("; ", m.LibellesScenariosDeRisque)).FontSize(6.8f).FontColor(GrisTexte);
                                        table.Cell().Background(fond).BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).Text(m.Responsable).FontSize(7.5f);
                                        table.Cell().Background(fond).BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).Text(m.FreinsEtDifficultes ?? "--").FontSize(7).FontColor(GrisTexte);
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
                        SectionTitre(c, T("Registre d'acceptation des risques residuels", "Register of residual-risk acceptance"));
                        var acceptes = data.ScenariosDeRisque.Where(s => s.AccepteParDirection).ToList();
                        if (acceptes.Count == 0)
                        {
                            c.Item().PaddingTop(4).Text(T("Aucun risque residuel accepte formellement a ce stade.", "No residual risk formally accepted at this stage.")).FontSize(8.5f).Italic().FontColor(GrisTexte);
                        }
                        else
                        {
                            foreach (var s in acceptes)
                            {
                                c.Item().PaddingTop(10).Column(sc =>
                                {
                                    sc.Item().Row(row =>
                                    {
                                        row.RelativeItem().Text(s.LibelleCouple + " -- " + s.LibelleChemin).FontFamily(SansSemiBold).FontSize(9);
                                        row.ConstantItem(70).AlignRight().Text(s.NiveauRisqueResiduel ?? "--").FontFamily(MonoMedium).FontSize(8).FontColor(CouleurNiveau(s.NiveauRisqueResiduel));
                                    });
                                    sc.Item().PaddingTop(2).Text(T("Proprietaire du risque : ", "Risk owner: ") + s.NomProprietaireRisque).FontSize(7.8f);
                                    sc.Item().Text(T("Validateur securite : ", "Security validator: ") + s.NomValidateurSecurite).FontSize(7.8f);
                                    if (s.NomSponsorExecutif is not null)
                                        sc.Item().Text(T("Sponsor executif : ", "Executive sponsor: ") + s.NomSponsorExecutif).FontSize(7.8f);
                                    if (s.DateAcceptationUtc is not null)
                                        sc.Item().Text(T("Accepte le ", "Accepted on ") + s.DateAcceptationUtc.Value.ToString(anglais ? "yyyy-MM-dd" : "dd/MM/yyyy")).FontFamily(MonoMedium).FontSize(7).FontColor(GrisTexte);
                                    if (s.JustificationAcceptation is not null)
                                        sc.Item().PaddingTop(2).Text(s.JustificationAcceptation).FontSize(7.8f).Italic().FontColor(GrisTexte);
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
                        row.RelativeItem().Text(T("EBIOS Risk Manager -- Livrable Atelier 5", "EBIOS Risk Manager -- Workshop 5 deliverable")).FontFamily(Mono).FontSize(7).FontColor(GrisTexte);
                        row.RelativeItem().AlignRight().Text(DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm")).FontFamily(MonoMedium).FontSize(7).FontColor(GrisTexte);
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void LigneRisque(TableDescriptor table, string label, string v1, string v2, string v3, string v4, bool anglais)
    {
        table.Cell().Background(BleuFranceClair).Padding(5).Text(label).FontFamily(MonoMedium).FontSize(7.5f).FontColor(BleuFrance);
        foreach (var v in new[] { v1, v2, v3, v4 })
            table.Cell().BorderBottom(0.6f).BorderColor(GrisLigne).PaddingVertical(5).AlignCenter().Text(LibellesRapport.NiveauRisque(v, anglais)).FontFamily(MonoMedium).FontSize(8).FontColor(CouleurNiveau(v));
    }
}
