using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EbiosRM.Api.Modules.Reporting;

public sealed class RapportAtelier3PdfGenerator
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

    public byte[] Generer(RapportAtelier3Data data)
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
                    col.Item().PaddingTop(2).Text("Atelier 3 -- Scenarios strategiques").FontFamily(SerifTitreSemiBold).FontSize(19).FontColor(Encre);
                    col.Item().Text(data.NomEtude).FontFamily(Mono).FontSize(8).FontColor(GrisTexte);
                    col.Item().PaddingTop(10).LineHorizontal(1.4f).LineColor(BleuFrance);
                });

                page.Content().PaddingTop(18).Column(col =>
                {
                    col.Spacing(20);

                    col.Item().Text("Ce document presente la cartographie de la dangerosite de l'ecosysteme (parties prenantes evaluees selon leur dependance, penetration, maturite cyber et confiance) ainsi que les scenarios strategiques construits a partir des couples source de risque / objectif vise retenus en Atelier 2.").FontSize(9.5f);

                    col.Item().Column(c => ConstruireSectionMethodologieDangerosite(c));

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, "Cartographie de la dangerosite de l'ecosysteme");
                        c.Item().PaddingTop(4).Text("Niveau de dangerosite = (Dependance x Penetration) / (Maturite cyber x Confiance) -- formule officielle EBIOS Risk Manager, calculee automatiquement.").FontSize(8.5f).Italic().FontColor(GrisTexte);
                        c.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.RelativeColumn(2); cd.ConstantColumn(60); cd.RelativeColumn(2);
                                cd.ConstantColumn(42); cd.ConstantColumn(42); cd.ConstantColumn(42); cd.ConstantColumn(42); cd.ConstantColumn(90);
                            });
                            EnteteCellule(table.Cell(), "Partie prenante");
                            EnteteCellule(table.Cell(), "Categorie");
                            EnteteCellule(table.Cell(), "Representant");
                            EnteteCellule(table.Cell(), "Dep.");
                            EnteteCellule(table.Cell(), "Pen.");
                            EnteteCellule(table.Cell(), "Mat.");
                            EnteteCellule(table.Cell(), "Conf.");
                            EnteteCellule(table.Cell(), "Niveau / Zone");

                            if (data.PartiesPrenantes.Count == 0)
                            {
                                table.Cell().ColumnSpan(8).PaddingVertical(6).Text("Aucune partie prenante renseignee.").FontSize(8.5f).Italic().FontColor(GrisTexte);
                            }
                            else
                            {
                                for (var i = 0; i < data.PartiesPrenantes.Count; i++)
                                {
                                    var p = data.PartiesPrenantes[i];
                                    var alt = i % 2 == 1;
                                    CelluleZebra(table.Cell(), p.Nom, alt, police: SansSemiBold);
                                    CelluleZebra(table.Cell(), p.LibelleCategorie, alt, taille: 7.5f);
                                    CelluleZebra(table.Cell(), p.Representant, alt);
                                    CelluleZebra(table.Cell(), p.Dependance?.ToString() ?? "--", alt);
                                    CelluleZebra(table.Cell(), p.Penetration?.ToString() ?? "--", alt);
                                    CelluleZebra(table.Cell(), p.MaturiteCyber?.ToString() ?? "--", alt);
                                    CelluleZebra(table.Cell(), p.Confiance?.ToString() ?? "--", alt);
                                    if (p.NiveauDangerosite is null || p.Zone is null)
                                        CelluleZebra(table.Cell(), "Non evaluee", alt, couleur: GrisTexte, police: MonoMedium, taille: 7.5f);
                                    else
                                        CelluleNiveauDangerosite(table, p.NiveauDangerosite.Value, p.Zone, p.DangerositeEstJugementExpert, alt);
                                }
                            }
                        });

                        var critiques = data.PartiesPrenantes.Where(p => p.Zone is "Controle" or "Danger").ToList();
                        if (critiques.Count > 0)
                        {
                            c.Item().PaddingTop(8).Text(text =>
                            {
                                text.Span(critiques.Count + " partie(s) prenante(s) critique(s)").FontFamily(SansSemiBold).FontSize(9);
                                text.Span(" (zone de controle ou de danger, perimetre reel de l'analyse) : ").FontSize(9);
                                text.Span(string.Join(", ", critiques.Select(p => p.Nom))).FontSize(9).Italic();
                            });
                        }
                    });

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, "Mesures de securite sur l'ecosysteme");
                        c.Item().PaddingTop(4).Text("Pour chaque partie prenante critique : mesures de reduction du risque proposees, et dangerosite residuelle apres application (recalculee sur reevaluation des 4 criteres).").FontSize(8.5f).Italic().FontColor(GrisTexte);

                        var critiques = data.PartiesPrenantes.Where(p => p.Zone is "Controle" or "Danger").ToList();
                        if (critiques.Count == 0)
                        {
                            c.Item().PaddingTop(4).Text("Aucune partie prenante critique a ce stade.").FontSize(8.5f).Italic().FontColor(GrisTexte);
                        }
                        else
                        {
                            foreach (var p in critiques)
                            {
                                c.Item().PaddingTop(10).Column(pc =>
                                {
                                    pc.Item().Text(p.Nom).FontFamily(SansSemiBold).FontSize(10).FontColor(BleuFrance);

                                    if (p.Mesures.Count == 0)
                                    {
                                        pc.Item().PaddingTop(2).Text("Aucune mesure proposee.").FontSize(8).Italic().FontColor(GrisTexte);
                                    }
                                    else
                                    {
                                        foreach (var mesure in p.Mesures)
                                            pc.Item().PaddingTop(2).Text("- " + mesure).FontSize(8.5f);
                                    }

                                    pc.Item().PaddingTop(4).Row(row =>
                                    {
                                        row.ConstantItem(120).Column(cc =>
                                        {
                                            cc.Item().Text("DANGEROSITE INITIALE").FontFamily(MonoMedium).FontSize(7).FontColor(GrisTexte);
                                            cc.Item().Text((p.NiveauDangerosite?.ToString("0.00") ?? "--") + " -- " + LibelleZone(p.Zone)).FontFamily(MonoMedium).FontSize(9).FontColor(CouleurZone(p.Zone));
                                            if (p.DangerositeEstJugementExpert)
                                                cc.Item().Text("Jugement d'expert").FontSize(6).Italic().FontColor(GrisTexte);
                                        });
                                        row.ConstantItem(20).AlignMiddle().Text("->").FontColor(GrisTexte);
                                        row.ConstantItem(140).Column(cc =>
                                        {
                                            cc.Item().Text("DANGEROSITE RESIDUELLE").FontFamily(MonoMedium).FontSize(7).FontColor(GrisTexte);
                                            if (p.NiveauDangerositeResiduel is null)
                                                cc.Item().Text("Non reevaluee").FontFamily(MonoMedium).FontSize(9).FontColor(GrisTexte);
                                            else
                                                cc.Item().Text(p.NiveauDangerositeResiduel.Value.ToString("0.00") + " -- " + LibelleZone(p.ZoneResiduelle)).FontFamily(MonoMedium).FontSize(9).FontColor(CouleurZone(p.ZoneResiduelle));
                                            if (p.DangerositeResiduelleEstJugementExpert)
                                                cc.Item().Text("Jugement d'expert").FontSize(6).Italic().FontColor(GrisTexte);
                                        });
                                    });
                                    if (p.DangerositeEstJugementExpert && p.JustificationDangerosite is not null)
                                        pc.Item().PaddingTop(2).Text("Dangerosite initiale -- jugement d'expert : " + p.JustificationDangerosite).FontSize(7.5f).Italic().FontColor(GrisTexte);
                                    if (p.DangerositeResiduelleEstJugementExpert && p.JustificationDangerositeResiduelle is not null)
                                        pc.Item().PaddingTop(2).Text("Dangerosite residuelle -- jugement d'expert : " + p.JustificationDangerositeResiduelle).FontSize(7.5f).Italic().FontColor(GrisTexte);
                                });
                            }
                        }
                    });

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, "Scenarios strategiques et chemins d'attaque");
                        c.Item().PaddingTop(4).Text("1 couple SR/OV retenu = 1 scenario stratégique. Chaque scenario cible un evenement redoute (Atelier 1) dont il herite la gravite -- identique pour le scenario et tous ses chemins d'attaque. 1 scenario => plusieurs chemins d'attaque possibles (direct, ou via une ou plusieurs parties prenantes de l'ecosysteme generant des evenements intermediaires).").FontSize(8.5f).Italic().FontColor(GrisTexte);
                        if (data.ScenariosStrategiques.Count == 0)
                        {
                            c.Item().PaddingTop(4).Text("Aucun scenario strategique cree a ce stade.").FontSize(8.5f).Italic().FontColor(GrisTexte);
                        }
                        else
                        {
                            foreach (var s in data.ScenariosStrategiques)
                            {
                                c.Item().PaddingTop(12).Column(sc =>
                                {
                                    sc.Item().Row(row =>
                                    {
                                        row.RelativeItem().Text(s.LibelleSourceRisque + " -- " + s.LibelleObjectifVise).FontFamily(SansSemiBold).FontSize(10.5f).FontColor(BleuFrance);
                                        row.ConstantItem(90).AlignRight().Text(LibellePertinence(s.Pertinence)).FontFamily(MonoMedium).FontSize(7.5f).FontColor(CouleurPertinence(s.Pertinence));
                                    });
                                    sc.Item().PaddingTop(3).Text(s.Description).FontSize(9);
                                    sc.Item().PaddingTop(3).Text(text =>
                                    {
                                        text.Span("Cible : ").FontSize(8).FontColor(GrisTexte);
                                        text.Span(s.LibelleEvenementRedoute).FontSize(8).FontColor(GrisTexte);
                                        text.Span("  --  Gravite ").FontSize(8).FontColor(GrisTexte);
                                        text.Span(s.Gravite.ToString()).FontFamily(MonoMedium).FontSize(8).FontColor(CouleurGravite(s.Gravite));
                                    });

                                    if (s.CheminsAttaque.Count == 0)
                                    {
                                        sc.Item().PaddingTop(4).Text("Aucun chemin d'attaque defini pour ce scenario.").FontSize(8).Italic().FontColor(GrisTexte);
                                    }
                                    else
                                    {
                                        foreach (var chemin in s.CheminsAttaque)
                                        {
                                            sc.Item().PaddingTop(6).PaddingLeft(10).BorderLeft(1.4f).BorderColor(BleuFranceClair).PaddingVertical(2).Column(cc =>
                                            {
                                                cc.Item().PaddingLeft(6).Text(chemin.Description).FontFamily(SansSemiBold).FontSize(8.5f);
                                                if (chemin.EvenementsIntermediaires.Count == 0)
                                                {
                                                    cc.Item().PaddingLeft(6).PaddingTop(1).Text("Chemin direct -- aucune partie prenante traversee.").FontSize(7.5f).Italic().FontColor(GrisTexte);
                                                }
                                                else
                                                {
                                                    for (var i = 0; i < chemin.EvenementsIntermediaires.Count; i++)
                                                    {
                                                        var ei = chemin.EvenementsIntermediaires[i];
                                                        cc.Item().PaddingLeft(6).PaddingTop(1).Text((i + 1) + ". " + ei.LibellePartiePrenante + " -- " + ei.Description).FontSize(7.5f).FontColor(GrisTexte);
                                                    }
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
                        row.RelativeItem().Text("EBIOS Risk Manager -- Livrable Atelier 3").FontFamily(Mono).FontSize(7).FontColor(GrisTexte);
                        row.RelativeItem().AlignRight().Text(DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm")).FontFamily(MonoMedium).FontSize(7).FontColor(GrisTexte);
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    private static string LibellePertinence(string p) => p switch
    {
        "TresPertinent" => "Tres pertinent",
        "PlutotPertinent" => "Plutot pertinent",
        "MoyennementPertinent" => "Moyennement pertinent",
        "PeuPertinent" => "Peu pertinent",
        _ => "--",
    };

    private static string CouleurPertinence(string p) => p switch
    {
        "TresPertinent" => RougeAlerte,
        "PlutotPertinent" => OrangeAlerte,
        "MoyennementPertinent" => GrisTexte,
        _ => VertConforme,
    };

    private static string CouleurGravite(int gravite)
    {
        if (gravite >= 4) return RougeAlerte;
        if (gravite >= 3) return OrangeAlerte;
        if (gravite >= 2) return "#A68A2A";
        return VertConforme;
    }

    private static void CelluleNiveauDangerosite(QuestPDF.Fluent.TableDescriptor table, double niveau, string? zone, bool jugementExpert, bool alterne)
    {
        var conteneur = alterne ? table.Cell().Background(GrisFond) : table.Cell();
        conteneur.BorderBottom(0.6f).BorderColor(GrisLigne).PaddingVertical(5).PaddingHorizontal(6).Column(cc =>
        {
            cc.Item().Text(niveau.ToString("0.00") + " -- " + LibelleZone(zone)).FontFamily(MonoMedium).FontSize(7.5f).FontColor(CouleurZone(zone));
            if (jugementExpert)
                cc.Item().Text("Jugement d'expert").FontSize(6).Italic().FontColor(GrisTexte);
        });
    }

    private static string LibelleZone(string? zone) => zone switch
    {
        "Danger" => "Zone de danger",
        "Controle" => "Zone de controle",
        "Veille" => "Zone de veille",
        _ => "--",
    };

    private static string CouleurZone(string? zone) => zone switch
    {
        "Danger" => RougeAlerte,
        "Controle" => OrangeAlerte,
        _ => VertConforme,
    };

    private static readonly (string Designation, string Echelle, string Dependance, string Penetration, string Maturite, string Confiance)[] LignesEchelleDangerosite = new[]
    {
        ("Tres eleve", "4", "Relation indispensable et unique (pas de substitution possible a court terme).",
            "Acces administrateur a des equipements d'infrastructure (annuaires, DNS, DHCP, pare-feu, hyperviseurs...) ou acces physique aux salles serveurs.",
            "Politique de management du risque integree, dimension proactive.",
            "Intentions parfaitement connues et pleinement compatibles avec celles de l'organisation."),
        ("Eleve", "3", "Relation indispensable mais non exclusive.",
            "Acces administrateur a des serveurs metier (fichiers, bases de donnees, web, applicatifs).",
            "Politique globale appliquee en mode reactif, avec recherche de centralisation et d'anticipation.",
            "Intentions connues et probablement positives."),
        ("Significatif", "2", "Relation utile aux fonctions strategiques.",
            "Acces administrateur a des terminaux utilisateurs, ou acces physique aux sites de l'organisation.",
            "Regles d'hygiene et reglementation prises en compte, sans politique globale, mode reactif.",
            "Intentions considerees comme neutres."),
        ("Tres peu", "1", "Relation non necessaire aux fonctions strategiques.",
            "Pas d'acces, ou acces utilisateur a des terminaux (poste de travail, ordiphone...).",
            "Regles d'hygiene appliquees ponctuellement et non formalisees. Capacite de reaction sur incident incertaine.",
            "Intentions non evaluables."),
    };

    private static void ConstruireSectionMethodologieDangerosite(QuestPDF.Fluent.ColumnDescriptor c)
    {
        SectionTitre(c, "Grille officielle d'evaluation de la dangerosite de l'ecosysteme");
        c.Item().PaddingTop(4).Text("Chaque partie prenante est evaluee selon 4 criteres (echelle 1 a 4) repartis en deux axes : l'exposition numerique (Dependance, Penetration) et la fiabilite numerique (Maturite cyber, Confiance).").FontSize(9);
        c.Item().PaddingTop(6).Table(table =>
        {
            table.ColumnsDefinition(cd =>
            {
                cd.RelativeColumn(1); cd.ConstantColumn(30);
                cd.RelativeColumn(3); cd.RelativeColumn(3); cd.RelativeColumn(3); cd.RelativeColumn(3);
            });
            EnteteCellule(table.Cell(), "Niveau");
            EnteteCellule(table.Cell(), "Ech.");
            EnteteCellule(table.Cell(), "Dependance");
            EnteteCellule(table.Cell(), "Penetration");
            EnteteCellule(table.Cell(), "Maturite cyber");
            EnteteCellule(table.Cell(), "Confiance");

            for (var i = 0; i < LignesEchelleDangerosite.Length; i++)
            {
                var l = LignesEchelleDangerosite[i];
                var alt = i % 2 == 1;
                CelluleZebra(table.Cell(), l.Designation, alt, police: SansSemiBold);
                table.Cell().Background(alt ? GrisFond : QuestPDF.Helpers.Colors.White).BorderBottom(0.6f).BorderColor(GrisLigne).PaddingVertical(5).AlignCenter().Text(l.Echelle).FontFamily(MonoMedium).FontSize(8.5f);
                CelluleZebra(table.Cell(), l.Dependance, alt, taille: 7.5f);
                CelluleZebra(table.Cell(), l.Penetration, alt, taille: 7.5f);
                CelluleZebra(table.Cell(), l.Maturite, alt, taille: 7.5f);
                CelluleZebra(table.Cell(), l.Confiance, alt, taille: 7.5f);
            }
        });

        c.Item().PaddingTop(8).Table(table =>
        {
            table.ColumnsDefinition(cd => { cd.RelativeColumn(2); cd.RelativeColumn(2); cd.RelativeColumn(3); });
            EnteteCellule(table.Cell(), "Zone (niveau de dangerosite)");
            EnteteCellule(table.Cell(), "Acceptabilite");
            EnteteCellule(table.Cell(), "Recommandation");
            CelluleZebra(table.Cell(), "Danger (>= 4)", false, couleur: RougeAlerte, police: MonoMedium);
            CelluleZebra(table.Cell(), "Inacceptable", false);
            CelluleZebra(table.Cell(), "Reduction du risque, ou refus d'etablir l'interaction.", false, taille: 8);
            CelluleZebra(table.Cell(), "Controle (1 a 4)", true, couleur: OrangeAlerte, police: MonoMedium);
            CelluleZebra(table.Cell(), "Tolerable sous controle", true);
            CelluleZebra(table.Cell(), "Enrolement dans le management du risque : surveillance accrue, audit, plan d'amelioration.", true, taille: 8);
            CelluleZebra(table.Cell(), "Veille (< 1)", false, couleur: VertConforme, police: MonoMedium);
            CelluleZebra(table.Cell(), "Acceptable en l'etat", false);
            CelluleZebra(table.Cell(), "Sans objet (dangerosite residuelle).", false, taille: 8);
        });
    }

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
