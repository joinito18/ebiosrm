using System.Globalization;
using System.Text;
using EbiosRM.Api.Modules.Bibliotheque.Domain;
using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;
using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;
using EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;

namespace EbiosRM.Api.Modules.Bibliotheque;

/// <summary>
/// Suggère des entrées de bibliothèque pertinentes pour une étude en cours, en
/// croisant les mots-clés du contenu de l'étude (événements redoutés, biens
/// support, couples SR/OV, chemins d'attaque) avec ceux des entrées candidates.
///
/// Scoring volontairement simple (recouvrement de mots-clés pondéré) : pas de
/// dépendance, résultat déterministe et explicable — chaque suggestion porte
/// les mots qui l'ont fait remonter.
/// </summary>
public sealed class ServiceSuggestionsBibliotheque
{
    private readonly IEvenementRedouteRepository _evenementsRedoutes;
    private readonly IBienSupportRepository _biensSupport;
    private readonly ICoupleSourceRisqueObjectifViseRepository _couples;
    private readonly ICheminAttaqueRepository _chemins;
    private readonly IPlanTraitementRisqueRepository _plans;
    private readonly IBibliothequeRepository _bibliotheque;

    public ServiceSuggestionsBibliotheque(
        IEvenementRedouteRepository evenementsRedoutes,
        IBienSupportRepository biensSupport,
        ICoupleSourceRisqueObjectifViseRepository couples,
        ICheminAttaqueRepository chemins,
        IPlanTraitementRisqueRepository plans,
        IBibliothequeRepository bibliotheque)
    {
        _evenementsRedoutes = evenementsRedoutes;
        _biensSupport = biensSupport;
        _couples = couples;
        _chemins = chemins;
        _plans = plans;
        _bibliotheque = bibliotheque;
    }

    public sealed record Suggestion(MesureBibliotheque Mesure, int Score, IReadOnlyList<string> MotsCles);

    private static readonly HashSet<string> MotsVides = new(StringComparer.Ordinal)
    {
        "le", "la", "les", "de", "des", "du", "un", "une", "et", "ou", "au", "aux", "en", "dans",
        "par", "pour", "sur", "sous", "avec", "sans", "ce", "cet", "cette", "ces", "son", "sa",
        "ses", "leur", "leurs", "qui", "que", "quoi", "dont", "est", "sont", "etre", "avoir", "plus",
        "moins", "tres", "peu", "non", "pas", "ne", "se", "si", "il", "elle", "ils", "elles", "on",
        "nous", "vous", "lui", "y", "d", "l", "s", "n", "c", "j", "m", "t", "a", "the", "of", "to",
        "and", "or", "for", "in", "on", "with", "prolongee", "suite", "cas", "type", "acces",
    };

    private static IEnumerable<string> Tokeniser(string? texte)
    {
        if (string.IsNullOrWhiteSpace(texte)) yield break;
        var sansAccents = new string(texte.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray());
        foreach (var brut in sansAccents.ToLowerInvariant().Split(
            new[] { ' ', '\t', '\n', '\r', ',', ';', '.', ':', '/', '\\', '(', ')', '[', ']', '"', '\'', '-', '_', '?', '!', '&' },
            StringSplitOptions.RemoveEmptyEntries))
        {
            var mot = brut.Trim();
            if (mot.Length >= 4 && !MotsVides.Contains(mot))
                yield return mot;
        }
    }

    public async Task<IReadOnlyList<Suggestion>> SuggererMesuresAsync(
        Guid etudeId, Guid proprietaireId, int limite, CancellationToken ct)
    {
        // 1. Sac de mots-clés du contexte de l'étude.
        var contexte = new HashSet<string>(StringComparer.Ordinal);
        foreach (var er in await _evenementsRedoutes.ListerParEtudeAsync(etudeId, ct))
            contexte.UnionWith(Tokeniser(er.Description));
        foreach (var bs in await _biensSupport.ListerParEtudeAsync(etudeId, ct))
        {
            contexte.UnionWith(Tokeniser(bs.Description));
            contexte.UnionWith(Tokeniser(bs.Type.ToString()));
        }
        foreach (var couple in await _couples.ListerParEtudeAsync(etudeId, ct))
        {
            contexte.UnionWith(Tokeniser(couple.DescriptionSourceRisque));
            contexte.UnionWith(Tokeniser(couple.DescriptionObjectifVise));
        }
        foreach (var chemin in await _chemins.ListerParEtudeAsync(etudeId, ct))
            contexte.UnionWith(Tokeniser(chemin.Description));

        if (contexte.Count == 0) return Array.Empty<Suggestion>();

        // 2. Mesures déjà dans le plan -> à exclure (comparaison sur les mots-clés).
        var plan = await _plans.ObtenirParEtudeAsync(etudeId, ct);
        var dejaCouvert = plan is null
            ? new HashSet<string>(StringComparer.Ordinal)
            : plan.Mesures.SelectMany(m => Tokeniser(m.Description)).ToHashSet(StringComparer.Ordinal);

        // 3. Candidats : catalogue système + bibliothèque personnelle.
        var candidats = CatalogueSysteme.Mesures
            .Concat(await _bibliotheque.ListerAsync<MesureBibliotheque>(proprietaireId, ct));

        var suggestions = new List<Suggestion>();
        foreach (var mesure in candidats)
        {
            var mots = Tokeniser(mesure.Titre).Concat(Tokeniser(mesure.Description)).Concat(Tokeniser(mesure.Categorie)).ToHashSet(StringComparer.Ordinal);
            var communs = mots.Where(contexte.Contains).ToList();
            if (communs.Count == 0) continue;

            // Malus si une mesure très proche est déjà dans le plan.
            var recouvrementPlan = mots.Count(dejaCouvert.Contains);
            var score = communs.Count * 2 - recouvrementPlan;
            if (score <= 0) continue;

            suggestions.Add(new Suggestion(mesure, score, communs.OrderBy(x => x).Take(5).ToList()));
        }

        return suggestions
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.Mesure.EstSysteme ? 1 : 0)
            .ThenBy(s => s.Mesure.Titre, StringComparer.OrdinalIgnoreCase)
            .Take(limite)
            .ToList();
    }
}
