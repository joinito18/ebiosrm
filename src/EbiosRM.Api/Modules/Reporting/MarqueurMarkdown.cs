using System.Text.RegularExpressions;

namespace EbiosRM.Api.Modules.Reporting;

/// <summary>
/// Lecteur Markdown minimaliste, en blocs typés — mêmes règles que le rendu
/// côté frontend (frontend/src/components/shared/Markdown.tsx). Utilisé par
/// <see cref="ManuelPdfGenerator"/> pour produire le manuel PDF à partir des
/// guides. Volontairement partiel : titres, paragraphes, listes, blocs de
/// code, citations, règles horizontales, tableaux pipe.
/// </summary>
public static class MarqueurMarkdown
{
    public abstract record Bloc;
    public sealed record Titre(int Niveau, string Texte) : Bloc;
    public sealed record Paragraphe(string Texte) : Bloc;
    public sealed record Citation(string Texte) : Bloc;
    public sealed record Code(string Texte) : Bloc;
    public sealed record Regle : Bloc;
    public sealed record ItemListe(string Texte, bool SousNiveau);
    public sealed record Liste(bool Ordonnee, IReadOnlyList<ItemListe> Items) : Bloc;
    public sealed record Tableau(IReadOnlyList<string> Entetes, IReadOnlyList<IReadOnlyList<string>> Lignes) : Bloc;

    public static IReadOnlyList<Bloc> Analyser(string markdown)
    {
        var lignes = markdown.Replace("\r\n", "\n").Split('\n');
        var blocs = new List<Bloc>();
        var para = new List<string>();
        var puces = new List<(int Indent, string Texte, bool Ordonnee)>();

        void ViderPara()
        {
            if (para.Count == 0) return;
            blocs.Add(new Paragraphe(string.Join(" ", para)));
            para.Clear();
        }

        void ViderPuces()
        {
            if (puces.Count == 0) return;
            var baseIndent = puces[0].Indent;
            var items = puces.Select(p => new ItemListe(p.Texte, p.Indent > baseIndent)).ToList();
            blocs.Add(new Liste(puces[0].Ordonnee, items));
            puces.Clear();
        }

        static IReadOnlyList<string> Cellules(string ligne)
            => ligne.Trim().Trim('|').Split('|').Select(c => c.Trim()).ToList();

        var i = 0;
        while (i < lignes.Length)
        {
            var ligne = lignes[i];

            if (ligne.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                ViderPara(); ViderPuces();
                var code = new List<string>();
                i++;
                while (i < lignes.Length && !lignes[i].TrimStart().StartsWith("```", StringComparison.Ordinal)) { code.Add(lignes[i]); i++; }
                i++;
                blocs.Add(new Code(string.Join("\n", code)));
                continue;
            }

            if (Regex.IsMatch(ligne.Trim(), @"^\|.*\|$") && i + 1 < lignes.Length && Regex.IsMatch(lignes[i + 1].Trim(), @"^\|[\s:|-]+\|$"))
            {
                ViderPara(); ViderPuces();
                var entetes = Cellules(ligne);
                i += 2;
                var corps = new List<IReadOnlyList<string>>();
                while (i < lignes.Length && Regex.IsMatch(lignes[i].Trim(), @"^\|.*\|$")) { corps.Add(Cellules(lignes[i])); i++; }
                blocs.Add(new Tableau(entetes, corps));
                continue;
            }

            var titre = Regex.Match(ligne, @"^(#{1,4})\s+(.*)$");
            if (titre.Success)
            {
                ViderPara(); ViderPuces();
                blocs.Add(new Titre(titre.Groups[1].Value.Length, titre.Groups[2].Value.Trim()));
                i++;
                continue;
            }

            if (Regex.IsMatch(ligne.Trim(), @"^---+$"))
            {
                ViderPara(); ViderPuces();
                blocs.Add(new Regle());
                i++;
                continue;
            }

            if (Regex.IsMatch(ligne, @"^>\s?"))
            {
                ViderPara(); ViderPuces();
                var cite = new List<string>();
                while (i < lignes.Length && Regex.IsMatch(lignes[i], @"^>\s?")) { cite.Add(Regex.Replace(lignes[i], @"^>\s?", "")); i++; }
                blocs.Add(new Citation(string.Join(" ", cite)));
                continue;
            }

            var puce = Regex.Match(ligne, @"^(\s*)([-*]|\d+\.)\s+(.*)$");
            if (puce.Success)
            {
                ViderPara();
                puces.Add((puce.Groups[1].Value.Length, puce.Groups[3].Value, Regex.IsMatch(puce.Groups[2].Value, @"\d+\.")));
                i++;
                continue;
            }

            if (ligne.Trim().Length == 0)
            {
                ViderPara(); ViderPuces();
                i++;
                continue;
            }

            // Ligne de continuation d'un item de liste (texte suite, souvent indente).
            if (puces.Count > 0 && para.Count == 0)
            {
                var d = puces[^1];
                puces[^1] = (d.Indent, d.Texte + " " + ligne.Trim(), d.Ordonnee);
                i++;
                continue;
            }

            para.Add(ligne.Trim());
            i++;
        }

        ViderPara(); ViderPuces();
        return blocs;
    }
}
