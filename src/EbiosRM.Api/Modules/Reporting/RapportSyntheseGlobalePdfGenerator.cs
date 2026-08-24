using System.Globalization;
using System.Text;
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
    private static readonly string GrisFond = "#F6F6F6";
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
                        SectionTitre(c, "Socle de securite -- conformite ISO/IEC 27001:2022");
                        var socle = data.ConformiteSocle;
                        var totalControles = socle.NombreConforme + socle.NombreNonConforme + socle.NombreNonApplicable;
                        if (totalControles == 0)
                        {
                            c.Item().PaddingTop(4).Text("Aucun controle renseigne dans le socle de securite.").FontSize(8.5f).Italic().FontColor(GrisTexte);
                        }
                        else
                        {
                            var pctConforme = 100.0 * socle.NombreConforme / totalControles;
                            c.Item().PaddingTop(6).ShowEntire().Row(row =>
                            {
                                row.ConstantItem(100).Height(100).Svg(AnneauMultiSegments(
                                    new List<(double, string)>
                                    {
                                        (socle.NombreConforme, VertConforme),
                                        (socle.NombreNonConforme, RougeAlerte),
                                        (socle.NombreNonApplicable, GrisLigne),
                                    },
                                    pctConforme.ToString("F0", CultureInfo.InvariantCulture) + "%")).FitWidth();
                                row.RelativeItem().PaddingLeft(16).Column(cc =>
                                {
                                    cc.Item().Row(r => Legende(r, VertConforme, socle.NombreConforme + " conforme(s)"));
                                    cc.Item().PaddingTop(3).Row(r => Legende(r, RougeAlerte, socle.NombreNonConforme + " non conforme(s)"));
                                    cc.Item().PaddingTop(3).Row(r => Legende(r, GrisLigne, socle.NombreNonApplicable + " non applicable(s)"));
                                    cc.Item().PaddingTop(3).Text(totalControles + " controle(s) evalue(s) au total.").FontSize(7.5f).FontColor(GrisTexte);
                                });
                            });
                            if (socle.ControlesNonConformes.Count > 0)
                            {
                                c.Item().PaddingTop(10).Text("Controles non conformes a traiter en priorite :").FontFamily(SansSemiBold).FontSize(8).FontColor(Encre);
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
                        SectionTitre(c, "Grille de determination du niveau de risque");
                        c.Item().PaddingTop(4).Text("Croisement Gravite (evenement redoute vise) x Vraisemblance (scenario operationnel), seuils par defaut du projet ajustables. La cartographie ci-dessous indique, pour chaque scenario, le calcul exact qui a produit son niveau.").FontSize(8).Italic().FontColor(GrisTexte);
                        c.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.ConstantColumn(110);
                                cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn();
                            });
                            table.Cell().Background(GrisFond).Padding(5).Text("Gravite \\ Vraisemblance").FontFamily(MonoMedium).FontSize(6.5f).FontColor(GrisTexte);
                            foreach (var v in new[] { "V1", "V2", "V3", "V4" })
                                EnteteCellule(table.Cell(), v);

                            void LigneRisque(string label, string v1, string v2, string v3, string v4)
                            {
                                table.Cell().Background(BleuFranceClair).Padding(4).Text(label).FontFamily(MonoMedium).FontSize(7f).FontColor(BleuFrance);
                                foreach (var v in new[] { v1, v2, v3, v4 })
                                    table.Cell().BorderBottom(0.6f).BorderColor(GrisLigne).PaddingVertical(4).AlignCenter().Text(v).FontFamily(MonoMedium).FontSize(7.5f).FontColor(CouleurNiveau(v));
                            }
                            LigneRisque("1", "Faible", "Faible", "Moyen", "Moyen");
                            LigneRisque("2", "Faible", "Faible", "Moyen", "Eleve");
                            LigneRisque("3", "Faible", "Moyen", "Eleve", "Eleve");
                            LigneRisque("4", "Faible", "Moyen", "Eleve", "Eleve");
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
                            var codes = data.ScenariosDeRisque.Select((s, i) => (Scenario: s, Code: "R" + (i + 1))).ToList();

                            c.Item().PaddingTop(8).ShowEntire().Row(row =>
                            {
                                row.AutoItem().Element(e => GrilleCartographie(e, "Cartographie du risque initial", codes,
                                    x => x.Gravite, x => VraisemblanceVersIndex(x.VraisemblanceInitiale)));
                                row.ConstantItem(36).AlignMiddle().AlignCenter().Text("->").FontFamily(SerifTitreSemiBold).FontSize(20).FontColor(BleuFrance);
                                row.AutoItem().Element(e => GrilleCartographie(e, "Cartographie du risque residuel", codes,
                                    x => x.GraviteResiduelle ?? 0, x => VraisemblanceVersIndex(x.VraisemblanceResiduelle)));
                            });

                            c.Item().PaddingTop(10).Column(cc =>
                            {
                                foreach (var (scenario, code) in codes)
                                {
                                    cc.Item().PaddingTop(1.5f).Text(t =>
                                    {
                                        t.Span(code + " -- ").FontFamily(MonoMedium).FontSize(7.5f).FontColor(BleuFrance);
                                        t.Span(scenario.LibelleCouple + " -- " + scenario.LibelleChemin).FontSize(7.5f).FontColor(GrisTexte);
                                    });
                                }
                            });

                            c.Item().PaddingTop(10).Table(table =>
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
                                    table.Cell().BorderBottom(0.6f).BorderColor(GrisLigne).Padding(5).Text(s.LibelleCouple + " -- " + s.LibelleChemin).FontSize(7.8f);
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
                        SectionTitre(c, "Avancement du plan de traitement du risque");
                        if (data.Mesures.Count == 0)
                        {
                            c.Item().PaddingTop(4).Text("Aucune mesure definie.").FontSize(8.5f).Italic().FontColor(GrisTexte);
                        }
                        else
                        {
                            var termine = data.AvancementPlanParStatut.GetValueOrDefault("Termine", 0);
                            var pctTermine = 100.0 * termine / data.Mesures.Count;
                            c.Item().PaddingTop(6).ShowEntire().Row(row =>
                            {
                                row.ConstantItem(90).Height(90).Svg(AnneauSimple(pctTermine, VertConforme, pctTermine.ToString("F0", CultureInfo.InvariantCulture) + "%")).FitWidth();
                                row.RelativeItem().PaddingLeft(16).AlignMiddle().Row(rr =>
                                {
                                    foreach (var statut in new[] { "ALancer", "EnCours", "Termine" })
                                        Chiffre(rr, data.AvancementPlanParStatut.GetValueOrDefault(statut, 0), LibelleStatut(statut));
                                });
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

    // Grille officielle Gravite x Vraisemblance (identique a ServiceCalculNiveauRisque.cs
    // et a la table "Grille de determination du niveau de risque" ci-dessus).
    // MatriceNiveaux[gravite-1][vraisemblance-1].
    private static readonly string[][] MatriceNiveaux =
    {
        new[] { "Faible", "Faible", "Moyen", "Moyen" },
        new[] { "Faible", "Faible", "Moyen", "Eleve" },
        new[] { "Faible", "Moyen", "Eleve", "Eleve" },
        new[] { "Faible", "Moyen", "Eleve", "Eleve" },
    };

    private static int VraisemblanceVersIndex(string? v) => v switch { "V1" => 1, "V2" => 2, "V3" => 3, "V4" => 4, _ => 0 };

    /// <summary>
    /// Reproduit la cartographie officielle EBIOS RM (cf. Atelier 5, "Gerer les
    /// risques residuels") : une grille Gravite x Vraisemblance a 4x4 cases
    /// colorees, chaque scenario etant place dans la case correspondant a ses
    /// coordonnees reelles -- pas un graphique generique, la representation
    /// exacte que la methode recommande.
    /// </summary>
    private static void GrilleCartographie(IContainer container, string titre, List<(ScenarioDeRisqueData Scenario, string Code)> codes, Func<ScenarioDeRisqueData, int> graviteFn, Func<ScenarioDeRisqueData, int> vraisemblanceIndexFn)
    {
        container.Column(col =>
        {
            col.Item().AlignCenter().Text(titre).FontFamily(SansSemiBold).FontSize(8).FontColor(Encre);
            col.Item().PaddingTop(6).Row(row =>
            {
                row.ConstantItem(14).Column(cc =>
                {
                    cc.Item().Height(16);
                    for (var gravite = 4; gravite >= 1; gravite--)
                        cc.Item().Height(34).AlignMiddle().AlignCenter().Text(gravite.ToString()).FontFamily(MonoMedium).FontSize(7).FontColor(BleuFrance);
                });
                row.AutoItem().Column(cc =>
                {
                    cc.Item().Height(16).AlignCenter().Text("GRAVITE").FontFamily(MonoMedium).FontSize(6).FontColor(BleuFrance).LetterSpacing(0.03f);
                    for (var gravite = 4; gravite >= 1; gravite--)
                    {
                        var g = gravite;
                        cc.Item().Row(ligne =>
                        {
                            for (var vrai = 1; vrai <= 4; vrai++)
                            {
                                var v = vrai;
                                var niveau = MatriceNiveaux[g - 1][v - 1];
                                var codesCellule = codes.Where(x => graviteFn(x.Scenario) == g && vraisemblanceIndexFn(x.Scenario) == v).Select(x => x.Code).ToList();
                                ligne.ConstantItem(34).Height(34).Border(0.7f).BorderColor(Colors.White).Background(CouleurNiveau(niveau))
                                    .AlignMiddle().AlignCenter().Text(string.Join(" ", codesCellule)).FontFamily(SansSemiBold).FontSize(6.5f).FontColor(Colors.White);
                            }
                        });
                    }
                    cc.Item().Row(ligne =>
                    {
                        for (var vrai = 1; vrai <= 4; vrai++)
                            ligne.ConstantItem(34).AlignCenter().PaddingTop(2).Text(vrai.ToString()).FontFamily(MonoMedium).FontSize(7).FontColor(BleuFrance);
                    });
                    cc.Item().AlignCenter().Text("VRAISEMBLANCE").FontFamily(MonoMedium).FontSize(6).FontColor(BleuFrance).LetterSpacing(0.03f);
                });
            });
        });
    }

    private static string LibelleClasse(string? classe) => classe switch
    {
        "AcceptableEnLEtat" => "Acceptable en l'etat",
        "TolerableSousControle" => "Tolerable sous controle",
        "Inacceptable" => "Inacceptable",
        _ => "--",
    };

    /// <summary>
    /// Anneau de progression a un seul segment (ex: pourcentage de conformite
    /// globale, pourcentage de mesures terminees). Rendu en SVG -- QuestPDF
    /// n'a pas de composant "gauge" natif, mais supporte l'embarquement SVG.
    /// </summary>
    private static string AnneauSimple(double pourcentage, string couleur, string labelCentre)
    {
        return AnneauMultiSegments(new List<(double, string)> { (pourcentage, couleur), (100 - pourcentage, "#EDEDED") }, labelCentre);
    }

    /// <summary>
    /// Anneau de repartition a plusieurs segments (ex: Conforme/Non conforme/
    /// Non applicable, ou Faible/Moyen/Eleve). Chaque segment est un arc de
    /// cercle SVG construit via stroke-dasharray/stroke-dashoffset -- technique
    /// standard pour un donut chart sans dependance a une lib de graphiques.
    /// </summary>
    private static string AnneauMultiSegments(List<(double Part, string Couleur)> segments, string labelCentre)
    {
        const double rayon = 50;
        const double centre = 60;
        const double epaisseur = 15;
        var circonference = 2 * Math.PI * rayon;
        var total = segments.Sum(s => s.Part);

        var sb = new StringBuilder();
        sb.Append("<svg viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\">");
        sb.Append(FormattableString.Invariant($"<circle cx=\"{centre}\" cy=\"{centre}\" r=\"{rayon}\" fill=\"none\" stroke=\"#EDEDED\" stroke-width=\"{epaisseur}\" />"));

        if (total > 0)
        {
            var cumule = 0.0;
            foreach (var (part, couleur) in segments)
            {
                if (part <= 0) continue;
                var fraction = part / total;
                var longueurArc = fraction * circonference;
                var vide = circonference - longueurArc;
                var decalage = -(cumule / total) * circonference;
                sb.Append(FormattableString.Invariant($"<circle cx=\"{centre}\" cy=\"{centre}\" r=\"{rayon}\" fill=\"none\" stroke=\"{couleur}\" stroke-width=\"{epaisseur}\" stroke-dasharray=\"{longueurArc.ToString("F2", CultureInfo.InvariantCulture)} {vide.ToString("F2", CultureInfo.InvariantCulture)}\" stroke-dashoffset=\"{decalage.ToString("F2", CultureInfo.InvariantCulture)}\" transform=\"rotate(-90 {centre} {centre})\" />"));
                cumule += part;
            }
        }

        sb.Append(FormattableString.Invariant($"<text x=\"{centre}\" y=\"{centre + 7}\" text-anchor=\"middle\" font-size=\"26\" font-family=\"IBM Plex Sans SemiBold, IBM Plex Sans, sans-serif\" fill=\"#161616\">{System.Security.SecurityElement.Escape(labelCentre)}</text>"));
        sb.Append("</svg>");
        return sb.ToString();
    }

    private static void Legende(QuestPDF.Fluent.RowDescriptor row, string couleur, string libelle)
    {
        row.AutoItem().Column(c =>
        {
            c.Item().Row(r =>
            {
                r.ConstantItem(8).Height(8).Background(couleur);
                r.ConstantItem(5);
                r.AutoItem().Text(libelle).FontSize(7.5f).FontColor(GrisTexte);
            });
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
}
