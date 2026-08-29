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

    public static string LibelleClasse(string? classe, bool anglais = false) => LibellesRapport.ClasseAcceptation(classe, anglais);

    public static string LibelleStatutMesure(string statut, bool anglais = false) => LibellesRapport.StatutMesure(statut, anglais);

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
    public static void PastilleStatutMesure(IContainer cell, string statut, bool anglais = false)
    {
        cell.Padding(4).AlignCenter().Background(CouleurStatutMesure(statut)).Padding(3)
            .Text(LibelleStatutMesure(statut, anglais)).FontFamily(SansSemiBold).FontSize(7).FontColor(Colors.White).AlignCenter();
    }

    /// <summary>Bandeau plein largeur identifiant un axe du plan de traitement (Gouvernance/Protection/Defense/Resilience), reproduit la banniere bleue du PACS officiel.</summary>
    public static void BandeAxe(TableDescriptor table, int nbColonnes, string axe, bool anglais = false)
    {
        table.Cell().ColumnSpan((uint)nbColonnes).Background(BleuFrance).Padding(4)
            .Text(LibellesRapport.Axe(axe, anglais).ToUpperInvariant()).FontFamily(SansSemiBold).FontSize(8).FontColor(Colors.White).LetterSpacing(0.03f);
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

    /// <summary>
    /// Vrai camembert (secteurs pleins depuis le centre), a la difference de
    /// l'anneau de <see cref="AnneauMultiSegments"/> -- plus lisible en un
    /// coup d'oeil pour une Direction que des arcs fins. Un segment qui
    /// totalise a lui seul 100% est trace en cercle plein directement : un
    /// arc SVG ne peut pas dessiner un tour complet (360deg) en un seul
    /// trace M-L-A-Z, le point de depart et d'arrivee seraient confondus.
    /// </summary>
    public static string Camembert(List<(double Part, string Couleur)> segments)
    {
        const double rayon = 55, centre = 60;
        var total = segments.Sum(s => s.Part);
        var sb = new StringBuilder();
        sb.Append("<svg viewBox=\"0 0 120 120\" xmlns=\"http://www.w3.org/2000/svg\">");

        if (total <= 0)
        {
            sb.Append(FormattableString.Invariant($"<circle cx=\"{centre}\" cy=\"{centre}\" r=\"{rayon}\" fill=\"#EDEDED\" />"));
        }
        else
        {
            var angle = -Math.PI / 2;
            foreach (var (part, couleur) in segments)
            {
                if (part <= 0) continue;
                var fraction = part / total;
                if (fraction >= 0.9999)
                {
                    sb.Append(FormattableString.Invariant($"<circle cx=\"{centre}\" cy=\"{centre}\" r=\"{rayon}\" fill=\"{couleur}\" />"));
                    continue;
                }
                var angleFin = angle + fraction * 2 * Math.PI;
                var x1 = centre + rayon * Math.Cos(angle);
                var y1 = centre + rayon * Math.Sin(angle);
                var x2 = centre + rayon * Math.Cos(angleFin);
                var y2 = centre + rayon * Math.Sin(angleFin);
                var grandArc = angleFin - angle > Math.PI ? 1 : 0;
                sb.Append(FormattableString.Invariant($"<path d=\"M{centre},{centre} L{x1:F2},{y1:F2} A{rayon},{rayon} 0 {grandArc} 1 {x2:F2},{y2:F2} Z\" fill=\"{couleur}\" stroke=\"white\" stroke-width=\"1.2\" />"));
                angle = angleFin;
            }
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    /// <summary>
    /// Graphique en barres verticales (ex: taux de conformite par theme).
    /// echelleMax fixe la hauteur representant 100% de la barre -- passer 100
    /// pour des pourcentages, ou le plus grand effectif pour des comptages.
    ///
    /// Les barres occupent toute la largeur (reparties uniformement). Une piste
    /// de fond tres discrete + une ligne de base garantissent qu'une valeur a
    /// 0 reste visible sans "casser" le graphique. L'etiquette de valeur est
    /// posee dans la barre (texte blanc) quand elle est assez haute, au-dessus
    /// sinon -- pas de collision avec les graphiques voisins.
    /// </summary>
    public static string GraphiqueBarres(List<(string Label, double Valeur, string Couleur)> barres, double echelleMax, string suffixeValeur)
    {
        var n = Math.Max(barres.Count, 1);
        const double margeHaut = 14, hauteurZone = 110, margeBas = 26;
        const double hauteurTotale = margeHaut + hauteurZone + margeBas;
        // Largeur adaptee au nombre de barres : ni ecrasees, ni perdues dans du vide.
        var largeurTotale = Math.Clamp(n * 88.0, 200, 460);
        var pas = largeurTotale / n;
        var largeurBarre = Math.Min(pas * 0.62, 56);
        var baseY = margeHaut + hauteurZone;

        var sb = new StringBuilder();
        sb.Append(FormattableString.Invariant($"<svg viewBox=\"0 0 {largeurTotale:F0} {hauteurTotale:F0}\" xmlns=\"http://www.w3.org/2000/svg\">"));
        // Ligne de base (les barres ne sont pas soulignees par une piste de fond
        // pleine hauteur : trop presente visuellement, elle ecrasait les barres).
        sb.Append(FormattableString.Invariant($"<line x1=\"0\" y1=\"{baseY:F1}\" x2=\"{largeurTotale:F0}\" y2=\"{baseY:F1}\" stroke=\"{GrisLigne}\" stroke-width=\"1\" />"));

        for (var i = 0; i < barres.Count; i++)
        {
            var (label, valeur, couleur) = barres[i];
            var cx = (i + 0.5) * pas;
            var x = cx - largeurBarre / 2;
            var fraction = echelleMax <= 0 ? 0 : Math.Clamp(valeur / echelleMax, 0, 1);
            var hauteurBarre = fraction * hauteurZone;
            var y = baseY - hauteurBarre;

            // Contour discret de la hauteur "100 %" pour situer la barre, sans aplat.
            sb.Append(FormattableString.Invariant($"<rect x=\"{x:F1}\" y=\"{margeHaut:F1}\" width=\"{largeurBarre:F1}\" height=\"{hauteurZone:F1}\" fill=\"none\" stroke=\"#EDEDED\" stroke-width=\"1\" rx=\"2\" />"));
            if (hauteurBarre > 1.5)
                sb.Append(FormattableString.Invariant($"<rect x=\"{x:F1}\" y=\"{y:F1}\" width=\"{largeurBarre:F1}\" height=\"{hauteurBarre:F1}\" fill=\"{couleur}\" rx=\"2\" />"));

            var valeurTexte = valeur.ToString("F0", CultureInfo.InvariantCulture) + suffixeValeur;
            if (hauteurBarre > 22)
                sb.Append(FormattableString.Invariant($"<text x=\"{cx:F1}\" y=\"{y + 14:F1}\" text-anchor=\"middle\" font-size=\"10\" font-family=\"IBM Plex Sans SemiBold, IBM Plex Sans, sans-serif\" fill=\"#FFFFFF\">{valeurTexte}</text>"));
            else
                sb.Append(FormattableString.Invariant($"<text x=\"{cx:F1}\" y=\"{y - 5:F1}\" text-anchor=\"middle\" font-size=\"10\" font-family=\"IBM Plex Sans SemiBold, IBM Plex Sans, sans-serif\" fill=\"{Encre}\">{valeurTexte}</text>"));

            sb.Append(FormattableString.Invariant($"<text x=\"{cx:F1}\" y=\"{baseY + 15:F1}\" text-anchor=\"middle\" font-size=\"8\" font-family=\"IBM Plex Sans, sans-serif\" fill=\"{GrisTexte}\">{System.Security.SecurityElement.Escape(label)}</text>"));
        }
        sb.Append("</svg>");
        return sb.ToString();
    }

    /// <summary>
    /// Graphique radar/toile d'araignee (ex: cartographie de conformite par
    /// theme). Chaque axe est une valeur 0-100%, les axes sont repartis
    /// uniformement autour du cercle en commencant en haut.
    /// </summary>
    public static string GraphiqueRadar(List<(string Label, double ValeurPct)> axes, string couleur)
    {
        const double cx = 110, cy = 105, rayonMax = 68;
        var n = axes.Count;
        var sb = new StringBuilder();
        sb.Append("<svg viewBox=\"0 0 220 220\" xmlns=\"http://www.w3.org/2000/svg\">");

        double AngleDe(int i) => -Math.PI / 2 + 2 * Math.PI * i / n;

        foreach (var frac in new[] { 0.25, 0.5, 0.75, 1.0 })
        {
            var points = Enumerable.Range(0, n).Select(i =>
            {
                var angle = AngleDe(i);
                var x = cx + rayonMax * frac * Math.Cos(angle);
                var y = cy + rayonMax * frac * Math.Sin(angle);
                return FormattableString.Invariant($"{x:F1},{y:F1}");
            });
            sb.Append(FormattableString.Invariant($"<polygon points=\"{string.Join(" ", points)}\" fill=\"none\" stroke=\"{GrisLigne}\" stroke-width=\"0.7\" />"));
        }

        for (var i = 0; i < n; i++)
        {
            var angle = AngleDe(i);
            var x = cx + rayonMax * Math.Cos(angle);
            var y = cy + rayonMax * Math.Sin(angle);
            sb.Append(FormattableString.Invariant($"<line x1=\"{cx}\" y1=\"{cy}\" x2=\"{x:F1}\" y2=\"{y:F1}\" stroke=\"{GrisLigne}\" stroke-width=\"0.7\" />"));
        }

        var pointsDonnees = new List<(double X, double Y)>();
        for (var i = 0; i < n; i++)
        {
            var angle = AngleDe(i);
            var fraction = Math.Clamp(axes[i].ValeurPct / 100.0, 0, 1);
            pointsDonnees.Add((cx + rayonMax * fraction * Math.Cos(angle), cy + rayonMax * fraction * Math.Sin(angle)));
        }
        var polygonDonnees = string.Join(" ", pointsDonnees.Select(p => FormattableString.Invariant($"{p.X:F1},{p.Y:F1}")));
        sb.Append(FormattableString.Invariant($"<polygon points=\"{polygonDonnees}\" fill=\"{couleur}\" fill-opacity=\"0.3\" stroke=\"{couleur}\" stroke-width=\"1.8\" />"));
        foreach (var p in pointsDonnees)
            sb.Append(FormattableString.Invariant($"<circle cx=\"{p.X:F1}\" cy=\"{p.Y:F1}\" r=\"2.6\" fill=\"{couleur}\" />"));

        for (var i = 0; i < n; i++)
        {
            var angle = AngleDe(i);
            var cosA = Math.Cos(angle);
            var lx = cx + (rayonMax + 18) * cosA;
            var ly = cy + (rayonMax + 18) * Math.Sin(angle);
            var ancre = cosA > 0.3 ? "start" : cosA < -0.3 ? "end" : "middle";
            sb.Append(FormattableString.Invariant($"<text x=\"{lx:F1}\" y=\"{ly:F1}\" text-anchor=\"{ancre}\" font-size=\"9\" font-family=\"IBM Plex Sans, sans-serif\" fill=\"{GrisTexte}\">{System.Security.SecurityElement.Escape(axes[i].Label)} ({axes[i].ValeurPct.ToString("F0", CultureInfo.InvariantCulture)}%)</text>"));
        }

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
    public static void GrilleCartographie(IContainer container, string titre, List<(ScenarioDeRisqueData Scenario, string Code)> codes, Func<ScenarioDeRisqueData, int> graviteFn, Func<ScenarioDeRisqueData, int> vraisemblanceIndexFn, float tailleCase = 34)
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
                        cc.Item().Height(tailleCase).AlignMiddle().AlignCenter().Text(gravite.ToString()).FontFamily(MonoMedium).FontSize(7).FontColor(BleuFrance);
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
                                ligne.ConstantItem(tailleCase).Height(tailleCase).Border(0.7f).BorderColor(Colors.White).Background(CouleurNiveau(niveau))
                                    .AlignMiddle().AlignCenter().Text(string.Join(" ", codesCellule)).FontFamily(SansSemiBold).FontSize(6.5f).FontColor(Colors.White);
                            }
                        });
                    }
                    cc.Item().Row(ligne =>
                    {
                        for (var vrai = 1; vrai <= 4; vrai++)
                            ligne.ConstantItem(tailleCase).AlignCenter().PaddingTop(2).Text(vrai.ToString()).FontFamily(MonoMedium).FontSize(7).FontColor(BleuFrance);
                    });
                    cc.Item().AlignCenter().Text("VRAISEMBLANCE").FontFamily(MonoMedium).FontSize(6).FontColor(BleuFrance).LetterSpacing(0.03f);
                });
            });
        });
    }

    /// <summary>Les 2 grilles (initial/residuel) reliees par une fleche, plus la legende des codes -- bloc complet reutilise par le rapport Atelier 5 et la synthese globale. tailleCase permet a la synthese d'afficher une grille plus imposante sans affecter le rapport Atelier 5.</summary>
    public static void CartographieCompleteAvecLegende(ColumnDescriptor c, List<ScenarioDeRisqueData> scenarios, bool anglais = false, float tailleCase = 34)
    {
        var codes = scenarios.Select((s, i) => (Scenario: s, Code: "R" + (i + 1))).ToList();
        var titreInitial = anglais ? "Initial risk map" : "Cartographie du risque initial";
        var titreResiduel = anglais ? "Residual risk map" : "Cartographie du risque residuel";

        c.Item().PaddingTop(8).ShowEntire().Row(row =>
        {
            row.AutoItem().Element(e => GrilleCartographie(e, titreInitial, codes,
                x => x.Gravite, x => VraisemblanceVersIndex(x.VraisemblanceInitiale), tailleCase));
            row.ConstantItem(36).AlignMiddle().AlignCenter().Text("->").FontFamily(SerifTitreSemiBold).FontSize(20).FontColor(BleuFrance);
            row.AutoItem().Element(e => GrilleCartographie(e, titreResiduel, codes,
                x => x.GraviteResiduelle ?? 0, x => VraisemblanceVersIndex(x.VraisemblanceResiduelle), tailleCase));
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
