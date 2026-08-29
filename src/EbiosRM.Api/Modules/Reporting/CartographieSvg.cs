using System.Globalization;
using System.Security;
using System.Text;

namespace EbiosRM.Api.Modules.Reporting;

/// <summary>
/// Génère les deux schémas de l'Atelier 3 sous forme de chaînes SVG, utilisées
/// à la fois dans le rapport PDF (via <c>.Svg()</c> de QuestPDF) et dans l'app
/// (endpoints <c>/etudes/{id}/cartographie/*.svg</c>) -- une seule géométrie,
/// pas de divergence entre les deux rendus.
///
///   - <see cref="RadarEcosysteme"/> : cartographie de la dangerosité de
///     l'écosystème en cercles concentriques (méthode ANSSI). L'objet de
///     l'étude au centre ; plus une partie prenante est dangereuse, plus elle
///     est proche du centre. Trois zones : contrôle, veille, danger.
///   - <see cref="ArbreCheminsAttaque"/> : les scénarios stratégiques et leurs
///     chemins d'attaque en arbre (source de risque -> objectif visé ->
///     chemin -> parties prenantes traversées -> objet de l'étude).
/// </summary>
public static class CartographieSvg
{
    private const string BleuFrance = "#000091";
    private const string Encre = "#161616";
    private const string GrisTexte = "#3A3A3A";
    private const string GrisLigne = "#DDDDDD";
    // Convention identique au reste de l'app et aux rapports PDF :
    // Danger = rouge, Controle = orange, Veille = vert.
    private const string Danger = "#B34000";
    private const string Controle = "#BA7517";
    private const string Veille = "#18753C";
    private const string Police = "IBM Plex Sans, Segoe UI, sans-serif";

    private static string I(FormattableString s) => FormattableString.Invariant(s);
    private static string E(string? s) => SecurityElement.Escape(s ?? string.Empty);
    private static string N(double d) => d.ToString("0.##", CultureInfo.InvariantCulture);

    private static string Tronquer(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";

    private static string CouleurZone(string? zone) => zone switch
    {
        "Danger" => Danger,
        "Controle" => Controle,
        "Veille" => Veille,
        _ => GrisLigne,
    };

    private static string LibelleZone(string? zone) => zone switch
    {
        "Danger" => "Danger",
        "Controle" => "Contrôle",
        "Veille" => "Veille",
        _ => "Non évaluée",
    };

    private static string LibellePertinence(string? p) => p switch
    {
        "TresPertinent" => "très pertinent",
        "PlutotPertinent" => "plutôt pertinent",
        "MoyennementPertinent" => "moyennement pertinent",
        "PeuPertinent" => "peu pertinent",
        _ => p ?? "",
    };

    public readonly record struct PartieRadar(string Nom, string Categorie, double? Niveau, string? Zone);

    /// <param name="titre">Titre affiché en haut du schéma (ex. « initiale » / « résiduelle »).</param>
    public static string RadarEcosysteme(IReadOnlyList<PartieRadar> parties, string titre)
    {
        const double largeur = 900, hauteur = 640;
        const double cx = 450, cy = 320;
        const double rVeille = 258, rControle = 184, rDanger = 110, rObjet = 46;

        // Rayon en fonction du niveau : plus c'est dangereux, plus c'est central.
        static double RayonPour(string? zone, double? niveau)
        {
            var d = niveau ?? 0;
            return zone switch
            {
                "Danger" => Lerp(rDanger - 8, rObjet + 12, Clamp((d - 4) / 8.0)),
                "Controle" => Lerp(rControle - 8, rDanger + 4, Clamp((d - 1) / 3.0)),
                "Veille" => Lerp(rVeille - 8, rControle + 4, Clamp(d / 1.0)),
                _ => rVeille + 22,
            };
        }

        var sb = new StringBuilder();
        sb.Append(I($"<svg viewBox=\"0 0 {largeur} {hauteur}\" xmlns=\"http://www.w3.org/2000/svg\" font-family=\"{Police}\">"));
        sb.Append(I($"<text x=\"12\" y=\"20\" font-size=\"12\" font-weight=\"600\" fill=\"{Encre}\">Cartographie de la dangerosité de l'écosystème &#8212; {E(titre)}</text>"));

        // Anneaux (du plus large au plus petit pour l'empilement des remplissages).
        sb.Append(I($"<circle cx=\"{cx}\" cy=\"{cy}\" r=\"{rVeille}\" fill=\"#F0F7F2\" stroke=\"{GrisLigne}\" />"));
        sb.Append(I($"<circle cx=\"{cx}\" cy=\"{cy}\" r=\"{rControle}\" fill=\"#FBF7EF\" stroke=\"{GrisLigne}\" />"));
        sb.Append(I($"<circle cx=\"{cx}\" cy=\"{cy}\" r=\"{rDanger}\" fill=\"#FBF0EA\" stroke=\"{GrisLigne}\" />"));

        // Seuil de criticité : frontière veille / contrôle.
        sb.Append(I($"<circle cx=\"{cx}\" cy=\"{cy}\" r=\"{rControle}\" fill=\"none\" stroke=\"{Danger}\" stroke-width=\"1.2\" stroke-dasharray=\"5 4\" />"));

        // Étiquettes de zones, calées à gauche du diagramme le long de chaque anneau.
        sb.Append(I($"<text x=\"{cx - rVeille + 8}\" y=\"{cy - 4}\" font-size=\"10\" fill=\"{Veille}\">Veille</text>"));
        sb.Append(I($"<text x=\"{cx - rControle + 8}\" y=\"{cy - 4}\" font-size=\"10\" fill=\"{Controle}\">Contrôle</text>"));
        sb.Append(I($"<text x=\"{cx - rDanger + 8}\" y=\"{cy - 4}\" font-size=\"10\" fill=\"{Danger}\">Danger</text>"));

        // Objet de l'étude au centre.
        sb.Append(I($"<circle cx=\"{cx}\" cy=\"{cy}\" r=\"{rObjet}\" fill=\"{BleuFrance}\" />"));
        sb.Append(I($"<text x=\"{cx}\" y=\"{cy - 2}\" text-anchor=\"middle\" font-size=\"10\" fill=\"white\">Objet de</text>"));
        sb.Append(I($"<text x=\"{cx}\" y=\"{cy + 12}\" text-anchor=\"middle\" font-size=\"10\" fill=\"white\">l'étude</text>"));

        // Parties prenantes : réparties en angle, triées par zone (danger d'abord).
        var ordre = new Dictionary<string, int> { ["Danger"] = 0, ["Controle"] = 1, ["Veille"] = 2 };
        var triees = parties
            .OrderBy(p => ordre.GetValueOrDefault(p.Zone ?? "", 3))
            .ThenBy(p => p.Nom, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var n = triees.Count;

        for (var i = 0; i < n; i++)
        {
            var p = triees[i];
            // Décalage d'un demi-secteur : aucune partie pile sur l'axe vertical
            // (là où sont les étiquettes de zones n'étaient — ici décalé quand même).
            var angle = -Math.PI / 2 + (n == 1 ? 0.6 : (i + 0.5) * 2 * Math.PI / n);
            var r = RayonPour(p.Zone, p.Niveau);
            var x = cx + r * Math.Cos(angle);
            var y = cy + r * Math.Sin(angle);
            var couleur = CouleurZone(p.Zone);

            var aGauche = Math.Cos(angle) < -0.05;
            var lx = x + (aGauche ? -11 : 11);
            var ancre = aGauche ? "end" : "start";
            var infos = p.Niveau is null
                ? $"{p.Nom} — {p.Categorie} — non évaluée"
                : $"{p.Nom} — {p.Categorie} — niveau {N(p.Niveau.Value)} ({LibelleZone(p.Zone)})";

            sb.Append(I($"<g><title>{E(infos)}</title>"));
            sb.Append(I($"<circle cx=\"{N(x)}\" cy=\"{N(y)}\" r=\"7\" fill=\"{couleur}\" stroke=\"white\" stroke-width=\"1.5\" />"));
            sb.Append(I($"<text x=\"{N(lx)}\" y=\"{N(y + 3)}\" text-anchor=\"{ancre}\" font-size=\"10\" fill=\"{Encre}\">{E(Tronquer(p.Nom, 22))}</text>"));
            sb.Append("</g>");
        }

        // Légende.
        var ly = hauteur - 16;
        sb.Append(I($"<circle cx=\"18\" cy=\"{ly - 4}\" r=\"5\" fill=\"{Danger}\" /><text x=\"30\" y=\"{ly}\" font-size=\"10\" fill=\"{GrisTexte}\">Danger (niveau &#8805; 4)</text>"));
        sb.Append(I($"<circle cx=\"200\" cy=\"{ly - 4}\" r=\"5\" fill=\"{Controle}\" /><text x=\"212\" y=\"{ly}\" font-size=\"10\" fill=\"{GrisTexte}\">Contrôle (1 &#8804; niveau &lt; 4)</text>"));
        sb.Append(I($"<circle cx=\"430\" cy=\"{ly - 4}\" r=\"5\" fill=\"{Veille}\" /><text x=\"442\" y=\"{ly}\" font-size=\"10\" fill=\"{GrisTexte}\">Veille (niveau &lt; 1)</text>"));
        sb.Append(I($"<text x=\"600\" y=\"{ly}\" font-size=\"10\" fill=\"{Danger}\">- - - seuil de criticité</text>"));

        sb.Append("</svg>");
        return sb.ToString();
    }

    public readonly record struct CheminArbre(string Description, IReadOnlyList<string> PartiesTraversees);
    public readonly record struct ScenarioArbre(
        string SourceRisque, string ObjectifVise, string Description, string Pertinence,
        string EvenementRedoute, int Gravite, IReadOnlyList<CheminArbre> Chemins);

    public static string ArbreCheminsAttaque(IReadOnlyList<ScenarioArbre> scenarios)
    {
        const double largeurCol = 168, gap = 22, hauteurNoeud = 52, gapVertical = 20, margeHaut = 34;
        var maxParties = scenarios
            .SelectMany(s => s.Chemins.Select(c => c.PartiesTraversees.Count))
            .DefaultIfEmpty(0)
            .Max();
        // Colonnes : source de risque | objectif visé + scénario | parties traversées... | objet
        var nbColonnes = 3 + maxParties;
        var largeur = nbColonnes * largeurCol + (nbColonnes - 1) * gap + 24;

        var sb = new StringBuilder();
        var blocs = new StringBuilder();
        double y = margeHaut;

        foreach (var (s, idx) in scenarios.Select((s, i) => (s, i)))
        {
            var lignes = Math.Max(1, s.Chemins.Count);
            var hauteurBloc = lignes * hauteurNoeud + (lignes - 1) * gapVertical;
            var yCentre = y + hauteurBloc / 2;

            blocs.Append(I($"<text x=\"12\" y=\"{N(y - 10)}\" font-size=\"11\" font-weight=\"600\" fill=\"{Encre}\">Scénario {idx + 1} &#8212; {E(LibellePertinence(s.Pertinence))}</text>"));

            // Colonne 0 : source de risque.
            Noeud(blocs, 12, yCentre - hauteurNoeud / 2, largeurCol, "Source de risque", s.SourceRisque, "#EEF0FF", BleuFrance);
            // Colonne 1 : objectif visé + description du scénario.
            var x1 = 12 + largeurCol + gap;
            Noeud(blocs, x1, yCentre - hauteurNoeud / 2, largeurCol, "Objectif visé : " + Tronquer(s.ObjectifVise, 28), s.Description, "#F6F6F6", GrisTexte);
            Fleche(blocs, 12 + largeurCol, yCentre, x1, yCentre);

            var xObjet = 12 + (2 + maxParties) * (largeurCol + gap);

            for (var li = 0; li < lignes; li++)
            {
                var yLigne = y + li * (hauteurNoeud + gapVertical) + hauteurNoeud / 2;
                var chemin = li < s.Chemins.Count ? s.Chemins[li] : new CheminArbre("", Array.Empty<string>());

                Fleche(blocs, x1 + largeurCol, yCentre, x1 + largeurCol + gap / 2, yLigne);
                Fleche(blocs, x1 + largeurCol + gap / 2, yLigne, x1 + largeurCol + gap, yLigne);

                var xPrec = x1 + largeurCol + gap;
                for (var pi = 0; pi < maxParties; pi++)
                {
                    var xCol = 12 + (2 + pi) * (largeurCol + gap);
                    if (pi < chemin.PartiesTraversees.Count)
                    {
                        Noeud(blocs, xCol, yLigne - hauteurNoeud / 2, largeurCol, "Partie prenante", chemin.PartiesTraversees[pi], "#FBF0EA", Danger);
                        Fleche(blocs, xPrec, yLigne, xCol, yLigne);
                        xPrec = xCol + largeurCol;
                    }
                }
                Fleche(blocs, xPrec, yLigne, xObjet, yLigne);
            }

            Noeud(blocs, xObjet, yCentre - hauteurNoeud / 2, largeurCol,
                "Objet de l'étude (G" + s.Gravite + ")", s.EvenementRedoute, BleuFrance, "white", texteBlanc: true);

            y += hauteurBloc + 46;
        }

        var hauteur = Math.Max(120, y);
        sb.Append(I($"<svg viewBox=\"0 0 {N(largeur)} {N(hauteur)}\" xmlns=\"http://www.w3.org/2000/svg\" font-family=\"{Police}\">"));
        sb.Append(I($"<defs><marker id=\"fl\" markerWidth=\"7\" markerHeight=\"7\" refX=\"6\" refY=\"3.5\" orient=\"auto\"><path d=\"M0,0 L7,3.5 L0,7 z\" fill=\"{GrisTexte}\" /></marker></defs>"));
        sb.Append(blocs);
        sb.Append("</svg>");
        return sb.ToString();
    }

    private static void Noeud(StringBuilder sb, double x, double y, double w, string entete, string corps, string fond, string couleurBord, bool texteBlanc = false)
    {
        var couleurTexte = texteBlanc ? "white" : Encre;
        var couleurEntete = texteBlanc ? "#D9D9F5" : GrisTexte;
        sb.Append(I($"<rect x=\"{N(x)}\" y=\"{N(y)}\" width=\"{N(w)}\" height=\"52\" rx=\"5\" fill=\"{fond}\" stroke=\"{couleurBord}\" stroke-width=\"1\" />"));
        sb.Append(I($"<text x=\"{N(x + 8)}\" y=\"{N(y + 15)}\" font-size=\"8\" fill=\"{couleurEntete}\">{E(Tronquer(entete, 30))}</text>"));
        foreach (var (ligne, li) in DecouperEnLignes(corps, 30, 2).Select((l, i) => (l, i)))
            sb.Append(I($"<text x=\"{N(x + 8)}\" y=\"{N(y + 30 + li * 12)}\" font-size=\"9.5\" fill=\"{couleurTexte}\">{E(ligne)}</text>"));
    }

    private static void Fleche(StringBuilder sb, double x1, double y1, double x2, double y2)
        => sb.Append(I($"<line x1=\"{N(x1)}\" y1=\"{N(y1)}\" x2=\"{N(x2)}\" y2=\"{N(y2)}\" stroke=\"{GrisTexte}\" stroke-width=\"1\" marker-end=\"url(#fl)\" />"));

    private static IEnumerable<string> DecouperEnLignes(string texte, int largeur, int maxLignes)
    {
        var mots = texte.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lignes = new List<string>();
        var courante = "";
        var reste = false;
        for (var mi = 0; mi < mots.Length; mi++)
        {
            var mot = mots[mi];
            if (courante.Length == 0) courante = mot;
            else if (courante.Length + 1 + mot.Length <= largeur) courante += " " + mot;
            else
            {
                lignes.Add(courante);
                courante = mot;
                if (lignes.Count == maxLignes) { reste = true; courante = ""; break; }
            }
        }
        if (courante.Length > 0 && lignes.Count < maxLignes) lignes.Add(courante);
        else if (courante.Length > 0) reste = true;

        if (reste && lignes.Count > 0)
        {
            var derniere = lignes[^1];
            if (derniere.Length > largeur - 1) derniere = derniere[..(largeur - 1)];
            lignes[^1] = derniere.TrimEnd() + "…";
        }
        return lignes.Count == 0 ? new[] { "" } : lignes;
    }

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;
    private static double Clamp(double t) => Math.Max(0, Math.Min(1, t));
}
