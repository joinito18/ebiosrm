using EbiosRM.Api.Modules.Conformite.Domain;
using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;
using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

namespace EbiosRM.Api.Modules.Conformite;

/// <summary>
/// Croise le contenu d'une étude (référentiels du socle de sécurité en
/// Atelier 1, mesures du plan de traitement en Atelier 5) avec les exigences
/// d'un référentiel réglementaire (ISO 27001 Annexe A, NIS2 art. 21), pour
/// produire un tableau de couverture.
///
/// Règles :
///   - une exigence ISO est <b>conforme</b> si le socle contient un référentiel
///     de même code à l'état « Conforme » ; <b>partielle</b> si le socle la
///     cite sans être conforme, ou si une mesure de traitement la vise ;
///     <b>non couverte</b> sinon ; <b>non applicable</b> si le socle le déclare.
///   - une exigence NIS2 est évaluée directement (mesure de traitement qui la
///     vise) et par dérivation de l'état ISO via la correspondance indicative
///     <see cref="CatalogueConformite.CorrespondanceNis2VersIso"/> ; on garde
///     le meilleur des deux.
/// </summary>
public sealed class ServiceConformite
{
    private readonly IEtudeRepository _etudes;
    private readonly ISocleSecuriteRepository _socles;
    private readonly IPlanTraitementRisqueRepository _plans;

    public ServiceConformite(IEtudeRepository etudes, ISocleSecuriteRepository socles, IPlanTraitementRisqueRepository plans)
    {
        _etudes = etudes;
        _socles = socles;
        _plans = plans;
    }

    public enum Couverture { NonCouverte, Partielle, Conforme, NonApplicable }

    public sealed record MesureLiee(Guid Id, string Description, string Statut);

    public sealed record LigneConformite(
        string Code, string Titre, string Categorie, Couverture Couverture,
        string? EtatSocle, IReadOnlyList<MesureLiee> Mesures);

    public sealed record SyntheseConformite(int Total, int Conforme, int Partielle, int NonCouverte, int NonApplicable);

    public sealed record RapportConformite(string Referentiel, SyntheseConformite Synthese, IReadOnlyList<LigneConformite> Lignes);

    public async Task<RapportConformite?> ConstruireAsync(Guid etudeId, ReferentielConformite referentiel, CancellationToken ct)
    {
        var etude = await _etudes.ObtenirParIdAsync(etudeId, ct);
        if (etude is null) return null;

        var socle = await _socles.ObtenirParEtudeAsync(etudeId, ct);
        var plan = await _plans.ObtenirParEtudeAsync(etudeId, ct);

        // État de conformité ISO déclaré dans le socle, par code de contrôle.
        // Le socle historique note « 5.1 », le catalogue « A.5.1 » -> on normalise.
        var etatSocleParCode = (socle?.Referentiels ?? Enumerable.Empty<ReferentielApplicable>())
            .Where(r => !string.IsNullOrWhiteSpace(r.CodeControle))
            .GroupBy(r => NormaliserCode(r.CodeControle!), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Etat, StringComparer.OrdinalIgnoreCase);

        // Mesures de traitement, indexées par code d'exigence couverte.
        var mesures = plan?.Mesures ?? new List<MesureTraitementRisque>();
        var mesuresParCode = new Dictionary<string, List<MesureLiee>>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in mesures)
            foreach (var code in m.CodesConformite)
            {
                var cle = NormaliserCode(code);
                if (!mesuresParCode.TryGetValue(cle, out var liste))
                    mesuresParCode[cle] = liste = new List<MesureLiee>();
                liste.Add(new MesureLiee(m.Id, m.Description, m.Statut.ToString()));
            }

        Couverture CouvertureIso(string code)
        {
            code = NormaliserCode(code);
            if (etatSocleParCode.TryGetValue(code, out var etat))
            {
                if (etat == EtatConformite.Conforme) return Couverture.Conforme;
                if (etat == EtatConformite.NonApplicable) return Couverture.NonApplicable;
                return Couverture.Partielle; // NonConforme : identifiée mais pas conforme
            }
            return mesuresParCode.ContainsKey(code) ? Couverture.Partielle : Couverture.NonCouverte;
        }

        var lignes = new List<LigneConformite>();

        foreach (var exigence in CatalogueConformite.Pour(referentiel))
        {
            Couverture couverture;
            string? etatSocle = null;

            if (referentiel == ReferentielConformite.Iso27001)
            {
                couverture = CouvertureIso(exigence.Code);
                if (etatSocleParCode.TryGetValue(NormaliserCode(exigence.Code), out var e)) etatSocle = e.ToString();
            }
            else
            {
                // NIS2 : direct (mesure) puis dérivation ISO, on garde le meilleur.
                var directe = mesuresParCode.ContainsKey(exigence.Code) ? Couverture.Partielle : Couverture.NonCouverte;

                var isoLiees = CatalogueConformite.CorrespondanceNis2VersIso.GetValueOrDefault(exigence.Code, Array.Empty<string>());
                var etats = isoLiees.Select(CouvertureIso).Where(c => c != Couverture.NonApplicable).ToList();
                var derivee = etats.Count == 0 ? Couverture.NonCouverte
                    : etats.All(c => c == Couverture.Conforme) ? Couverture.Conforme
                    : etats.Any(c => c is Couverture.Conforme or Couverture.Partielle) ? Couverture.Partielle
                    : Couverture.NonCouverte;

                couverture = (Couverture)Math.Max((int)directe, (int)derivee);
            }

            var mesuresLiees = mesuresParCode.GetValueOrDefault(NormaliserCode(exigence.Code), new List<MesureLiee>());
            lignes.Add(new LigneConformite(exigence.Code, exigence.Titre, exigence.Categorie, couverture, etatSocle, mesuresLiees));
        }

        var synthese = new SyntheseConformite(
            lignes.Count,
            lignes.Count(l => l.Couverture == Couverture.Conforme),
            lignes.Count(l => l.Couverture == Couverture.Partielle),
            lignes.Count(l => l.Couverture == Couverture.NonCouverte),
            lignes.Count(l => l.Couverture == Couverture.NonApplicable));

        return new RapportConformite(referentiel.ToString(), synthese, lignes);
    }

    /// <summary>
    /// « 5.1 » / « a.5.1 » / « A.5.1 » -> « A.5.1 ». Laisse tel quel un code
    /// non ISO (ex. « 21.2.b » pour NIS2).
    /// </summary>
    private static string NormaliserCode(string code)
    {
        var c = code.Trim();
        if (c.Length == 0) return c;
        if (c.StartsWith("A.", StringComparison.OrdinalIgnoreCase)) return "A." + c[2..];
        // Un code ISO 27002 est de la forme <chapitre>.<n> avec chapitre 5 à 8.
        if (c.Length > 1 && c[0] is '5' or '6' or '7' or '8' && c[1] == '.') return "A." + c;
        return c;
    }
}
