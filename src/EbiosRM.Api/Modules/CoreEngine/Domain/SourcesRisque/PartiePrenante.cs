namespace EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;

/// <summary>
/// Catégorisation de la partie prenante dans l'écosystème (grille officielle
/// EBIOS RM, cf. exemple "société de biotechnologie" : Clients, Partenaires,
/// Prestataires). "Autre" avec DescriptionCategorie libre pour ne pas
/// enfermer l'analyste dans une liste non exhaustive.
/// </summary>
public enum CategoriePartiePrenante
{
    Client,
    Partenaire,
    Prestataire,
    Autre
}

/// <summary>
/// Zone de criticité dérivée du niveau de dangerosité (grille officielle
/// EBIOS RM). Les parties prenantes en zone Contrôle ou Danger sont dites
/// "critiques" et définissent le périmètre réel de l'analyse de risque de
/// l'Atelier 3.
/// </summary>
public enum ZoneDangerosite
{
    Veille,
    Controle,
    Danger
}

/// <summary>
/// Mesure de sécurité proposée sur l'écosystème pour réduire la dangerosité
/// induite par une partie prenante critique (doc officielle Atelier 3,
/// partie 3 : "réduire le niveau de dangerosité induit par les parties
/// prenantes critiques" / "agir sur le déroulement des scénarios
/// stratégiques"). Entité owned de PartiePrenante -- texte libre, la
/// réévaluation de la dangerosité résiduelle se fait séparément
/// (EvaluerDangerositeResiduelle), pas par mesure individuelle (cohérent
/// avec l'exemple officiel : plusieurs mesures peuvent contribuer à une
/// même dangerosité résiduelle recalculée globalement).
/// </summary>
public sealed class MesureEcosysteme
{
    public Guid Id { get; private set; }
    public string Description { get; private set; } = default!;
    public DateTime CreeLeUtc { get; private set; }

    private MesureEcosysteme() { }

    internal static MesureEcosysteme Creer(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La description de la mesure est obligatoire.", nameof(description));

        return new MesureEcosysteme
        {
            // Id volontairement non assigné (ValueGeneratedOnAdd -- cf. leçon
            // EvenementIntermediaire/CheminAttaque).
            Description = description.Trim(),
            CreeLeUtc = DateTime.UtcNow
        };
    }

    internal void Modifier(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La description de la mesure est obligatoire.", nameof(description));
        Description = description.Trim();
    }
}

/// <summary>
/// Aggregate Root : PartiePrenante (module Sources de risque, Atelier 2).
/// </summary>
public sealed class PartiePrenante
{
    public const int EchelleMin = 1;
    public const int EchelleMax = 4;

    public Guid Id { get; private set; }
    public Guid EtudeId { get; private set; }
    public string Nom { get; private set; } = default!;
    public string RolesEtAttentes { get; private set; } = default!;
    public string Representant { get; private set; } = default!;
    public CategoriePartiePrenante Categorie { get; private set; }
    public string? DescriptionCategorie { get; private set; }

    /// <summary>
    /// Évaluation de la dangerosité (Atelier 3, en fin de chaîne d'écosystème
    /// -- distinct de la création de la partie prenante en Atelier 2).
    /// "Dangerosité" est le terme officiel depuis EBIOS RM 1.5 (mars 2024,
    /// conformité ISO/CEI 27005:2022) -- remplace "Menace", jugé inexact car
    /// impliquant une intention hostile délibérée non vérifiée pour toute
    /// partie prenante (ex. sous-traitant simplement négligent). Nulls tant
    /// que l'évaluation n'a pas encore eu lieu.
    /// </summary>
    public int? Dependance { get; private set; }
    public int? Penetration { get; private set; }
    public int? MaturiteCyber { get; private set; }
    public int? Confiance { get; private set; }

    /// <summary>Niveau calculé par ServiceCalculNiveauDangerosite, jamais saisi manuellement.</summary>
    public double? NiveauDangerositeCalcule { get; private set; }

    /// <summary>Écart de jugement d'expert de l'analyste, le cas échéant.</summary>
    public double? NiveauDangerositeRetenu { get; private set; }
    public string? JustificationDangerosite { get; private set; }

    /// <summary>Valeur effective : la retenue si l'analyste s'est écarté du calcul, sinon la calculée.</summary>
    public double? NiveauDangerosite => NiveauDangerositeRetenu ?? NiveauDangerositeCalcule;

    /// <summary>
    /// Zone de criticité, dérivée de NiveauDangerosite (effectif) par
    /// ServiceCalculNiveauDangerosite -- jamais persistée (calculée à la
    /// volée, [NotMapped] côté EF Core), cohérent avec l'invariant "aucune
    /// valeur dérivée saisie ou stockée séparément de sa source de calcul".
    /// </summary>
    public ZoneDangerosite? Zone => NiveauDangerosite.HasValue ? ServiceCalculNiveauDangerosite.DeterminerZone(NiveauDangerosite.Value) : null;

    /// <summary>
    /// Mesures de sécurité proposées sur l'écosystème (Atelier 3, partie 3).
    /// </summary>
    private readonly List<MesureEcosysteme> _mesures = new();
    public IReadOnlyList<MesureEcosysteme> Mesures => _mesures;

    /// <summary>
    /// Réévaluation de la dangerosité après application des mesures de
    /// sécurité (dangerosité résiduelle). Nulls tant qu'aucune réévaluation
    /// n'a eu lieu -- distincte de l'évaluation initiale
    /// (Dependance/Penetration/...), qui reste inchangée comme référence
    /// "avant mesures".
    /// </summary>
    public int? DependanceResiduelle { get; private set; }
    public int? PenetrationResiduelle { get; private set; }
    public int? MaturiteCyberResiduelle { get; private set; }
    public int? ConfianceResiduelle { get; private set; }
    public double? NiveauDangerositeResiduelCalcule { get; private set; }
    public double? NiveauDangerositeResiduelRetenu { get; private set; }
    public string? JustificationDangerositeResiduelle { get; private set; }
    public double? NiveauDangerositeResiduel => NiveauDangerositeResiduelRetenu ?? NiveauDangerositeResiduelCalcule;
    public ZoneDangerosite? ZoneResiduelle => NiveauDangerositeResiduel.HasValue ? ServiceCalculNiveauDangerosite.DeterminerZone(NiveauDangerositeResiduel.Value) : null;

    public DateTime CreeLeUtc { get; private set; }

    private PartiePrenante() { }

    public static PartiePrenante Creer(
        Guid etudeId, string nom, string rolesEtAttentes, string representant,
        CategoriePartiePrenante categorie, string? descriptionCategorie = null)
    {
        if (etudeId == Guid.Empty)
            throw new ArgumentException("La partie prenante doit être rattachée à une étude.", nameof(etudeId));
        if (string.IsNullOrWhiteSpace(nom))
            throw new ArgumentException("Le nom de la partie prenante est obligatoire.", nameof(nom));
        if (string.IsNullOrWhiteSpace(rolesEtAttentes))
            throw new ArgumentException("Les rôles et attentes sont obligatoires.", nameof(rolesEtAttentes));
        if (string.IsNullOrWhiteSpace(representant))
            throw new ArgumentException("Le représentant est obligatoire.", nameof(representant));
        if (categorie == CategoriePartiePrenante.Autre && string.IsNullOrWhiteSpace(descriptionCategorie))
            throw new ArgumentException("Une description est obligatoire pour la catégorie 'Autre'.", nameof(descriptionCategorie));

        return new PartiePrenante
        {
            Id = Guid.NewGuid(),
            EtudeId = etudeId,
            Nom = nom.Trim(),
            RolesEtAttentes = rolesEtAttentes.Trim(),
            Representant = representant.Trim(),
            Categorie = categorie,
            DescriptionCategorie = string.IsNullOrWhiteSpace(descriptionCategorie) ? null : descriptionCategorie.Trim(),
            CreeLeUtc = DateTime.UtcNow
        };
    }

    public void Modifier(
        string nom, string rolesEtAttentes, string representant,
        CategoriePartiePrenante categorie, string? descriptionCategorie = null)
    {
        if (string.IsNullOrWhiteSpace(nom))
            throw new ArgumentException("Le nom de la partie prenante est obligatoire.", nameof(nom));
        if (string.IsNullOrWhiteSpace(rolesEtAttentes))
            throw new ArgumentException("Les rôles et attentes sont obligatoires.", nameof(rolesEtAttentes));
        if (string.IsNullOrWhiteSpace(representant))
            throw new ArgumentException("Le représentant est obligatoire.", nameof(representant));
        if (categorie == CategoriePartiePrenante.Autre && string.IsNullOrWhiteSpace(descriptionCategorie))
            throw new ArgumentException("Une description est obligatoire pour la catégorie 'Autre'.", nameof(descriptionCategorie));

        Nom = nom.Trim();
        RolesEtAttentes = rolesEtAttentes.Trim();
        Representant = representant.Trim();
        Categorie = categorie;
        DescriptionCategorie = string.IsNullOrWhiteSpace(descriptionCategorie) ? null : descriptionCategorie.Trim();
    }

    /// <summary>
    /// Enregistre l'évaluation de la dangerosité (Atelier 3). Le niveau est
    /// calculé en amont par ServiceCalculNiveauDangerosite, jamais ici --
    /// aucune valeur dérivée n'est jamais saisissable ni calculée en dehors
    /// du Core Engine (cf. §6.5 invariants).
    /// </summary>
    public void EvaluerDangerosite(int dependance, int penetration, int maturiteCyber, int confiance, double niveauDangerositeCalcule)
    {
        Dependance = dependance;
        Penetration = penetration;
        MaturiteCyber = maturiteCyber;
        Confiance = confiance;
        NiveauDangerositeCalcule = niveauDangerositeCalcule;
        // L'override (NiveauDangerositeRetenu/JustificationDangerosite), s'il existe,
        // n'est jamais effacé par une réévaluation des entrées -- sticky jusqu'à
        // DefinirDangerositeRetenue/ReinitialiserDangerosite explicites.
    }

    /// <summary>
    /// Enregistre la dangerosité résiduelle (après application des mesures
    /// de sécurité de l'écosystème). Même principe que EvaluerDangerosite :
    /// le niveau est calculé en amont, jamais ici.
    /// </summary>
    public void EvaluerDangerositeResiduelle(int dependance, int penetration, int maturiteCyber, int confiance, double niveauDangerositeCalcule)
    {
        DependanceResiduelle = dependance;
        PenetrationResiduelle = penetration;
        MaturiteCyberResiduelle = maturiteCyber;
        ConfianceResiduelle = confiance;
        NiveauDangerositeResiduelCalcule = niveauDangerositeCalcule;
    }

    /// <summary>
    /// Enregistre le jugement d'expert de l'analyste sur la dangerosité
    /// initiale, qui prévaut sur le calcul mécanique. Justification obligatoire.
    /// </summary>
    public void DefinirDangerositeRetenue(double niveauRetenu, string justification)
    {
        if (string.IsNullOrWhiteSpace(justification))
            throw new ArgumentException("Une justification est obligatoire pour retenir une dangerosité différente du calcul.", nameof(justification));
        if (niveauRetenu < 0.0625 || niveauRetenu > 16)
            throw new ArgumentOutOfRangeException(nameof(niveauRetenu), niveauRetenu, "Le niveau retenu doit rester dans les bornes théoriques de la formule (0.0625 à 16).");

        NiveauDangerositeRetenu = niveauRetenu;
        JustificationDangerosite = justification.Trim();
    }

    /// <summary>Revient à la valeur calculée, efface l'écart de l'analyste.</summary>
    public void ReinitialiserDangerosite()
    {
        NiveauDangerositeRetenu = null;
        JustificationDangerosite = null;
    }

    /// <summary>Même principe que DefinirDangerositeRetenue, pour la dangerosité résiduelle.</summary>
    public void DefinirDangerositeResiduelleRetenue(double niveauRetenu, string justification)
    {
        if (string.IsNullOrWhiteSpace(justification))
            throw new ArgumentException("Une justification est obligatoire pour retenir une dangerosité résiduelle différente du calcul.", nameof(justification));
        if (niveauRetenu < 0.0625 || niveauRetenu > 16)
            throw new ArgumentOutOfRangeException(nameof(niveauRetenu), niveauRetenu, "Le niveau retenu doit rester dans les bornes théoriques de la formule (0.0625 à 16).");

        NiveauDangerositeResiduelRetenu = niveauRetenu;
        JustificationDangerositeResiduelle = justification.Trim();
    }

    public void ReinitialiserDangerositeResiduelle()
    {
        NiveauDangerositeResiduelRetenu = null;
        JustificationDangerositeResiduelle = null;
    }

    public void AjouterMesure(string description)
    {
        _mesures.Add(MesureEcosysteme.Creer(description));
    }

    public void ModifierMesure(Guid mesureId, string description)
    {
        var mesure = _mesures.FirstOrDefault(m => m.Id == mesureId);
        if (mesure is null)
            throw new ArgumentException("Mesure introuvable sur cette partie prenante.", nameof(mesureId));
        mesure.Modifier(description);
    }

    public void SupprimerMesure(Guid mesureId)
    {
        var mesure = _mesures.FirstOrDefault(m => m.Id == mesureId);
        if (mesure is null)
            throw new ArgumentException("Mesure introuvable sur cette partie prenante.", nameof(mesureId));
        _mesures.Remove(mesure);
    }
}
