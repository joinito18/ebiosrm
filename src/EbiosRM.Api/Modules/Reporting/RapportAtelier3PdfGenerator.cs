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

    public byte[] Generer(RapportAtelier3Data data, bool anglais = false)
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
                    col.Item().PaddingTop(2).Text(T("Atelier 3 -- Scenarios strategiques", "Workshop 3 -- Strategic scenarios")).FontFamily(SerifTitreSemiBold).FontSize(19).FontColor(Encre);
                    col.Item().Text(data.NomEtude).FontFamily(Mono).FontSize(8).FontColor(GrisTexte);
                    col.Item().PaddingTop(10).LineHorizontal(1.4f).LineColor(BleuFrance);
                });

                page.Content().PaddingTop(18).Column(col =>
                {
                    col.Spacing(20);

                    col.Item().Text(anglais ? "This document presents the ecosystem threat-level mapping (stakeholders assessed on their dependence, penetration, cyber maturity and trust) as well as the strategic scenarios built from the risk origin / target objective pairs selected in Workshop 2." : "Ce document presente la cartographie de la dangerosite de l'ecosysteme (parties prenantes evaluees selon leur dependance, penetration, maturite cyber et confiance) ainsi que les scenarios strategiques construits a partir des couples source de risque / objectif vise retenus en Atelier 2.").FontSize(9.5f);

                    col.Item().Column(c => ConstruireSectionMethodologieDangerosite(c, anglais));

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, T("Cartographie de la dangerosite de l'ecosysteme", "Ecosystem threat-level mapping"));
                        c.Item().PaddingTop(4).Text(anglais ? "Threat level = (Dependence x Penetration) / (Cyber maturity x Trust) -- official EBIOS Risk Manager formula, computed automatically." : "Niveau de dangerosite = (Dependance x Penetration) / (Maturite cyber x Confiance) -- formule officielle EBIOS Risk Manager, calculee automatiquement.").FontSize(8.5f).Italic().FontColor(GrisTexte);

                        var partiesEvaluees = data.PartiesPrenantes.Where(p => p.NiveauDangerosite is not null).ToList();
                        if (partiesEvaluees.Count > 0)
                        {
                            var radarInitial = data.PartiesPrenantes
                                .Select(p => new CartographieSvg.PartieRadar(p.Nom, p.LibelleCategorie, p.NiveauDangerosite, p.Zone))
                                .ToList();
                            c.Item().PaddingTop(8).AlignCenter().Width(430).Svg(CartographieSvg.RadarEcosysteme(radarInitial, anglais ? "initial" : "initiale"));

                            if (data.PartiesPrenantes.Any(p => p.NiveauDangerositeResiduel is not null))
                            {
                                var radarResiduel = data.PartiesPrenantes
                                    .Select(p => new CartographieSvg.PartieRadar(
                                        p.Nom, p.LibelleCategorie,
                                        p.NiveauDangerositeResiduel ?? p.NiveauDangerosite,
                                        p.ZoneResiduelle ?? p.Zone))
                                    .ToList();
                                c.Item().PaddingTop(10).AlignCenter().Width(430).Svg(CartographieSvg.RadarEcosysteme(radarResiduel, anglais ? "after measures (residual)" : "apres mesures (residuelle)"));
                            }
                        }

                        c.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.RelativeColumn(2); cd.ConstantColumn(60); cd.RelativeColumn(2);
                                cd.ConstantColumn(42); cd.ConstantColumn(42); cd.ConstantColumn(42); cd.ConstantColumn(42); cd.ConstantColumn(90);
                            });
                            EnteteCellule(table.Cell(), T("Partie prenante", "Stakeholder"));
                            EnteteCellule(table.Cell(), T("Categorie", "Category"));
                            EnteteCellule(table.Cell(), T("Representant", "Representative"));
                            EnteteCellule(table.Cell(), "Dep.");
                            EnteteCellule(table.Cell(), "Pen.");
                            EnteteCellule(table.Cell(), "Mat.");
                            EnteteCellule(table.Cell(), "Conf.");
                            EnteteCellule(table.Cell(), T("Niveau / Zone", "Level / Zone"));

                            if (data.PartiesPrenantes.Count == 0)
                            {
                                table.Cell().ColumnSpan(8).PaddingVertical(6).Text(T("Aucune partie prenante renseignee.", "No stakeholder recorded.")).FontSize(8.5f).Italic().FontColor(GrisTexte);
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
                                        CelluleZebra(table.Cell(), T("Non evaluee", "Not assessed"), alt, couleur: GrisTexte, police: MonoMedium, taille: 7.5f);
                                    else
                                        CelluleNiveauDangerosite(table, p.NiveauDangerosite.Value, p.Zone, p.DangerositeEstJugementExpert, alt, anglais);
                                }
                            }
                        });

                        var critiques = data.PartiesPrenantes.Where(p => p.Zone is "Controle" or "Danger").ToList();
                        if (critiques.Count > 0)
                        {
                            c.Item().PaddingTop(8).Text(text =>
                            {
                                text.Span(critiques.Count + T(" partie(s) prenante(s) critique(s)", " critical stakeholder(s)")).FontFamily(SansSemiBold).FontSize(9);
                                text.Span(T(" (zone de controle ou de danger, perimetre reel de l'analyse) : ", " (control or danger zone, real scope of the analysis): ")).FontSize(9);
                                text.Span(string.Join(", ", critiques.Select(p => p.Nom))).FontSize(9).Italic();
                            });
                        }
                    });

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, T("Mesures de securite sur l'ecosysteme", "Ecosystem security measures"));
                        c.Item().PaddingTop(4).Text(anglais ? "For each critical stakeholder: proposed risk-reduction measures, and the residual threat level after they are applied (recomputed from a re-assessment of the 4 criteria)." : "Pour chaque partie prenante critique : mesures de reduction du risque proposees, et dangerosite residuelle apres application (recalculee sur reevaluation des 4 criteres).").FontSize(8.5f).Italic().FontColor(GrisTexte);

                        var critiques = data.PartiesPrenantes.Where(p => p.Zone is "Controle" or "Danger").ToList();
                        if (critiques.Count == 0)
                        {
                            c.Item().PaddingTop(4).Text(T("Aucune partie prenante critique a ce stade.", "No critical stakeholder at this stage.")).FontSize(8.5f).Italic().FontColor(GrisTexte);
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
                                        pc.Item().PaddingTop(2).Text(T("Aucune mesure proposee.", "No measure proposed.")).FontSize(8).Italic().FontColor(GrisTexte);
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
                                            cc.Item().Text(T("DANGEROSITE INITIALE", "INITIAL THREAT LEVEL")).FontFamily(MonoMedium).FontSize(7).FontColor(GrisTexte);
                                            cc.Item().Text((p.NiveauDangerosite?.ToString("0.00") ?? "--") + " -- " + LibellesRapport.Zone(p.Zone, anglais)).FontFamily(MonoMedium).FontSize(9).FontColor(CouleurZone(p.Zone));
                                            if (p.DangerositeEstJugementExpert)
                                                cc.Item().Text(T("Jugement d'expert", "Expert judgement")).FontSize(6).Italic().FontColor(GrisTexte);
                                        });
                                        row.ConstantItem(20).AlignMiddle().Text("->").FontColor(GrisTexte);
                                        row.ConstantItem(140).Column(cc =>
                                        {
                                            cc.Item().Text(T("DANGEROSITE RESIDUELLE", "RESIDUAL THREAT LEVEL")).FontFamily(MonoMedium).FontSize(7).FontColor(GrisTexte);
                                            if (p.NiveauDangerositeResiduel is null)
                                                cc.Item().Text(T("Non reevaluee", "Not re-assessed")).FontFamily(MonoMedium).FontSize(9).FontColor(GrisTexte);
                                            else
                                                cc.Item().Text(p.NiveauDangerositeResiduel.Value.ToString("0.00") + " -- " + LibellesRapport.Zone(p.ZoneResiduelle, anglais)).FontFamily(MonoMedium).FontSize(9).FontColor(CouleurZone(p.ZoneResiduelle));
                                            if (p.DangerositeResiduelleEstJugementExpert)
                                                cc.Item().Text(T("Jugement d'expert", "Expert judgement")).FontSize(6).Italic().FontColor(GrisTexte);
                                        });
                                    });
                                    if (p.DangerositeEstJugementExpert && p.JustificationDangerosite is not null)
                                        pc.Item().PaddingTop(2).Text(T("Dangerosite initiale -- jugement d'expert : ", "Initial threat level -- expert judgement: ") + p.JustificationDangerosite).FontSize(7.5f).Italic().FontColor(GrisTexte);
                                    if (p.DangerositeResiduelleEstJugementExpert && p.JustificationDangerositeResiduelle is not null)
                                        pc.Item().PaddingTop(2).Text(T("Dangerosite residuelle -- jugement d'expert : ", "Residual threat level -- expert judgement: ") + p.JustificationDangerositeResiduelle).FontSize(7.5f).Italic().FontColor(GrisTexte);
                                });
                            }
                        }
                    });

                    col.Item().Column(c =>
                    {
                        SectionTitre(c, T("Scenarios strategiques et chemins d'attaque", "Strategic scenarios and attack paths"));
                        c.Item().PaddingTop(4).Text(anglais ? "1 selected RO/TO pair = 1 strategic scenario. Each scenario targets a feared event (Workshop 1) from which it inherits the severity -- identical for the scenario and all its attack paths. 1 scenario => several possible attack paths (direct, or via one or more ecosystem stakeholders generating intermediate events)." : "1 couple SR/OV retenu = 1 scenario stratégique. Chaque scenario cible un evenement redoute (Atelier 1) dont il herite la gravite -- identique pour le scenario et tous ses chemins d'attaque. 1 scenario => plusieurs chemins d'attaque possibles (direct, ou via une ou plusieurs parties prenantes de l'ecosysteme generant des evenements intermediaires).").FontSize(8.5f).Italic().FontColor(GrisTexte);
                        if (data.ScenariosStrategiques.Count == 0)
                        {
                            c.Item().PaddingTop(4).Text(T("Aucun scenario strategique cree a ce stade.", "No strategic scenario created at this stage.")).FontSize(8.5f).Italic().FontColor(GrisTexte);
                        }
                        else
                        {
                            var arbre = data.ScenariosStrategiques
                                .Select(s => new CartographieSvg.ScenarioArbre(
                                    s.LibelleSourceRisque, s.LibelleObjectifVise, s.Description, s.Pertinence,
                                    s.LibelleEvenementRedoute, s.Gravite,
                                    s.CheminsAttaque
                                        .Select(ch => new CartographieSvg.CheminArbre(
                                            ch.Description,
                                            ch.EvenementsIntermediaires.Select(e => e.LibellePartiePrenante).ToList()))
                                        .ToList()))
                                .ToList();
                            c.Item().PaddingTop(8).Svg(CartographieSvg.ArbreCheminsAttaque(arbre));

                            foreach (var s in data.ScenariosStrategiques)
                            {
                                c.Item().PaddingTop(12).Column(sc =>
                                {
                                    sc.Item().Row(row =>
                                    {
                                        row.RelativeItem().Text(s.LibelleSourceRisque + " -- " + s.LibelleObjectifVise).FontFamily(SansSemiBold).FontSize(10.5f).FontColor(BleuFrance);
                                        row.ConstantItem(90).AlignRight().Text(LibellesRapport.Pertinence(s.Pertinence, anglais)).FontFamily(MonoMedium).FontSize(7.5f).FontColor(CouleurPertinence(s.Pertinence));
                                    });
                                    sc.Item().PaddingTop(3).Text(s.Description).FontSize(9);
                                    sc.Item().PaddingTop(3).Text(text =>
                                    {
                                        text.Span(T("Cible : ", "Target: ")).FontSize(8).FontColor(GrisTexte);
                                        text.Span(s.LibelleEvenementRedoute).FontSize(8).FontColor(GrisTexte);
                                        text.Span(T("  --  Gravite ", "  --  Severity ")).FontSize(8).FontColor(GrisTexte);
                                        text.Span(s.Gravite.ToString()).FontFamily(MonoMedium).FontSize(8).FontColor(CouleurGravite(s.Gravite));
                                    });

                                    if (s.CheminsAttaque.Count == 0)
                                    {
                                        sc.Item().PaddingTop(4).Text(T("Aucun chemin d'attaque defini pour ce scenario.", "No attack path defined for this scenario.")).FontSize(8).Italic().FontColor(GrisTexte);
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
                                                    cc.Item().PaddingLeft(6).PaddingTop(1).Text(T("Chemin direct -- aucune partie prenante traversee.", "Direct path -- no stakeholder traversed.")).FontSize(7.5f).Italic().FontColor(GrisTexte);
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
                        row.RelativeItem().Text(T("EBIOS Risk Manager -- Livrable Atelier 3", "EBIOS Risk Manager -- Workshop 3 deliverable")).FontFamily(Mono).FontSize(7).FontColor(GrisTexte);
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

    private static void CelluleNiveauDangerosite(QuestPDF.Fluent.TableDescriptor table, double niveau, string? zone, bool jugementExpert, bool alterne, bool anglais)
    {
        string T(string fr, string en) => anglais ? en : fr;
        var conteneur = alterne ? table.Cell().Background(GrisFond) : table.Cell();
        conteneur.BorderBottom(0.6f).BorderColor(GrisLigne).PaddingVertical(5).PaddingHorizontal(6).Column(cc =>
        {
            cc.Item().Text(niveau.ToString("0.00") + " -- " + LibellesRapport.Zone(zone, anglais)).FontFamily(MonoMedium).FontSize(7.5f).FontColor(CouleurZone(zone));
            if (jugementExpert)
                cc.Item().Text(T("Jugement d'expert", "Expert judgement")).FontSize(6).Italic().FontColor(GrisTexte);
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

    private static readonly (string Designation, string DesignationEn, string Echelle, string Dependance, string DependanceEn, string Penetration, string PenetrationEn, string Maturite, string MaturiteEn, string Confiance, string ConfianceEn)[] LignesEchelleDangerosite = new[]
    {
        ("Tres eleve", "Very high", "4", "Relation indispensable et unique (pas de substitution possible a court terme).",
            "Indispensable and unique relationship (no short-term substitution possible).",
            "Acces administrateur a des equipements d'infrastructure (annuaires, DNS, DHCP, pare-feu, hyperviseurs...) ou acces physique aux salles serveurs.",
            "Administrator access to infrastructure equipment (directories, DNS, DHCP, firewalls, hypervisors...) or physical access to server rooms.",
            "Politique de management du risque integree, dimension proactive.",
            "Integrated risk-management policy, proactive stance.",
            "Intentions parfaitement connues et pleinement compatibles avec celles de l'organisation.",
            "Intentions perfectly known and fully compatible with those of the organisation."),
        ("Eleve", "High", "3", "Relation indispensable mais non exclusive.",
            "Indispensable but not exclusive relationship.",
            "Acces administrateur a des serveurs metier (fichiers, bases de donnees, web, applicatifs).",
            "Administrator access to business servers (files, databases, web, applications).",
            "Politique globale appliquee en mode reactif, avec recherche de centralisation et d'anticipation.",
            "Organisation-wide policy applied reactively, with a drive towards centralisation and anticipation.",
            "Intentions connues et probablement positives.",
            "Known and probably positive intentions."),
        ("Significatif", "Significant", "2", "Relation utile aux fonctions strategiques.",
            "Relationship useful to strategic functions.",
            "Acces administrateur a des terminaux utilisateurs, ou acces physique aux sites de l'organisation.",
            "Administrator access to user endpoints, or physical access to the organisation's sites.",
            "Regles d'hygiene et reglementation prises en compte, sans politique globale, mode reactif.",
            "Hygiene rules and regulations taken into account, without an organisation-wide policy, reactive mode.",
            "Intentions considerees comme neutres.",
            "Intentions considered neutral."),
        ("Tres peu", "Very low", "1", "Relation non necessaire aux fonctions strategiques.",
            "Relationship not necessary to strategic functions.",
            "Pas d'acces, ou acces utilisateur a des terminaux (poste de travail, ordiphone...).",
            "No access, or user access to endpoints (workstation, smartphone...).",
            "Regles d'hygiene appliquees ponctuellement et non formalisees. Capacite de reaction sur incident incertaine.",
            "Hygiene rules applied occasionally and not formalised. Uncertain incident-response capability.",
            "Intentions non evaluables.",
            "Intentions cannot be assessed."),
    };

    private static void ConstruireSectionMethodologieDangerosite(QuestPDF.Fluent.ColumnDescriptor c, bool anglais)
    {
        string T(string fr, string en) => anglais ? en : fr;
        SectionTitre(c, T("Grille officielle d'evaluation de la dangerosite de l'ecosysteme", "Official ecosystem threat-level assessment grid"));
        c.Item().PaddingTop(4).Text(anglais ? "Each stakeholder is assessed against 4 criteria (scale 1 to 4) split into two axes: digital exposure (Dependence, Penetration) and digital reliability (Cyber maturity, Trust)." : "Chaque partie prenante est evaluee selon 4 criteres (echelle 1 a 4) repartis en deux axes : l'exposition numerique (Dependance, Penetration) et la fiabilite numerique (Maturite cyber, Confiance).").FontSize(9);
        c.Item().PaddingTop(6).Table(table =>
        {
            table.ColumnsDefinition(cd =>
            {
                cd.RelativeColumn(1); cd.ConstantColumn(30);
                cd.RelativeColumn(3); cd.RelativeColumn(3); cd.RelativeColumn(3); cd.RelativeColumn(3);
            });
            EnteteCellule(table.Cell(), T("Niveau", "Level"));
            EnteteCellule(table.Cell(), T("Ech.", "Sc."));
            EnteteCellule(table.Cell(), T("Dependance", "Dependence"));
            EnteteCellule(table.Cell(), T("Penetration", "Penetration"));
            EnteteCellule(table.Cell(), T("Maturite cyber", "Cyber maturity"));
            EnteteCellule(table.Cell(), T("Confiance", "Trust"));

            for (var i = 0; i < LignesEchelleDangerosite.Length; i++)
            {
                var l = LignesEchelleDangerosite[i];
                var alt = i % 2 == 1;
                CelluleZebra(table.Cell(), anglais ? l.DesignationEn : l.Designation, alt, police: SansSemiBold);
                table.Cell().Background(alt ? GrisFond : QuestPDF.Helpers.Colors.White).BorderBottom(0.6f).BorderColor(GrisLigne).PaddingVertical(5).AlignCenter().Text(l.Echelle).FontFamily(MonoMedium).FontSize(8.5f);
                CelluleZebra(table.Cell(), anglais ? l.DependanceEn : l.Dependance, alt, taille: 7.5f);
                CelluleZebra(table.Cell(), anglais ? l.PenetrationEn : l.Penetration, alt, taille: 7.5f);
                CelluleZebra(table.Cell(), anglais ? l.MaturiteEn : l.Maturite, alt, taille: 7.5f);
                CelluleZebra(table.Cell(), anglais ? l.ConfianceEn : l.Confiance, alt, taille: 7.5f);
            }
        });

        c.Item().PaddingTop(8).Table(table =>
        {
            table.ColumnsDefinition(cd => { cd.RelativeColumn(2); cd.RelativeColumn(2); cd.RelativeColumn(3); });
            EnteteCellule(table.Cell(), T("Zone (niveau de dangerosite)", "Zone (threat level)"));
            EnteteCellule(table.Cell(), T("Acceptabilite", "Acceptability"));
            EnteteCellule(table.Cell(), T("Recommandation", "Recommendation"));
            CelluleZebra(table.Cell(), T("Danger (>= 4)", "Danger (>= 4)"), false, couleur: RougeAlerte, police: MonoMedium);
            CelluleZebra(table.Cell(), T("Inacceptable", "Unacceptable"), false);
            CelluleZebra(table.Cell(), T("Reduction du risque, ou refus d'etablir l'interaction.", "Risk reduction, or refusal to establish the interaction."), false, taille: 8);
            CelluleZebra(table.Cell(), T("Controle (1 a 4)", "Control (1 to 4)"), true, couleur: OrangeAlerte, police: MonoMedium);
            CelluleZebra(table.Cell(), T("Tolerable sous controle", "Tolerable under control"), true);
            CelluleZebra(table.Cell(), T("Enrolement dans le management du risque : surveillance accrue, audit, plan d'amelioration.", "Enrolment in risk management: heightened monitoring, audit, improvement plan."), true, taille: 8);
            CelluleZebra(table.Cell(), T("Veille (< 1)", "Watch (< 1)"), false, couleur: VertConforme, police: MonoMedium);
            CelluleZebra(table.Cell(), T("Acceptable en l'etat", "Acceptable as is"), false);
            CelluleZebra(table.Cell(), T("Sans objet (dangerosite residuelle).", "Not applicable (residual threat level)."), false, taille: 8);
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
