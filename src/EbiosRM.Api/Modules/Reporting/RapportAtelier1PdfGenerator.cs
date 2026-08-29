using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EbiosRM.Api.Modules.Reporting;

public sealed class RapportAtelier1PdfGenerator
{
    // Palette "Bleu France" -- identité institutionnelle sobre, cohérente
    // avec un livrable EBIOS RM destiné à une direction/RSSI.
    private static readonly string BleuFrance = "#000091";
    private static readonly string BleuFranceClair = "#E3E3FD";
    private static readonly string Encre = "#161616";
    private static readonly string GrisTexte = "#3A3A3A";
    private static readonly string GrisLigne = "#DDDDDD";
    private static readonly string GrisFond = "#F6F6F6";
    private static readonly string RougeAlerte = "#B34000";
    private static readonly string VertConforme = "#18753C";

    private const string SerifTitre = "Fraunces 72pt";
    private const string SerifTitreSemiBold = "Fraunces 72pt SemiBold";
    private const string Sans = "IBM Plex Sans";
    private const string SansMedium = "IBM Plex Sans Medium";
    private const string SansSemiBold = "IBM Plex Sans SemiBold";
    private const string Mono = "IBM Plex Mono";
    private const string MonoMedium = "IBM Plex Mono Medium";

    public byte[] Generer(RapportAtelier1Data data, bool anglais = false)
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
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("EBIOS RISK MANAGER").FontFamily(MonoMedium).FontSize(8).FontColor(BleuFrance).LetterSpacing(0.05f);
                            c.Item().PaddingTop(2).Text(T("Atelier 1 -- Cadrage et socle de sécurité", "Workshop 1 -- Scoping and security baseline")).FontFamily(SerifTitreSemiBold).FontSize(19).FontColor(Encre);
                        });
                        row.ConstantItem(140).AlignRight().Column(c =>
                        {
                            c.Item().Text(T("Version ", "Version ") + data.Version).FontFamily(MonoMedium).FontSize(8).FontColor(GrisTexte);
                            c.Item().Text(T("Validé le ", "Validated on ") + data.DateValidationUtc.ToString(anglais ? "yyyy-MM-dd" : "dd/MM/yyyy")).FontFamily(Mono).FontSize(8).FontColor(GrisTexte);
                        });
                    });
                    col.Item().PaddingTop(10).LineHorizontal(1.4f).LineColor(BleuFrance);
                });

                page.Content().PaddingTop(18).Column(col =>
                {
                    col.Spacing(20);

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, T("Cadrage de l'étude", "Study scoping"));
                        c.Item().PaddingTop(6).Background(GrisFond).Padding(12).Column(inner =>
                        {
                            Champ(inner, T("Étude", "Study"), data.NomEtude);
                            Champ(inner, T("Mission", "Mission"), data.Mission);
                            Champ(inner, T("Périmètre", "Scope"), data.Perimetre);
                            Champ(inner, T("Statut au moment de la validation", "Status at validation time"), LibellesRapport.StatutEtude(data.Statut, anglais));
                        });
                    });

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, T("Valeurs métier et biens supports", "Business values and supporting assets"));

                        c.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2.1f);
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(2.1f);
                                columns.RelativeColumn(1.1f);
                                columns.RelativeColumn(1.2f);
                            });

                            EnteteCellule(table.Cell(), T("Valeur métier", "Business value"));
                            EnteteCellule(table.Cell(), T("Entité (VM)", "Entity (BV)"));
                            EnteteCellule(table.Cell(), T("Bien support", "Supporting asset"));
                            EnteteCellule(table.Cell(), T("Type", "Type"));
                            EnteteCellule(table.Cell(), T("Entité (bien)", "Entity (asset)"));

                            var ligne = 0;
                            foreach (var vm in data.ValeursMetier)
                            {
                                if (vm.BiensSupport.Count == 0)
                                {
                                    var alt = ligne % 2 == 1;
                                    CelluleZebra(table.Cell(), vm.Description, alt, police: SansMedium);
                                    CelluleZebra(table.Cell(), vm.EntiteProprietaire, alt);
                                    CelluleZebra(table.Cell(), "--", alt, couleur: GrisTexte);
                                    CelluleZebra(table.Cell(), "--", alt, couleur: GrisTexte);
                                    CelluleZebra(table.Cell(), "--", alt, couleur: GrisTexte);
                                    ligne++;
                                }
                                else
                                {
                                    foreach (var bien in vm.BiensSupport)
                                    {
                                        var alt = ligne % 2 == 1;
                                        CelluleZebra(table.Cell(), vm.Description, alt, police: SansMedium);
                                        CelluleZebra(table.Cell(), vm.EntiteProprietaire, alt);
                                        CelluleZebra(table.Cell(), bien.Description, alt);
                                        CelluleZebra(table.Cell(), LibellesRapport.TypeBienSupport(bien.Type, anglais), alt);
                                        CelluleZebra(table.Cell(), bien.EntiteProprietaire, alt);
                                        ligne++;
                                    }
                                }
                            }
                        });
                    });

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, T("Événements redoutés", "Feared events"));

                        c.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(4);
                                columns.ConstantColumn(70);
                            });

                            table.Header(header =>
                            {
                                EnteteCellule(header.Cell(), T("Valeur métier", "Business value"));
                                EnteteCellule(header.Cell(), T("Événement redouté", "Feared event"));
                                EnteteCellule(header.Cell(), T("Gravité", "Severity"));
                            });

                            foreach (var er in data.EvenementsRedoutes)
                            {
                                Cellule(table.Cell(), er.ValeurMetierDescription);
                                Cellule(table.Cell(), er.Description);
                                table.Cell().PaddingVertical(5).AlignCenter().Text(er.Gravite.ToString())
                                    .FontFamily(MonoMedium).FontSize(9)
                                    .FontColor(er.Gravite >= 3 ? RougeAlerte : Encre);
                            }
                        });
                    });

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, T("Socle de sécurité -- ISO/IEC 27001:2022, Annexe A", "Security baseline -- ISO/IEC 27001:2022, Annex A"));

                        if (data.ReferentielsApplicables.Count == 0)
                        {
                            c.Item().PaddingTop(6).Text(T("Aucun référentiel renseigné.", "No framework recorded.")).FontSize(9).Italic().FontColor(GrisTexte);
                        }
                        else
                        {
                            c.Item().PaddingTop(6).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(3);
                                    columns.ConstantColumn(80);
                                });

                                EnteteCellule(table.Cell(), T("Référentiel", "Framework"));
                                EnteteCellule(table.Cell(), T("État actuel", "Current state"));
                                EnteteCellule(table.Cell(), T("État", "State"));

                                for (var i = 0; i < data.ReferentielsApplicables.Count; i++)
                                {
                                    var r = data.ReferentielsApplicables[i];
                                    var alt = i % 2 == 1;
                                    var couleurEtat = r.Etat == "Conforme" ? VertConforme
                                        : r.Etat == "NonApplicable" ? GrisTexte
                                        : RougeAlerte;
                                    CelluleZebra(table.Cell(), r.Nom, alt);
                                    CelluleZebra(table.Cell(), string.IsNullOrWhiteSpace(r.EtatActuel) ? "--" : r.EtatActuel, alt);
                                    CelluleZebra(table.Cell(), LibellesRapport.EtatConformite(r.Etat, anglais), alt, couleur: couleurEtat, police: MonoMedium, taille: 8);
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
                        row.RelativeItem().Text(T("EBIOS Risk Manager -- Livrable Atelier 1", "EBIOS Risk Manager -- Workshop 1 deliverable")).FontFamily(Mono).FontSize(7).FontColor(GrisTexte);
                        row.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span(T("Généré le ", "Generated on ")).FontFamily(Mono).FontSize(7).FontColor(GrisTexte);
                            x.Span(DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm")).FontFamily(MonoMedium).FontSize(7).FontColor(GrisTexte);
                        });
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void SectionTitre(QuestPDF.Fluent.ColumnDescriptor col, string texte)
    {
        col.Item().Row(row =>
        {
            row.ConstantItem(3).Height(16).Background(BleuFrance);
            row.RelativeItem().PaddingLeft(8).Text(texte).FontFamily(SerifTitreSemiBold).FontSize(13).FontColor(Encre);
        });
    }

    private static void Champ(QuestPDF.Fluent.ColumnDescriptor col, string label, string valeur)
    {
        col.Item().PaddingBottom(3).Row(row =>
        {
            row.ConstantItem(140).Text(label).FontFamily(MonoMedium).FontSize(8).FontColor(GrisTexte);
            row.RelativeItem().Text(valeur).FontSize(9.5f).FontColor(Encre);
        });
    }

    private static void EnteteCellule(QuestPDF.Infrastructure.IContainer cell, string texte)
    {
        cell.Background(BleuFranceClair).Padding(5).Text(texte).FontFamily(MonoMedium).FontSize(7.5f).FontColor(BleuFrance).LetterSpacing(0.02f);
    }

    private static void Cellule(QuestPDF.Infrastructure.IContainer cell, string texte)
    {
        cell.BorderBottom(0.6f).BorderColor(GrisLigne).PaddingVertical(5).PaddingRight(6).Text(texte).FontSize(8.5f);
    }

    private static void CelluleZebra(QuestPDF.Infrastructure.IContainer cell, string texte, bool alterne, string? couleur = null, string? police = null, float? taille = null)
    {
        var conteneur = alterne ? cell.Background(GrisFond) : cell;
        var texteDescr = conteneur.BorderBottom(0.6f).BorderColor(GrisLigne).PaddingVertical(5).PaddingHorizontal(6).Text(texte);
        texteDescr.FontSize(taille ?? 8.5f);
        texteDescr.FontColor(couleur ?? Encre);
        if (police != null)
        {
            texteDescr.FontFamily(police);
        }
    }
}
