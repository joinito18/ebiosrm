using EbiosRM.Api.Modules.Bibliotheque.Domain;

namespace EbiosRM.Api.Modules.Conformite.Domain;

public enum ReferentielConformite
{
    Iso27001,
    Nis2,
}

/// <summary>
/// Une exigence d'un référentiel réglementaire ou normatif.
///   - ISO/IEC 27001:2022 : les 93 mesures de l'Annexe A (source
///     <see cref="CatalogueIso27002"/>).
///   - NIS2 : les 10 domaines de mesures de gestion des risques de l'article
///     21(2) a) à j) de la directive (UE) 2022/2555.
/// </summary>
public sealed record ExigenceConformite(ReferentielConformite Referentiel, string Code, string Titre, string Categorie);

/// <summary>
/// Catalogues d'exigences + correspondance <b>indicative</b> ISO 27001 -> NIS2.
/// Embarqué dans le code (offline). La correspondance est une aide : elle doit
/// toujours être validée par l'analyste pour un contexte donné.
/// </summary>
public static class CatalogueConformite
{
    public static readonly IReadOnlyList<ExigenceConformite> Iso27001 =
        CatalogueIso27002.Controles
            .Select(c => new ExigenceConformite(ReferentielConformite.Iso27001, c.Code, c.Titre, c.Theme))
            .ToList();

    public static readonly IReadOnlyList<ExigenceConformite> Nis2 = new[]
    {
        Nis("21.2.a", "Analyse des risques et politiques de sécurité des systèmes d'information", "Gouvernance"),
        Nis("21.2.b", "Gestion des incidents", "Détection et réaction"),
        Nis("21.2.c", "Continuité des activités, gestion des sauvegardes, reprise après sinistre et gestion des crises", "Résilience"),
        Nis("21.2.d", "Sécurité de la chaîne d'approvisionnement", "Écosystème"),
        Nis("21.2.e", "Sécurité de l'acquisition, du développement et de la maintenance, y compris la gestion et la divulgation des vulnérabilités", "Protection"),
        Nis("21.2.f", "Politiques et procédures d'évaluation de l'efficacité des mesures de gestion des risques", "Gouvernance"),
        Nis("21.2.g", "Pratiques de base en matière de cyberhygiène et formation à la cybersécurité", "Protection"),
        Nis("21.2.h", "Politiques et procédures relatives à l'utilisation de la cryptographie et, le cas échéant, du chiffrement", "Protection"),
        Nis("21.2.i", "Sécurité des ressources humaines, politiques de contrôle d'accès et gestion des actifs", "Protection"),
        Nis("21.2.j", "Authentification à plusieurs facteurs, communications vocales/vidéo/texte sécurisées et communications d'urgence sécurisées", "Protection"),
    };

    private static ExigenceConformite Nis(string code, string titre, string categorie)
        => new(ReferentielConformite.Nis2, code, titre, categorie);

    /// <summary>
    /// Correspondance indicative : pour chaque domaine NIS2, les mesures ISO
    /// 27001 Annexe A qui y contribuent. Sert à dériver une couverture NIS2 à
    /// partir de l'état de conformité ISO d'une étude.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> CorrespondanceNis2VersIso = new Dictionary<string, IReadOnlyList<string>>
    {
        ["21.2.a"] = new[] { "A.5.1", "A.5.2", "A.5.4", "A.5.8", "A.5.31", "A.5.36" },
        ["21.2.b"] = new[] { "A.5.24", "A.5.25", "A.5.26", "A.5.27", "A.5.28", "A.6.8", "A.8.15", "A.8.16" },
        ["21.2.c"] = new[] { "A.5.29", "A.5.30", "A.7.5", "A.8.13", "A.8.14" },
        ["21.2.d"] = new[] { "A.5.19", "A.5.20", "A.5.21", "A.5.22", "A.5.23" },
        ["21.2.e"] = new[] { "A.5.7", "A.8.8", "A.8.25", "A.8.26", "A.8.27", "A.8.28", "A.8.29", "A.8.30", "A.8.31", "A.8.32" },
        ["21.2.f"] = new[] { "A.5.35", "A.5.36", "A.8.34" },
        ["21.2.g"] = new[] { "A.5.10", "A.6.3", "A.7.7", "A.8.7", "A.8.19" },
        ["21.2.h"] = new[] { "A.8.24" },
        ["21.2.i"] = new[] { "A.5.9", "A.5.10", "A.5.11", "A.5.12", "A.5.15", "A.5.16", "A.5.18", "A.6.1", "A.6.2", "A.6.4", "A.6.5", "A.6.6", "A.8.2", "A.8.3" },
        ["21.2.j"] = new[] { "A.5.14", "A.5.17", "A.8.5", "A.8.20", "A.8.21" },
    };

    public static IReadOnlyList<ExigenceConformite> Pour(ReferentielConformite referentiel)
        => referentiel == ReferentielConformite.Nis2 ? Nis2 : Iso27001;

    public static ExigenceConformite? Trouver(string code)
        => Iso27001.Concat(Nis2).FirstOrDefault(e => string.Equals(e.Code, code, StringComparison.OrdinalIgnoreCase));
}
