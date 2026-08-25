using System.Globalization;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EbiosRM.Api.Modules.Reporting;

/// <summary>
/// Palette, polices et elements graphiques partages entre tous les rapports
/// PDF (Atelier 1 a 5 + Synthese globale) -- source unique pour eviter que
/// deux generateurs divergent sur le meme element (deja arrive une fois avec
/// le libelle d'un scenario de risque construit dans un ordre different
/// entre ServiceAssemblageScenariosDeRisque et un rapport).
/// </summary>
public static class RapportPdfStyle
{
    public const string BleuFrance = "#000091";
    public const string BleuFranceClair = "#E3E3FD";
    public const string Encre = "#161616";
    public const string GrisTexte = "#3A3A3A";
    public const string GrisLigne = "#DDDDDD";
    public const string GrisFond = "#F6F6F6";
    public const string RougeAlerte = "#B34000";
    public const string OrangeAlerte = "#BA7517";
    public const string VertConforme = "#18753C";

    public const string SerifTitreSemiBold = "Fraunces 72pt SemiBold";
    public const string Sans = "IBM Plex Sans";
    public const string SansSemiBold = "IBM Plex Sans SemiBold";
    public const string Mono = "IBM Plex Mono";
    public const string MonoMedium = "IBM Plex Mono Medium";

    public static string CouleurNiveau(string? niveau) => niveau switch
    {
        "Eleve" => RougeAlerte,
        "Moyen" => OrangeAlerte,
        "Faible" => VertConforme,
        _ => GrisTexte,
    };

    public static string LibelleClasse(string? classe) => classe switch
    {
        "AcceptableEnLEtat" => "Acceptable en l'etat",
        "TolerableSousControle" => "Tolerable sous controle",
        "Inacceptable" => "Inacceptable",
        _ => "--",
    };

    public static string LibelleStatutMesure(string statut) => statut switch
    {
        "ALancer" => "A lancer",
        "EnCours" => "En cours",
        "Termine" => "Termine",
        _ => statut,
    };

    public static string CouleurStatutMesure(string statut) => statut switch
    {
        "Termine" => VertConforme,
        "EnCours" => OrangeAlerte,
        _ => RougeAlerte, // ALancer
    };

    public static void SectionTitre(ColumnDescriptor col, string texte)
    {
        col.Item().Row(row =>
        {
            row.ConstantItem(3).Height(16).Background(BleuFrance);
            row.RelativeItem().PaddingLeft(8).Text(texte).FontFamily(SerifTitreSemiBold).FontSize(13).FontColor(Encre);
        });
    }

    public static void EnteteCellule(IContainer cell, string texte)
    {
        cell.Background(BleuFranceClair).Padding(5).Text(texte).FontFamily(MonoMedium).FontSize(7.5f).FontColor(BleuFrance).LetterSpacing(0.02f);
    }

    /// <summary>Pastille de statut coloree (reproduit la colonne STATUT du PACS officiel : rouge/orange/vert).</summary>
    public static void PastilleStatutMesure(IContainer cell, string statut)
    {
        cell.Padding(4).AlignCenter().Background(CouleurStatutMesure(statut)).Padding(3)
            .Text(LibelleStatutMesure(statut)).FontFamily(SansSemiBold).FontSize(7).FontColor(Colors.White).AlignCenter();
    }

    /// <summary>Bandeau plein largeur identifiant un axe du plan de traitement (Gouvernance/Protection/Defense/Resilience), reproduit la banniere bleue du PACS officiel.</summary>
    public static void BandeAxe(TableDescriptor table, int nbColonnes, string axe)
    {
        table.Cell().ColumnSpan((uint)nbColonnes).Background(BleuFrance).Padding(4)
            .Text(axe.ToUpperInvariant()).FontFamily(SansSemiBold).FontSize(8).FontColor(Colors.White).LetterSpacing(0.03f);
    }

    public static void Chiffre(RowDescriptor row, int valeur, string libelle)
    {
        row.RelativeItem().Column(c =>
        {
            c.Item().Text(valeur.ToString()).FontFamily(SerifTitreSemiBold).FontSize(22).FontColor(BleuFrance);
            c.Item().Text(libelle).FontSize(7.5f).FontColor(GrisTexte);
        });
    }

    public static void Legende(RowDescriptor row, string couleur, string libelle)
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

    /// <summary>
    /// Anneau de progression a un seul segment (ex: pourcentage de conformite
    /// globale, pourcentage de mesures terminees). Rendu en SVG -- QuestPDF
    /// n'a pas de composant "gauge" natif, mais supporte l'embarquement SVG.
    /// </summary>
    public static string AnneauSimple(double pourcentage, string couleur, string labelCentre)
    {
        return AnneauMultiSegments(new List<(double, string)> { (pourcentage, couleur), (100 - pourcentage, "#EDEDED") }, labelCentre);
    }

    /// <summary>
    /// Anneau de repartition a plusieurs segments (ex: Conforme/Non conforme/
    /// Non applicable, ou Faible/Moyen/Eleve). Chaque segment est un arc de
    /// cercle SVG construit via stroke-dasharray/stroke-dashoffset -- technique
    /// standard pour un donut chart sans dependance a une lib de graphiques.
    /// </summary>
    public static string AnneauMultiSegments(List<(double Part, string Couleur)> segments, string labelCentre)
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

    // Grille officielle Gravite x Vraisemblance (identique a ServiceCalculNiveauRisque.cs).
    // MatriceNiveaux[gravite-1][vraisemblance-1].
    public static readonly string[][] MatriceNiveaux =
    {
        new[] { "Faible", "Faible", "Moyen", "Moyen" },
        new[] { "Faible", "Faible", "Moyen", "Eleve" },
        new[] { "Faible", "Moyen", "Eleve", "Eleve" },
        new[] { "Faible", "Moyen", "Eleve", "Eleve" },
    };

    public static int VraisemblanceVersIndex(string? v) => v switch { "V1" => 1, "V2" => 2, "V3" => 3, "V4" => 4, _ => 0 };

    /// <summary>
    /// Reproduit la cartographie officielle EBIOS RM (Atelier 5, "Decider de la
    /// strategie de traitement du risque") : une grille Gravite x Vraisemblance
    /// a 4x4 cases colorees, chaque scenario etant place dans la case
    /// correspondant a ses coordonnees reelles -- pas un graphique generique,
    /// la representation exacte que la methode recommande.
    /// </summary>
    public static void GrilleCartographie(IContainer container, string titre, List<(ScenarioDeRisqueData Scenario, string Code)> codes, Func<ScenarioDeRisqueData, int> graviteFn, Func<ScenarioDeRisqueData, int> vraisemblanceIndexFn)
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

    /// <summary>Les 2 grilles (initial/residuel) reliees par une fleche, plus la legende des codes -- bloc complet reutilise par le rapport Atelier 5 et la synthese globale.</summary>
    public static void CartographieCompleteAvecLegende(ColumnDescriptor c, List<ScenarioDeRisqueData> scenarios)
    {
        var codes = scenarios.Select((s, i) => (Scenario: s, Code: "R" + (i + 1))).ToList();

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
    }
}
