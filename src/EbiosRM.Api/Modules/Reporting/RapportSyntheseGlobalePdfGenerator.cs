using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EbiosRM.Api.Modules.Reporting;

/// <summary>
/// Document autonome distinct du rapport d'Atelier 5 -- destiné à la
/// Direction, consolide les 5 ateliers d'une étude validée.
/// </summary>
public sealed class RapportSyntheseGlobalePdfGenerator
{
    private static readonly string BleuFrance = "#000091";
    private static readonly string BleuFranceClair = "#E3E3FD";
    private static readonly string Encre = "#161616";
    private static readonly string GrisTexte = "#3A3A3A";
    private static readonly string GrisLigne = "#DDDDDD";
    private static readonly string RougeAlerte = "#B34000";
    private static readonly string OrangeAlerte = "#BA7517";
    private static readonly string VertConforme = "#18753C";

    private const string SerifTitreSemiBold = "Fraunces 72pt SemiBold";
    private const string Sans = "IBM Plex Sans";
    private const string SansSemiBold = "IBM Plex Sans SemiBold";
    private const string Mono = "IBM Plex Mono";
    private const string MonoMedium = "IBM Plex Mono Medium";

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
                            c.Item().PaddingTop(6).Table(table =>
                            {
                                table.ColumnsDefinition(cd =>
                                {
                                    cd.RelativeColumn(3); cd.RelativeColumn(1.4f); cd.RelativeColumn(1.4f); cd.RelativeColumn(1.8f);
                                });
                                EnteteCellule(table.Cell(), "Scenario");
                                EnteteCellule(table.Cell(), "Niveau initial");
                                EnteteCellule(table.Cell(), "Niveau residuel");
                                EnteteCellule(table.Cell(), "Classe d'acceptation");

                                foreach (var s in data.ScenariosDeRisque.OrderByDescending(s => s.NiveauRisqueResiduel))
                                {
                                    table.Cell().BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).Text(s.LibelleCouple + " -- " + s.LibelleChemin).FontSize(7.8f);
                                    table.Cell().BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).AlignCenter().Text(s.NiveauRisqueInitial ?? "--").FontFamily(MonoMedium).FontSize(8).FontColor(CouleurNiveau(s.NiveauRisqueInitial));
                                    table.Cell().BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).AlignCenter().Text(s.NiveauRisqueResiduel ?? "--").FontFamily(MonoMedium).FontSize(8).FontColor(CouleurNiveau(s.NiveauRisqueResiduel));
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
                            c.Item().PaddingTop(6).Row(row =>
                            {
                                foreach (var statut in new[] { "ALancer", "EnCours", "Termine" })
                                    Chiffre(row, data.AvancementPlanParStatut.GetValueOrDefault(statut, 0), LibelleStatut(statut));
                            });
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

    private static void Chiffre(QuestPDF.Fluent.RowDescriptor row, int valeur, string libelle)
    {
        row.RelativeItem().Column(c =>
        {
            c.Item().Text(valeur.ToString()).FontFamily(SerifTitreSemiBold).FontSize(22).FontColor(BleuFrance);
            c.Item().Text(libelle).FontSize(7.5f).FontColor(GrisTexte);
        });
    }

    private static string LibelleStatut(string statut) => statut switch
    {
        "ALancer" => "A lancer",
        "EnCours" => "En cours",
        "Termine" => "Termine",
        _ => statut,
    };

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
