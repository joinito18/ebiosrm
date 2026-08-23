using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EbiosRM.Api.Modules.Reporting;

public sealed class RapportAtelier2PdfGenerator
{
    private static readonly string BleuFrance = "#000091";
    private static readonly string BleuFranceClair = "#E3E3FD";
    private static readonly string Encre = "#161616";
    private static readonly string GrisTexte = "#3A3A3A";
    private static readonly string GrisLigne = "#DDDDDD";
    private static readonly string GrisFond = "#F6F6F6";
    private static readonly string RougeAlerte = "#B34000";
    private static readonly string VertConforme = "#18753C";

    private const string SerifTitreSemiBold = "Fraunces 72pt SemiBold";
    private const string Sans = "IBM Plex Sans";
    private const string SansSemiBold = "IBM Plex Sans SemiBold";
    private const string Mono = "IBM Plex Mono";
    private const string MonoMedium = "IBM Plex Mono Medium";

    private static readonly (string Nom, string Code, string Description)[] Themes = new[]
    {
        ("Technologique", "A.8", "Defaillances techniques : firewall en fin de vie, OS obsoletes, antivirus expires, sauvegardes non chiffrees."),
        ("Organisationnel", "A.5", "Absence de gouvernance, de politiques formelles et de gestion des tiers."),
        ("Personnes", "A.6", "Risques lies aux acteurs internes, a la gestion des identites et aux facteurs humains."),
        ("Physique", "A.7", "Risques lies a l'environnement physique et aux infrastructures des sites."),
    };

    public byte[] Generer(RapportAtelier2Data data)
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
                    col.Item().PaddingTop(2).Text("Atelier 2 -- Sources de risque").FontFamily(SerifTitreSemiBold).FontSize(19).FontColor(Encre);
                    col.Item().Text(data.NomEtude).FontFamily(Mono).FontSize(8).FontColor(GrisTexte);
                    col.Item().PaddingTop(10).LineHorizontal(1.4f).LineColor(BleuFrance);
                });

                page.Content().PaddingTop(18).Column(col =>
                {
                    col.Spacing(20);

                    col.Item().Text("Ce document identifie les couples Source de Risque / Objectif Vise pertinents pour l'objet de l'etude. Les couples \"Tres pertinent\" seront traites en priorite dans l'Atelier 3, les couples \"Plutot pertinent\" seront mis sous surveillance.").FontSize(9.5f);

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, "Parties prenantes importantes");
                        c.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(cd => { cd.RelativeColumn(2); cd.RelativeColumn(3); cd.RelativeColumn(2); });
                            EnteteCellule(table.Cell(), "Partie prenante");
                            EnteteCellule(table.Cell(), "Roles et attentes");
                            EnteteCellule(table.Cell(), "Representant");
                            if (data.PartiesPrenantes.Count == 0)
                            {
                                table.Cell().ColumnSpan(3).PaddingVertical(6).Text("Aucune partie prenante renseignee.").FontSize(8.5f).Italic().FontColor(GrisTexte);
                            }
                            else
                            {
                                for (var i = 0; i < data.PartiesPrenantes.Count; i++)
                                {
                                    var p = data.PartiesPrenantes[i];
                                    var alt = i % 2 == 1;
                                    CelluleZebra(table.Cell(), p.Nom, alt, police: SansSemiBold);
                                    CelluleZebra(table.Cell(), p.RolesEtAttentes, alt);
                                    CelluleZebra(table.Cell(), p.Representant, alt);
                                }
                            }
                        });
                    });

                    col.Item().Column(c => ConstruireSectionMethodologie(c));

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, "Tableau recapitulatif des sources de risque et objectifs vises");
                        foreach (var (nom, code, description) in Themes)
                        {
                            var liste = nom switch
                            {
                                "Technologique" => data.CouplesTechnologique,
                                "Organisationnel" => data.CouplesOrganisationnel,
                                "Personnes" => data.CouplesPersonnes,
                                _ => data.CouplesPhysique,
                            };
                            c.Item().PaddingTop(10).Column(sc =>
                            {
                                sc.Item().Text("Mesures " + nom + " (" + code + ")").FontFamily(SansSemiBold).FontSize(10.5f).FontColor(BleuFrance);
                                sc.Item().PaddingBottom(4).Text(description).FontSize(8.5f).FontColor(GrisTexte);

                                if (liste.Count == 0)
                                {
                                    sc.Item().Text("Aucun couple renseigne pour ce theme.").FontSize(8.5f).Italic().FontColor(GrisTexte);
                                    return;
                                }

                                sc.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(cd =>
                                    {
                                        cd.RelativeColumn(2); cd.RelativeColumn(2); cd.RelativeColumn(3);
                                        cd.ConstantColumn(55); cd.ConstantColumn(55); cd.ConstantColumn(70);
                                    });
                                    EnteteCellule(table.Cell(), "Source de risque");
                                    EnteteCellule(table.Cell(), "Objectif vise");
                                    EnteteCellule(table.Cell(), "Contexte / vulnerabilite");
                                    EnteteCellule(table.Cell(), "Motiv.");
                                    EnteteCellule(table.Cell(), "Ress.");
                                    EnteteCellule(table.Cell(), "Pertinence");

                                    for (var i = 0; i < liste.Count; i++)
                                    {
                                        var cp = liste[i];
                                        var alt = i % 2 == 1;
                                        CelluleZebra(table.Cell(), cp.LibelleSourceRisque, alt);
                                        CelluleZebra(table.Cell(), cp.LibelleObjectifVise, alt);
                                        CelluleZebra(table.Cell(), cp.ContexteVulnerabilite, alt);
                                        CelluleZebra(table.Cell(), cp.Motivation.ToString(), alt);
                                        CelluleZebra(table.Cell(), cp.Ressources.ToString(), alt);
                                        CellulePertinence(table, cp, alt);
                                    }
                                });
                            });
                        }
                    });

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, "Cartographie de synthese des couples SR/OV");
                        c.Item().PaddingTop(6).Text("Repartition par niveau de pertinence").FontFamily(SansSemiBold).FontSize(10);
                        c.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(cd => { cd.RelativeColumn(3); cd.ConstantColumn(90); cd.ConstantColumn(90); });
                            EnteteCellule(table.Cell(), "Niveau de pertinence");
                            EnteteCellule(table.Cell(), "Nombre de couples");
                            EnteteCellule(table.Cell(), "Pourcentage");
                            for (var i = 0; i < data.Repartition.Niveaux.Count; i++)
                            {
                                var n = data.Repartition.Niveaux[i];
                                var alt = i % 2 == 1;
                                CelluleZebra(table.Cell(), LibellePertinence(n.Niveau), alt, couleur: CouleurPertinence(n.Niveau), police: MonoMedium);
                                CelluleZebra(table.Cell(), n.Nombre.ToString(), alt);
                                CelluleZebra(table.Cell(), n.Pourcentage.ToString("0.00") + " %", alt);
                            }
                        });
                    });

                    col.Item().Column(c =>
                    {
                        var tousLesCouples = data.CouplesTechnologique
                            .Concat(data.CouplesOrganisationnel)
                            .Concat(data.CouplesPersonnes)
                            .Concat(data.CouplesPhysique)
                            .ToList();

                        var retenus = tousLesCouples.Where(x => x.Pertinence is "TresPertinent" or "PlutotPertinent")
                            .OrderByDescending(x => x.Pertinence == "TresPertinent").ToList();
                        var surveillance = tousLesCouples.Where(x => x.Pertinence is "MoyennementPertinent" or "PeuPertinent").ToList();

                        SectionTitre(c, "Couples retenus pour l'Atelier 3 (scenarios strategiques)");
                        if (retenus.Count == 0)
                        {
                            c.Item().PaddingTop(4).Text("Aucun couple retenu a ce stade.").FontSize(8.5f).Italic().FontColor(GrisTexte);
                        }
                        else
                        {
                            foreach (var cp in retenus)
                            {
                                c.Item().PaddingTop(3).Text("- " + cp.LibelleSourceRisque + " / " + cp.LibelleObjectifVise + " (" + LibellePertinence(cp.Pertinence) + ")").FontSize(9);
                                if (cp.PertinenceEstJugementExpert)
                                    c.Item().PaddingLeft(10).Text("Niveau determine par jugement d'expert de l'analyste : " + cp.JustificationPertinence).FontSize(7.5f).Italic().FontColor(GrisTexte);
                            }
                        }

                        c.Item().PaddingTop(12).Text("Couples mis sous surveillance (non retenus pour l'Atelier 3)").FontFamily(SansSemiBold).FontSize(11);
                        if (surveillance.Count == 0)
                        {
                            c.Item().PaddingTop(4).Text("Aucun couple sous surveillance.").FontSize(8.5f).Italic().FontColor(GrisTexte);
                        }
                        else
                        {
                            foreach (var cp in surveillance)
                            {
                                c.Item().PaddingTop(3).Text("- " + cp.LibelleSourceRisque + " / " + cp.LibelleObjectifVise + " (" + LibellePertinence(cp.Pertinence) + ")").FontSize(9).FontColor(GrisTexte);
                                if (cp.PertinenceEstJugementExpert)
                                    c.Item().PaddingLeft(10).Text("Niveau determine par jugement d'expert de l'analyste : " + cp.JustificationPertinence).FontSize(7.5f).Italic().FontColor(GrisTexte);
                            }
                        }
                    });
                });

                page.Footer().Column(col =>
                {
                    col.Item().PaddingBottom(4).LineHorizontal(0.6f).LineColor(GrisLigne);
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("EBIOS Risk Manager -- Livrable Atelier 2").FontFamily(Mono).FontSize(7).FontColor(GrisTexte);
                        row.RelativeItem().AlignRight().Text(DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm")).FontFamily(MonoMedium).FontSize(7).FontColor(GrisTexte);
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void ConstruireSectionMethodologie(QuestPDF.Fluent.ColumnDescriptor c)
    {
        SectionTitre(c, "Approche methodologique d'evaluation de la pertinence SR/OV");
        c.Item().PaddingTop(4).Text("Un risque : possibilite qu'un evenement redoute survienne et que ses effets perturbent les missions de l'objet de l'etude. L'evaluation se base sur la formule : Niveau de risque = Gravite x Vraisemblance.").FontSize(9);
        c.Item().PaddingTop(4).Text("Deux criteres majeurs caracterisent la dangerosite d'un couple SR/OV : sa motivation ou determination, et ses ressources techniques et financieres. Seuls les couples \"Tres pertinent\" et \"Plutot pertinent\" seront retenus pour la suite de l'etude (Atelier 3).").FontSize(9);

        c.Item().PaddingTop(8).Text("Motivation").FontFamily(SansSemiBold).FontSize(9.5f);
        c.Item().PaddingTop(3).Table(table =>
        {
            table.ColumnsDefinition(cd => { cd.RelativeColumn(2); cd.ConstantColumn(45); cd.RelativeColumn(6); });
            EnteteCellule(table.Cell(), "Designation");
            EnteteCellule(table.Cell(), "Echelle");
            EnteteCellule(table.Cell(), "Signification");
            LigneMotivation(table, "Fortement motive", "4", "La SR considere l'objet de l'etude comme une cible prioritaire. Volonte durable, moyens importants mobilises malgre les obstacles.", 0);
            LigneMotivation(table, "Motive", "3", "La SR poursuit un objectif clair (financier, politique, ideologique ou personnel) et investit du temps et des ressources.", 1);
            LigneMotivation(table, "Significatif", "2", "La SR recherche un gain limite ou un objectif ponctuel. Abandonne facilement si les premieres protections sont efficaces.", 0);
            LigneMotivation(table, "Tres peu motive", "1", "La SR n'a qu'un interet limite pour les actifs de l'objet de l'etude. Attaque opportuniste, sans objectif strategique.", 1);
        });

        c.Item().PaddingTop(8).Text("Ressources techniques et financieres").FontFamily(SansSemiBold).FontSize(9.5f);
        c.Item().PaddingTop(3).Table(table =>
        {
            table.ColumnsDefinition(cd => { cd.RelativeColumn(2); cd.ConstantColumn(45); cd.RelativeColumn(6); });
            EnteteCellule(table.Cell(), "Designation");
            EnteteCellule(table.Cell(), "Echelle");
            EnteteCellule(table.Cell(), "Signification");
            LigneMotivation(table, "Illimitees", "4", "Developper ou acquerir des outils de tres haut niveau, financer des operations de longue duree, mobiliser des experts.", 0);
            LigneMotivation(table, "Importantes", "3", "Conduire des attaques complexes et prolongees contre des infrastructures critiques.", 1);
            LigneMotivation(table, "Moderees", "2", "Acquerir des outils specialises, louer des infrastructures d'attaque ou mobiliser une petite equipe.", 0);
            LigneMotivation(table, "Limitees", "1", "Utilise principalement des outils gratuits ou accessibles, attaques simples et ponctuelles.", 1);
        });

        c.Item().PaddingTop(8).Text("Matrice d'evaluation utilisee").FontFamily(SansSemiBold).FontSize(9.5f);
        c.Item().PaddingTop(3).Table(table =>
        {
            table.ColumnsDefinition(cd =>
            {
                cd.ConstantColumn(90);
                cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn();
            });
            table.Cell().Background(GrisFond).Padding(5).Text("");
            foreach (var r in new[] { "1 (Limitees)", "2 (Moderees)", "3 (Importantes)", "4 (Illimitees)" })
                EnteteCellule(table.Cell(), r);

            LigneMatrice(table, "4 (Fortement motive)", "MoyennementPertinent", "PlutotPertinent", "TresPertinent", "TresPertinent");
            LigneMatrice(table, "3 (Motive)", "MoyennementPertinent", "PlutotPertinent", "PlutotPertinent", "TresPertinent");
            LigneMatrice(table, "2 (Significatif)", "PeuPertinent", "MoyennementPertinent", "PlutotPertinent", "PlutotPertinent");
            LigneMatrice(table, "1 (Tres peu motive)", "PeuPertinent", "PeuPertinent", "MoyennementPertinent", "MoyennementPertinent");
        });
    }

    private static void LigneMotivation(QuestPDF.Fluent.TableDescriptor table, string designation, string echelle, string signification, int altIndex)
    {
        var alt = altIndex % 2 == 1;
        CelluleZebra(table.Cell(), designation, alt, police: SansSemiBold);
        table.Cell().Background(alt ? GrisFond : Colors.White).BorderBottom(0.6f).BorderColor(GrisLigne).PaddingVertical(5).AlignCenter().Text(echelle).FontFamily(MonoMedium).FontSize(8.5f);
        CelluleZebra(table.Cell(), signification, alt, taille: 8);
    }

    private static void LigneMatrice(QuestPDF.Fluent.TableDescriptor table, string label, string p1, string p2, string p3, string p4)
    {
        table.Cell().Background(BleuFranceClair).Padding(5).Text(label).FontFamily(MonoMedium).FontSize(7.5f).FontColor(BleuFrance);
        foreach (var p in new[] { p1, p2, p3, p4 })
            table.Cell().BorderBottom(0.6f).BorderColor(GrisLigne).PaddingVertical(5).AlignCenter().Text(LibellePertinence(p)).FontFamily(MonoMedium).FontSize(7).FontColor(CouleurPertinence(p));
    }

    private static void CellulePertinence(QuestPDF.Fluent.TableDescriptor table, CoupleSrOvData cp, bool alterne)
    {
        var conteneur = alterne ? table.Cell().Background(GrisFond) : table.Cell();
        conteneur.BorderBottom(0.6f).BorderColor(GrisLigne).PaddingVertical(5).PaddingHorizontal(6).Column(cc =>
        {
            cc.Item().Text(LibellePertinence(cp.Pertinence)).FontFamily(MonoMedium).FontSize(7.5f).FontColor(CouleurPertinence(cp.Pertinence));
            if (cp.PertinenceEstJugementExpert)
                cc.Item().Text("Jugement d'expert").FontSize(6).Italic().FontColor(GrisTexte);
        });
    }

    private static string LibellePertinence(string p) => p switch
    {
        "TresPertinent" => "Tres pertinent",
        "PlutotPertinent" => "Plutot pertinent",
        "MoyennementPertinent" => "Moyennement pertinent",
        _ => "Peu pertinent",
    };

    private static string CouleurPertinence(string p) => p switch
    {
        "TresPertinent" => RougeAlerte,
        "PlutotPertinent" => "#BA7517",
        "MoyennementPertinent" => GrisTexte,
        _ => VertConforme,
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

    private static void CelluleZebra(QuestPDF.Infrastructure.IContainer cell, string texte, bool alterne, string? couleur = null, string? police = null, float? taille = null)
    {
        var conteneur = alterne ? cell.Background(GrisFond) : cell;
        var t = conteneur.BorderBottom(0.6f).BorderColor(GrisLigne).PaddingVertical(5).PaddingHorizontal(6).Text(texte);
        t.FontSize(taille ?? 8.5f);
        t.FontColor(couleur ?? Encre);
        if (police != null) t.FontFamily(police);
    }
}
