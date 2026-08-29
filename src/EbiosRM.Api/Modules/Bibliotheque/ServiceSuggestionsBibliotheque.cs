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
    private readonly IPartiePrenanteRepository _partiesPrenantes;
    private readonly IPlanTraitementRisqueRepository _plans;
    private readonly IBibliothequeRepository _bibliotheque;

    public ServiceSuggestionsBibliotheque(
        IEvenementRedouteRepository evenementsRedoutes,
        IBienSupportRepository biensSupport,
        ICoupleSourceRisqueObjectifViseRepository couples,
        ICheminAttaqueRepository chemins,
        IPartiePrenanteRepository partiesPrenantes,
        IPlanTraitementRisqueRepository plans,
        IBibliothequeRepository bibliotheque)
    {
        _evenementsRedoutes = evenementsRedoutes;
        _biensSupport = biensSupport;
        _couples = couples;
        _chemins = chemins;
        _partiesPrenantes = partiesPrenantes;
        _plans = plans;
        _bibliotheque = bibliotheque;
    }

    public sealed record Suggestion<T>(T Entree, int Score, IReadOnlyList<string> MotsCles);

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

    /// <summary>Sac de mots-clés décrivant le contenu de l'étude.</summary>
    private async Task<HashSet<string>> ContexteAsync(Guid etudeId, CancellationToken ct)
    {
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
        return contexte;
    }

    private static List<Suggestion<T>> Scorer<T>(
        IEnumerable<T> candidats, HashSet<string> contexte, HashSet<string> aExclure,
        Func<T, IEnumerable<string>> motsDe, Func<T, bool> estSysteme, Func<T, string> nom, int limite)
    {
        var suggestions = new List<Suggestion<T>>();
        foreach (var candidat in candidats)
        {
            var mots = motsDe(candidat).ToHashSet(StringComparer.Ordinal);
            var communs = mots.Where(contexte.Contains).ToList();
            if (communs.Count == 0) continue;

            var malus = mots.Count(aExclure.Contains);
            var score = communs.Count * 2 - malus;
            if (score <= 0) continue;

            suggestions.Add(new Suggestion<T>(candidat, score, communs.OrderBy(x => x).Take(5).ToList()));
        }

        return suggestions
            .OrderByDescending(s => s.Score)
            .ThenBy(s => estSysteme(s.Entree) ? 1 : 0)
            .ThenBy(s => nom(s.Entree), StringComparer.OrdinalIgnoreCase)
            .Take(limite)
            .ToList();
    }

    public async Task<IReadOnlyList<Suggestion<MesureBibliotheque>>> SuggererMesuresAsync(
        Guid etudeId, Guid proprietaireId, int limite, CancellationToken ct)
    {
        var contexte = await ContexteAsync(etudeId, ct);
        if (contexte.Count == 0) return Array.Empty<Suggestion<MesureBibliotheque>>();

        var plan = await _plans.ObtenirParEtudeAsync(etudeId, ct);
        var deja = plan is null
            ? new HashSet<string>(StringComparer.Ordinal)
            : plan.Mesures.SelectMany(m => Tokeniser(m.Description)).ToHashSet(StringComparer.Ordinal);

        var candidats = CatalogueSysteme.Mesures
            .Concat(await _bibliotheque.ListerAsync<MesureBibliotheque>(proprietaireId, ct));

        return Scorer(candidats, contexte, deja,
            m => Tokeniser(m.Titre).Concat(Tokeniser(m.Description)).Concat(Tokeniser(m.Categorie)),
            m => m.EstSysteme, m => m.Titre, limite);
    }

    public async Task<IReadOnlyList<Suggestion<PartiePrenanteBibliotheque>>> SuggererPartiesPrenantesAsync(
        Guid etudeId, Guid proprietaireId, int limite, CancellationToken ct)
    {
        var contexte = await ContexteAsync(etudeId, ct);
        if (contexte.Count == 0) return Array.Empty<Suggestion<PartiePrenanteBibliotheque>>();

        var deja = (await _partiesPrenantes.ListerParEtudeAsync(etudeId, ct))
            .SelectMany(p => Tokeniser(p.Nom).Concat(Tokeniser(p.RolesEtAttentes)))
            .ToHashSet(StringComparer.Ordinal);

        var candidats = CatalogueSysteme.PartiesPrenantes
            .Concat(await _bibliotheque.ListerAsync<PartiePrenanteBibliotheque>(proprietaireId, ct));

        return Scorer(candidats, contexte, deja,
            p => Tokeniser(p.Nom).Concat(Tokeniser(p.RolesEtAttentes)).Concat(Tokeniser(p.DescriptionCategorie)),
            p => p.EstSysteme, p => p.Nom, limite);
    }

    public async Task<IReadOnlyList<Suggestion<ModeOperatoireBibliotheque>>> SuggererModesOperatoiresAsync(
        Guid etudeId, Guid proprietaireId, int limite, CancellationToken ct)
    {
        var contexte = await ContexteAsync(etudeId, ct);
        if (contexte.Count == 0) return Array.Empty<Suggestion<ModeOperatoireBibliotheque>>();

        var candidats = CatalogueSysteme.ModesOperatoires
            .Concat(await _bibliotheque.ListerAsync<ModeOperatoireBibliotheque>(proprietaireId, ct));

        return Scorer(candidats, contexte, new HashSet<string>(StringComparer.Ordinal),
            m => Tokeniser(m.Nom).Concat(Tokeniser(m.Description))
                .Concat(m.Actions.SelectMany(a => Tokeniser(a.Description).Concat(Tokeniser(a.TechniqueMitre)))),
            m => m.EstSysteme, m => m.Nom, limite);
    }
}
