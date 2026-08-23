using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EbiosRM.Api.Modules.Reporting;

public sealed class RapportAtelier4PdfGenerator
{
    private static readonly string BleuFrance = "#000091";
    private static readonly string BleuFranceClair = "#E3E3FD";
    private static readonly string Encre = "#161616";
    private static readonly string GrisTexte = "#3A3A3A";
    private static readonly string GrisLigne = "#DDDDDD";
    private static readonly string GrisFond = "#F6F6F6";
    private static readonly string RougeAlerte = "#B34000";
    private static readonly string OrangeAlerte = "#BA7517";
    private static readonly string JauneAlerte = "#A68A2A";
    private static readonly string VertConforme = "#18753C";

    private const string SerifTitreSemiBold = "Fraunces 72pt SemiBold";
    private const string Sans = "IBM Plex Sans";
    private const string SansSemiBold = "IBM Plex Sans SemiBold";
    private const string Mono = "IBM Plex Mono";
    private const string MonoMedium = "IBM Plex Mono Medium";

    public byte[] Generer(RapportAtelier4Data data)
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
                    col.Item().PaddingTop(2).Text("Atelier 4 -- Scenarios operationnels").FontFamily(SerifTitreSemiBold).FontSize(19).FontColor(Encre);
                    col.Item().Text(data.NomEtude).FontFamily(Mono).FontSize(8).FontColor(GrisTexte);
                    col.Item().PaddingTop(10).LineHorizontal(1.4f).LineColor(BleuFrance);
                });

                page.Content().PaddingTop(18).Column(col =>
                {
                    col.Spacing(20);

                    col.Item().Text("Ce document decrit les scenarios operationnels (modes operatoires techniques) associes a chaque chemin d'attaque strategique retenu en Atelier 3, et leur vraisemblance evaluee selon la grille officielle Probabilite de succes x Difficulte technique.").FontSize(9.5f);

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, "Grille officielle d'evaluation de la vraisemblance");
                        c.Item().PaddingTop(4).Text("Methode d'evaluation affinee : chaque mode operatoire est cote selon sa probabilite de succes (chance de reussite pour l'attaquant) et sa difficulte technique (ressources engagees), croisees dans la grille ci-dessous.").FontSize(8.5f).Italic().FontColor(GrisTexte);
                        c.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.ConstantColumn(150);
                                cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn();
                            });
                            table.Cell().Background(GrisFond).Padding(5).Text("Probabilite \\ Difficulte").FontFamily(MonoMedium).FontSize(7).FontColor(GrisTexte);
                            foreach (var d in new[] { "1. Faible", "2. Moderee", "3. Elevee", "4. Tres elevee" })
                                EnteteCellule(table.Cell(), d);

                            LigneVraisemblance(table, "4. Quasi-certaine", "V4", "V3", "V3", "V2");
                            LigneVraisemblance(table, "3. Tres elevee", "V3", "V3", "V2", "V2");
                            LigneVraisemblance(table, "2. Significative", "V3", "V2", "V2", "V1");
                            LigneVraisemblance(table, "1. Faible", "V2", "V2", "V1", "V1");
                        });
                    });

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, "Scenarios operationnels");
                        if (data.ScenariosOperationnels.Count == 0)
                        {
                            c.Item().PaddingTop(4).Text("Aucun scenario operationnel cree a ce stade.").FontSize(8.5f).Italic().FontColor(GrisTexte);
                        }
                        else
                        {
                            foreach (var s in data.ScenariosOperationnels)
                            {
                                c.Item().PaddingTop(12).Column(sc =>
                                {
                                    sc.Item().Row(row =>
                                    {
                                        row.RelativeItem().Column(cc =>
                                        {
                                            cc.Item().Text(s.LibelleCouple).FontFamily(SansSemiBold).FontSize(10.5f).FontColor(BleuFrance);
                                            cc.Item().Text(s.LibelleCheminAttaque).FontSize(8.5f).FontColor(GrisTexte);
                                        });
                                        if (s.VraisemblanceGlobale != null)
                                            row.ConstantItem(90).AlignRight().AlignTop().Text("Vraisemblance " + s.VraisemblanceGlobale).FontFamily(MonoMedium).FontSize(8).FontColor(CouleurVraisemblance(s.VraisemblanceGlobale));
                                    });

                                    if (s.ModesOperatoires.Count == 0)
                                    {
                                        sc.Item().PaddingTop(4).Text("Aucun mode operatoire defini pour ce scenario.").FontSize(8).Italic().FontColor(GrisTexte);
                                    }
                                    else
                                    {
                                        foreach (var mode in s.ModesOperatoires)
                                        {
                                            sc.Item().PaddingTop(6).PaddingLeft(10).BorderLeft(1.4f).BorderColor(BleuFranceClair).PaddingVertical(2).Column(mc =>
                                            {
                                                mc.Item().PaddingLeft(6).Row(row =>
                                                {
                                                    row.RelativeItem().Text(mode.Description).FontFamily(SansSemiBold).FontSize(8.5f);
                                                    row.ConstantItem(60).AlignRight().Text(mode.Vraisemblance).FontFamily(MonoMedium).FontSize(8).FontColor(CouleurVraisemblance(mode.Vraisemblance));
                                                });
                                                if (mode.ActionsElementaires.Count > 0)
                                                {
                                                    string TexteParPhase(string phase)
                                                    {
                                                        var texte = string.Join("; ", mode.ActionsElementaires
                                                            .Where(a => a.Phase == phase)
                                                            .Select(a => a.Description + " -> " + a.LibelleBienSupport));
                                                        return texte.Length > 0 ? texte : "--";
                                                    }

                                                    mc.Item().PaddingLeft(6).PaddingTop(2).Row(row =>
                                                    {
                                                        row.RelativeItem().Text(t => { t.Span("CONNAITRE ").FontFamily(MonoMedium).FontSize(6.5f).FontColor(GrisTexte); t.Span(TexteParPhase("Connaitre")).FontSize(7).FontColor(GrisTexte); });
                                                        row.RelativeItem().Text(t => { t.Span("RENTRER ").FontFamily(MonoMedium).FontSize(6.5f).FontColor(GrisTexte); t.Span(TexteParPhase("Rentrer")).FontSize(7).FontColor(GrisTexte); });
                                                    });
                                                    mc.Item().PaddingLeft(6).PaddingTop(1).Row(row =>
                                                    {
                                                        row.RelativeItem().Text(t => { t.Span("TROUVER ").FontFamily(MonoMedium).FontSize(6.5f).FontColor(GrisTexte); t.Span(TexteParPhase("Trouver")).FontSize(7).FontColor(GrisTexte); });
                                                        row.RelativeItem().Text(t => { t.Span("EXPLOITER ").FontFamily(MonoMedium).FontSize(6.5f).FontColor(GrisTexte); t.Span(TexteParPhase("Exploiter")).FontSize(7).FontColor(GrisTexte); });
                                                    });
                                                }
                                                mc.Item().PaddingLeft(6).PaddingTop(2).Text("Probabilite de succes " + mode.ProbabiliteSucces + " -- Difficulte technique " + mode.DifficulteTechnique).FontFamily(MonoMedium).FontSize(7).FontColor(GrisTexte);
                                                if (mode.VraisemblanceEstJugementExpert)
                                                {
                                                    mc.Item().PaddingLeft(6).PaddingTop(1).Text("Vraisemblance determinee par jugement d'expert").FontSize(6.5f).Italic().FontColor(GrisTexte);
                                                    if (mode.JustificationVraisemblance is not null)
                                                        mc.Item().PaddingLeft(6).Text(mode.JustificationVraisemblance).FontSize(7).Italic().FontColor(GrisTexte);
                                                }
                                            });
                                        }
                                    }
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
                        row.RelativeItem().Text("EBIOS Risk Manager -- Livrable Atelier 4").FontFamily(Mono).FontSize(7).FontColor(GrisTexte);
                        row.RelativeItem().AlignRight().Text(DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm")).FontFamily(MonoMedium).FontSize(7).FontColor(GrisTexte);
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void LigneVraisemblance(QuestPDF.Fluent.TableDescriptor table, string label, string v1, string v2, string v3, string v4)
    {
        table.Cell().Background(BleuFranceClair).Padding(5).Text(label).FontFamily(MonoMedium).FontSize(7.5f).FontColor(BleuFrance);
        foreach (var v in new[] { v1, v2, v3, v4 })
            table.Cell().BorderBottom(0.6f).BorderColor(GrisLigne).PaddingVertical(5).AlignCenter().Text(v).FontFamily(MonoMedium).FontSize(8).FontColor(CouleurVraisemblance(v));
    }

    private static string CouleurVraisemblance(string? v) => v switch
    {
        "V4" => RougeAlerte,
        "V3" => OrangeAlerte,
        "V2" => JauneAlerte,
        "V1" => VertConforme,
        _ => GrisTexte,
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
