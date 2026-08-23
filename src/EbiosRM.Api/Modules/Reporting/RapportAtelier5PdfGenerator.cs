using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EbiosRM.Api.Modules.Reporting;

public sealed class RapportAtelier5PdfGenerator
{
    private static readonly string BleuFrance = "#000091";
    private static readonly string BleuFranceClair = "#E3E3FD";
    private static readonly string Encre = "#161616";
    private static readonly string GrisTexte = "#3A3A3A";
    private static readonly string GrisLigne = "#DDDDDD";
    private static readonly string GrisFond = "#F6F6F6";
    private static readonly string RougeAlerte = "#B34000";
    private static readonly string OrangeAlerte = "#BA7517";
    private static readonly string VertConforme = "#18753C";

    private const string SerifTitreSemiBold = "Fraunces 72pt SemiBold";
    private const string Sans = "IBM Plex Sans";
    private const string SansSemiBold = "IBM Plex Sans SemiBold";
    private const string Mono = "IBM Plex Mono";
    private const string MonoMedium = "IBM Plex Mono Medium";

    public byte[] Generer(RapportAtelier5Data data)
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
                    col.Item().PaddingTop(2).Text("Atelier 5 -- Traitement du risque").FontFamily(SerifTitreSemiBold).FontSize(19).FontColor(Encre);
                    col.Item().Text(data.NomEtude).FontFamily(Mono).FontSize(8).FontColor(GrisTexte);
                    col.Item().PaddingTop(10).LineHorizontal(1.4f).LineColor(BleuFrance);
                });

                page.Content().PaddingTop(18).Column(col =>
                {
                    col.Spacing(20);

                    col.Item().Text("Ce document decrit le niveau de risque initial et residuel de chaque scenario de risque, le plan de traitement du risque associe, et le registre d'acceptation formelle des risques residuels par la Direction.").FontSize(9.5f);

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, "Grille officielle de determination du niveau de risque");
                        c.Item().PaddingTop(4).Text("Croisement Gravite (evenement redoute vise) x Vraisemblance (scenario operationnel), seuils par defaut du projet ajustables.").FontSize(8.5f).Italic().FontColor(GrisTexte);
                        c.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.ConstantColumn(150);
                                cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn();
                            });
                            table.Cell().Background(GrisFond).Padding(5).Text("Gravite \\ Vraisemblance").FontFamily(MonoMedium).FontSize(7).FontColor(GrisTexte);
                            foreach (var v in new[] { "V1", "V2", "V3", "V4" })
                                EnteteCellule(table.Cell(), v);

                            LigneRisque(table, "1", "Faible", "Faible", "Moyen", "Moyen");
                            LigneRisque(table, "2", "Faible", "Faible", "Moyen", "Eleve");
                            LigneRisque(table, "3", "Faible", "Moyen", "Eleve", "Eleve");
                            LigneRisque(table, "4", "Faible", "Moyen", "Eleve", "Eleve");
                        });
                    });

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, "Cartographie des risques -- avant / apres traitement");
                        if (data.ScenariosDeRisque.Count == 0)
                        {
                            c.Item().PaddingTop(4).Text("Aucun scenario de risque cree a ce stade.").FontSize(8.5f).Italic().FontColor(GrisTexte);
                        }
                        else
                        {
                            c.Item().PaddingTop(4).Text("Chaque colonne \"initial\"/\"residuel\" indique le calcul exact (Gravite x Vraisemblance) qui produit le niveau affiche en dessous -- a comparer avec la grille ci-dessus.").FontSize(7.5f).Italic().FontColor(GrisTexte);
                            c.Item().PaddingTop(6).Table(table =>
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
                                    table.Cell().BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).Column(cc =>
                                    {
                                        cc.Item().Text(s.LibelleCouple).FontFamily(SansSemiBold).FontSize(8);
                                        cc.Item().Text(s.LibelleChemin).FontSize(7.5f).FontColor(GrisTexte);
                                    });
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
                        SectionTitre(c, "Plan de traitement du risque");
                        if (data.Mesures.Count == 0)
                        {
                            c.Item().PaddingTop(4).Text("Aucune mesure de traitement definie a ce stade.").FontSize(8.5f).Italic().FontColor(GrisTexte);
                        }
                        else
                        {
                            foreach (var axe in new[] { "Gouvernance", "Protection", "Defense", "Resilience" })
                            {
                                var mesuresAxe = data.Mesures.Where(m => m.Axe == axe).ToList();
                                if (mesuresAxe.Count == 0)
                                    continue;

                                c.Item().PaddingTop(10).Text(axe.ToUpperInvariant()).FontFamily(MonoMedium).FontSize(9).FontColor(BleuFrance).LetterSpacing(0.03f);
                                c.Item().PaddingTop(4).Table(table =>
                                {
                                    table.ColumnsDefinition(cd =>
                                    {
                                        cd.RelativeColumn(2.6f); cd.RelativeColumn(1.6f); cd.RelativeColumn(0.9f); cd.RelativeColumn(0.9f); cd.RelativeColumn(1); cd.RelativeColumn(1);
                                    });
                                    EnteteCellule(table.Cell(), "Mesure");
                                    EnteteCellule(table.Cell(), "Responsable");
                                    EnteteCellule(table.Cell(), "Cout");
                                    EnteteCellule(table.Cell(), "Echeance");
                                    EnteteCellule(table.Cell(), "Statut");
                                    EnteteCellule(table.Cell(), "Scenarios");

                                    foreach (var m in mesuresAxe)
                                    {
                                        table.Cell().BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).Column(cc =>
                                        {
                                            cc.Item().Text(m.Description).FontSize(7.8f);
                                            if (m.FreinsEtDifficultes is not null)
                                                cc.Item().Text("Freins : " + m.FreinsEtDifficultes).FontSize(6.8f).Italic().FontColor(GrisTexte);
                                        });
                                        table.Cell().BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).Text(m.Responsable).FontSize(7.5f);
                                        table.Cell().BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).AlignCenter().Text(m.CoutComplexite).FontFamily(MonoMedium).FontSize(8);
                                        table.Cell().BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).Text(m.Echeance ?? "--").FontSize(7.5f);
                                        table.Cell().BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).Text(m.Statut).FontSize(7.5f);
                                        table.Cell().BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).Text(string.Join("; ", m.LibellesScenariosDeRisque)).FontSize(6.8f).FontColor(GrisTexte);
                                    }
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
                            c.Item().PaddingTop(4).Text("Aucun risque residuel accepte formellement a ce stade.").FontSize(8.5f).Italic().FontColor(GrisTexte);
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
                                    sc.Item().PaddingTop(2).Text("Proprietaire du risque : " + s.NomProprietaireRisque).FontSize(7.8f);
                                    sc.Item().Text("Validateur securite : " + s.NomValidateurSecurite).FontSize(7.8f);
                                    if (s.NomSponsorExecutif is not null)
                                        sc.Item().Text("Sponsor executif : " + s.NomSponsorExecutif).FontSize(7.8f);
                                    if (s.DateAcceptationUtc is not null)
                                        sc.Item().Text("Accepte le " + s.DateAcceptationUtc.Value.ToString("dd/MM/yyyy")).FontFamily(MonoMedium).FontSize(7).FontColor(GrisTexte);
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
                        row.RelativeItem().Text("EBIOS Risk Manager -- Livrable Atelier 5").FontFamily(Mono).FontSize(7).FontColor(GrisTexte);
                        row.RelativeItem().AlignRight().Text(DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm")).FontFamily(MonoMedium).FontSize(7).FontColor(GrisTexte);
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void LigneRisque(QuestPDF.Fluent.TableDescriptor table, string label, string v1, string v2, string v3, string v4)
    {
        table.Cell().Background(BleuFranceClair).Padding(5).Text(label).FontFamily(MonoMedium).FontSize(7.5f).FontColor(BleuFrance);
        foreach (var v in new[] { v1, v2, v3, v4 })
            table.Cell().BorderBottom(0.6f).BorderColor(GrisLigne).PaddingVertical(5).AlignCenter().Text(v).FontFamily(MonoMedium).FontSize(8).FontColor(CouleurNiveau(v));
    }

    private static string CouleurNiveau(string? niveau) => niveau switch
    {
        "Eleve" => RougeAlerte,
        "Moyen" => OrangeAlerte,
        "Faible" => VertConforme,
        _ => GrisTexte,
    };

    private static string LibelleClasse(string? classe) => classe switch
    {
        "AcceptableEnLEtat" => "Acceptable en l'etat",
        "TolerableSousControle" => "Tolerable sous controle",
        "Inacceptable" => "Inacceptable",
        _ => "--",
    };

    private static void SectionTitre(QuestPDF.Fluent.ColumnDescriptor col, string texte)
    {
        col.Item().Row(row =>
        {
            row.ConstantItem(3).Height(16).Background(BleuFrance);
            row.RelativeItem().PaddingLeft(8).Text(texte).FontFamily(SerifTitreSemiBold).FontSize(13).FontColor(Encre);
        });
    }

    private static void EnteteCellule(QuestPDF.Infrastructure.IContainer cell, string texte)
    {
        cell.Background(BleuFranceClair).Padding(5).Text(texte).FontFamily(MonoMedium).FontSize(7.5f).FontColor(BleuFrance).LetterSpacing(0.02f);
    }
}
