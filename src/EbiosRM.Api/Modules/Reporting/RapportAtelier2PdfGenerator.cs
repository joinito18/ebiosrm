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

    private static readonly (string Nom, string Code, string DescriptionFr, string DescriptionEn)[] Themes = new[]
    {
        ("Technologique", "A.8", "Defaillances techniques : firewall en fin de vie, OS obsoletes, antivirus expires, sauvegardes non chiffrees.", "Technical failures: end-of-life firewall, obsolete OS, expired antivirus, unencrypted backups."),
        ("Organisationnel", "A.5", "Absence de gouvernance, de politiques formelles et de gestion des tiers.", "No governance, formal policies or third-party management."),
        ("Personnes", "A.6", "Risques lies aux acteurs internes, a la gestion des identites et aux facteurs humains.", "Risks tied to internal actors, identity management and human factors."),
        ("Physique", "A.7", "Risques lies a l'environnement physique et aux infrastructures des sites.", "Risks tied to the physical environment and site infrastructure."),
    };

    public byte[] Generer(RapportAtelier2Data data, bool anglais = false)
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
                    col.Item().PaddingTop(2).Text(T("Atelier 2 -- Sources de risque", "Workshop 2 -- Risk origins")).FontFamily(SerifTitreSemiBold).FontSize(19).FontColor(Encre);
                    col.Item().Text(data.NomEtude).FontFamily(Mono).FontSize(8).FontColor(GrisTexte);
                    col.Item().PaddingTop(10).LineHorizontal(1.4f).LineColor(BleuFrance);
                });

                page.Content().PaddingTop(18).Column(col =>
                {
                    col.Spacing(20);

                    col.Item().Text(anglais ? "This document identifies the Risk Origin / Target Objective pairs relevant to the object of study. \"Highly relevant\" pairs are addressed first in Workshop 3; \"Fairly relevant\" pairs are placed under watch." : "Ce document identifie les couples Source de Risque / Objectif Vise pertinents pour l'objet de l'etude. Les couples \"Tres pertinent\" seront traites en priorite dans l'Atelier 3, les couples \"Plutot pertinent\" seront mis sous surveillance.").FontSize(9.5f);

                    col.Item().Column(c => ConstruireSectionMethodologie(c, anglais));

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, T("Tableau recapitulatif des sources de risque et objectifs vises", "Summary table of risk origins and target objectives"));
                        foreach (var (nom, code, descriptionFr, descriptionEn) in Themes)
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
                                sc.Item().Text(T("Mesures ", "Measures ") + LibellesRapport.Theme(nom, anglais) + " (" + code + ")").FontFamily(SansSemiBold).FontSize(10.5f).FontColor(BleuFrance);
                                sc.Item().PaddingBottom(4).Text(anglais ? descriptionEn : descriptionFr).FontSize(8.5f).FontColor(GrisTexte);

                                if (liste.Count == 0)
                                {
                                    sc.Item().Text(T("Aucun couple renseigne pour ce theme.", "No pair recorded for this theme.")).FontSize(8.5f).Italic().FontColor(GrisTexte);
                                    return;
                                }

                                sc.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(cd =>
                                    {
                                        cd.RelativeColumn(2); cd.RelativeColumn(2); cd.RelativeColumn(3);
                                        cd.ConstantColumn(55); cd.ConstantColumn(55); cd.ConstantColumn(70);
                                    });
                                    EnteteCellule(table.Cell(), T("Source de risque", "Risk origin"));
                                    EnteteCellule(table.Cell(), T("Objectif vise", "Target objective"));
                                    EnteteCellule(table.Cell(), T("Contexte / vulnerabilite", "Context / vulnerability"));
                                    EnteteCellule(table.Cell(), "Motiv.");
                                    EnteteCellule(table.Cell(), T("Ress.", "Res."));
                                    EnteteCellule(table.Cell(), T("Pertinence", "Relevance"));

                                    for (var i = 0; i < liste.Count; i++)
                                    {
                                        var cp = liste[i];
                                        var alt = i % 2 == 1;
                                        CelluleZebra(table.Cell(), cp.LibelleSourceRisque, alt);
                                        CelluleZebra(table.Cell(), cp.LibelleObjectifVise, alt);
                                        CelluleZebra(table.Cell(), cp.ContexteVulnerabilite, alt);
                                        CelluleZebra(table.Cell(), cp.Motivation.ToString(), alt);
                                        CelluleZebra(table.Cell(), cp.Ressources.ToString(), alt);
                                        CellulePertinence(table, cp, alt, anglais);
                                    }
                                });
                            });
                        }
                    });

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, T("Cartographie de synthese des couples SR/OV", "Summary map of RO/TO pairs"));
                        c.Item().PaddingTop(6).Text(T("Repartition par niveau de pertinence", "Breakdown by relevance level")).FontFamily(SansSemiBold).FontSize(10);
                        c.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(cd => { cd.RelativeColumn(3); cd.ConstantColumn(90); cd.ConstantColumn(90); });
                            EnteteCellule(table.Cell(), T("Niveau de pertinence", "Relevance level"));
                            EnteteCellule(table.Cell(), T("Nombre de couples", "Number of pairs"));
                            EnteteCellule(table.Cell(), T("Pourcentage", "Percentage"));
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

                        SectionTitre(c, T("Couples retenus pour l'Atelier 3 (scenarios strategiques)", "Pairs selected for Workshop 3 (strategic scenarios)"));
                        if (retenus.Count == 0)
                        {
                            c.Item().PaddingTop(4).Text(T("Aucun couple retenu a ce stade.", "No pair selected at this stage.")).FontSize(8.5f).Italic().FontColor(GrisTexte);
                        }
                        else
                        {
                            foreach (var cp in retenus)
                            {
                                c.Item().PaddingTop(3).Text("- " + cp.LibelleSourceRisque + " / " + cp.LibelleObjectifVise + " (" + LibellesRapport.Pertinence(cp.Pertinence, anglais) + ")").FontSize(9);
                                if (cp.PertinenceEstJugementExpert)
                                    c.Item().PaddingLeft(10).Text(T("Niveau determine par jugement d'expert de l'analyste : ", "Level set by the analyst's expert judgement: ") + cp.JustificationPertinence).FontSize(7.5f).Italic().FontColor(GrisTexte);
                            }
                        }

                        c.Item().PaddingTop(12).Text(T("Couples mis sous surveillance (non retenus pour l'Atelier 3)", "Pairs placed under watch (not selected for Workshop 3)")).FontFamily(SansSemiBold).FontSize(11);
                        if (surveillance.Count == 0)
                        {
                            c.Item().PaddingTop(4).Text(T("Aucun couple sous surveillance.", "No pair under watch.")).FontSize(8.5f).Italic().FontColor(GrisTexte);
                        }
                        else
                        {
                            foreach (var cp in surveillance)
                            {
                                c.Item().PaddingTop(3).Text("- " + cp.LibelleSourceRisque + " / " + cp.LibelleObjectifVise + " (" + LibellesRapport.Pertinence(cp.Pertinence, anglais) + ")").FontSize(9).FontColor(GrisTexte);
                                if (cp.PertinenceEstJugementExpert)
                                    c.Item().PaddingLeft(10).Text(T("Niveau determine par jugement d'expert de l'analyste : ", "Level set by the analyst's expert judgement: ") + cp.JustificationPertinence).FontSize(7.5f).Italic().FontColor(GrisTexte);
                            }
                        }
                    });
                });

                page.Footer().Column(col =>
                {
                    col.Item().PaddingBottom(4).LineHorizontal(0.6f).LineColor(GrisLigne);
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text(T("EBIOS Risk Manager -- Livrable Atelier 2", "EBIOS Risk Manager -- Workshop 2 deliverable")).FontFamily(Mono).FontSize(7).FontColor(GrisTexte);
                        row.RelativeItem().AlignRight().Text(DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm")).FontFamily(MonoMedium).FontSize(7).FontColor(GrisTexte);
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void ConstruireSectionMethodologie(QuestPDF.Fluent.ColumnDescriptor c, bool anglais)
    {
        string T(string fr, string en) => anglais ? en : fr;
        SectionTitre(c, T("Approche methodologique d'evaluation de la pertinence SR/OV", "Methodological approach to assessing RO/TO relevance"));
        c.Item().PaddingTop(4).Text(anglais ? "A risk: the possibility that a feared event occurs and its effects disrupt the missions of the object of study. The assessment relies on the formula: Risk level = Severity x Likelihood." : "Un risque : possibilite qu'un evenement redoute survienne et que ses effets perturbent les missions de l'objet de l'etude. L'evaluation se base sur la formule : Niveau de risque = Gravite x Vraisemblance.").FontSize(9);
        c.Item().PaddingTop(4).Text(anglais ? "Two major criteria characterise the threat level of an RO/TO pair: its motivation or determination, and its technical and financial resources. Only \"Highly relevant\" and \"Fairly relevant\" pairs are carried into the rest of the study (Workshop 3)." : "Deux criteres majeurs caracterisent la dangerosite d'un couple SR/OV : sa motivation ou determination, et ses ressources techniques et financieres. Seuls les couples \"Tres pertinent\" et \"Plutot pertinent\" seront retenus pour la suite de l'etude (Atelier 3).").FontSize(9);

        c.Item().PaddingTop(8).Text(T("Motivation", "Motivation")).FontFamily(SansSemiBold).FontSize(9.5f);
        c.Item().PaddingTop(3).Table(table =>
        {
            table.ColumnsDefinition(cd => { cd.RelativeColumn(2); cd.ConstantColumn(45); cd.RelativeColumn(6); });
            EnteteCellule(table.Cell(), T("Designation", "Label"));
            EnteteCellule(table.Cell(), T("Echelle", "Scale"));
            EnteteCellule(table.Cell(), T("Signification", "Meaning"));
            LigneMotivation(table, T("Fortement motive", "Strongly motivated"), "4", T("La SR considere l'objet de l'etude comme une cible prioritaire. Volonte durable, moyens importants mobilises malgre les obstacles.", "The RO regards the object of study as a priority target. Lasting resolve, significant means committed despite obstacles."), 0);
            LigneMotivation(table, T("Motive", "Motivated"), "3", T("La SR poursuit un objectif clair (financier, politique, ideologique ou personnel) et investit du temps et des ressources.", "The RO pursues a clear objective (financial, political, ideological or personal) and invests time and resources."), 1);
            LigneMotivation(table, T("Significatif", "Significant"), "2", T("La SR recherche un gain limite ou un objectif ponctuel. Abandonne facilement si les premieres protections sont efficaces.", "The RO seeks a limited gain or a one-off objective. Gives up easily if the first protections are effective."), 0);
            LigneMotivation(table, T("Tres peu motive", "Barely motivated"), "1", T("La SR n'a qu'un interet limite pour les actifs de l'objet de l'etude. Attaque opportuniste, sans objectif strategique.", "The RO has only limited interest in the assets of the object of study. Opportunistic attack, no strategic objective."), 1);
        });

        c.Item().PaddingTop(8).Text(T("Ressources techniques et financieres", "Technical and financial resources")).FontFamily(SansSemiBold).FontSize(9.5f);
        c.Item().PaddingTop(3).Table(table =>
        {
            table.ColumnsDefinition(cd => { cd.RelativeColumn(2); cd.ConstantColumn(45); cd.RelativeColumn(6); });
            EnteteCellule(table.Cell(), T("Designation", "Label"));
            EnteteCellule(table.Cell(), T("Echelle", "Scale"));
            EnteteCellule(table.Cell(), T("Signification", "Meaning"));
            LigneMotivation(table, T("Illimitees", "Unlimited"), "4", T("Developper ou acquerir des outils de tres haut niveau, financer des operations de longue duree, mobiliser des experts.", "Develop or acquire very high-end tools, fund long-running operations, mobilise experts."), 0);
            LigneMotivation(table, T("Importantes", "Substantial"), "3", T("Conduire des attaques complexes et prolongees contre des infrastructures critiques.", "Carry out complex, prolonged attacks against critical infrastructure."), 1);
            LigneMotivation(table, T("Moderees", "Moderate"), "2", T("Acquerir des outils specialises, louer des infrastructures d'attaque ou mobiliser une petite equipe.", "Acquire specialised tools, rent attack infrastructure or mobilise a small team."), 0);
            LigneMotivation(table, T("Limitees", "Limited"), "1", T("Utilise principalement des outils gratuits ou accessibles, attaques simples et ponctuelles.", "Mainly uses free or readily available tools, simple one-off attacks."), 1);
        });

        c.Item().PaddingTop(8).Text(T("Matrice d'evaluation utilisee", "Assessment matrix used")).FontFamily(SansSemiBold).FontSize(9.5f);
        c.Item().PaddingTop(3).Table(table =>
        {
            table.ColumnsDefinition(cd =>
            {
                cd.ConstantColumn(90);
                cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn();
            });
            table.Cell().Background(GrisFond).Padding(5).Text("");
            foreach (var r in (anglais ? new[] { "1 (Limited)", "2 (Moderate)", "3 (Substantial)", "4 (Unlimited)" } : new[] { "1 (Limitees)", "2 (Moderees)", "3 (Importantes)", "4 (Illimitees)" }))
                EnteteCellule(table.Cell(), r);

            LigneMatrice(table, T("4 (Fortement motive)", "4 (Strongly motivated)"), anglais, "MoyennementPertinent", "PlutotPertinent", "TresPertinent", "TresPertinent");
            LigneMatrice(table, T("3 (Motive)", "3 (Motivated)"), anglais, "MoyennementPertinent", "PlutotPertinent", "PlutotPertinent", "TresPertinent");
            LigneMatrice(table, T("2 (Significatif)", "2 (Significant)"), anglais, "PeuPertinent", "MoyennementPertinent", "PlutotPertinent", "PlutotPertinent");
            LigneMatrice(table, T("1 (Tres peu motive)", "1 (Barely motivated)"), anglais, "PeuPertinent", "PeuPertinent", "MoyennementPertinent", "MoyennementPertinent");
        });
    }

    private static void LigneMotivation(QuestPDF.Fluent.TableDescriptor table, string designation, string echelle, string signification, int altIndex)
    {
        var alt = altIndex % 2 == 1;
        CelluleZebra(table.Cell(), designation, alt, police: SansSemiBold);
        table.Cell().Background(alt ? GrisFond : Colors.White).BorderBottom(0.6f).BorderColor(GrisLigne).PaddingVertical(5).AlignCenter().Text(echelle).FontFamily(MonoMedium).FontSize(8.5f);
        CelluleZebra(table.Cell(), signification, alt, taille: 8);
    }

    private static void LigneMatrice(QuestPDF.Fluent.TableDescriptor table, string label, bool anglais, string p1, string p2, string p3, string p4)
    {
        table.Cell().Background(BleuFranceClair).Padding(5).Text(label).FontFamily(MonoMedium).FontSize(7.5f).FontColor(BleuFrance);
        foreach (var p in new[] { p1, p2, p3, p4 })
            table.Cell().BorderBottom(0.6f).BorderColor(GrisLigne).PaddingVertical(5).AlignCenter().Text(LibellesRapport.Pertinence(p, anglais)).FontFamily(MonoMedium).FontSize(7).FontColor(CouleurPertinence(p));
    }

    private static void CellulePertinence(QuestPDF.Fluent.TableDescriptor table, CoupleSrOvData cp, bool alterne, bool anglais)
    {
        string T(string fr, string en) => anglais ? en : fr;
        var conteneur = alterne ? table.Cell().Background(GrisFond) : table.Cell();
        conteneur.BorderBottom(0.6f).BorderColor(GrisLigne).PaddingVertical(5).PaddingHorizontal(6).Column(cc =>
        {
            cc.Item().Text(LibellesRapport.Pertinence(cp.Pertinence, anglais)).FontFamily(MonoMedium).FontSize(7.5f).FontColor(CouleurPertinence(cp.Pertinence));
            if (cp.PertinenceEstJugementExpert)
                cc.Item().Text(T("Jugement d'expert", "Expert judgement")).FontSize(6).Italic().FontColor(GrisTexte);
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
