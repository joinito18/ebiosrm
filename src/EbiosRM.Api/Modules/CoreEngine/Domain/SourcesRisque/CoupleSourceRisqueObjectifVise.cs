namespace EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;

public enum CategorieSourceRisque
{
    Etatique,
    CrimeOrganise,
    Terroriste,
    ActivisteIdeologique,
    OfficineSpecialisee,
    Amateur,
    Vengeur,
    MalveillantPathologique,
    Autre
}

public enum CategorieObjectifVise
{
    EspionnageEtatiqueOuIndustriel,
    PrePositionnementStrategique,
    InfluenceDestabilisation,
    EntraveAuFonctionnement,
    SabotageDestruction,
    Lucratif,
    DefiAmusement,
    Autre
}

/// <summary>
/// Niveau de pertinence dérivé, jamais saisi manuellement (cf. Phase 2 §6.5).
/// 4 niveaux, cohérent avec le rapport PDF Atelier 2 (cartographie de synthèse).
/// Calculé exclusivement par ServiceCalculPertinence à partir de la matrice
/// Motivation x Ressources.
/// </summary>
public enum NiveauPertinence
{
    PeuPertinent,
    MoyennementPertinent,
    PlutotPertinent,
    TresPertinent
}

/// <summary>
/// Aggregate Root : CoupleSourceRisqueObjectifVise (module Sources de risque, Atelier 2).
/// Theme : même convention que SocleSecurite.Theme (string libre, pas d'enum
/// partagé -- SocleSecurite ne définit pas non plus d'enum, juste une
/// convention de 4 valeurs alignée sur les thèmes ISO 27001 Annexe A).
/// </summary>
public sealed class CoupleSourceRisqueObjectifVise
{
    public const int EchelleMin = 1;
    public const int EchelleMax = 4;

    public Guid Id { get; private set; }
    public Guid EtudeId { get; private set; }
    public CategorieSourceRisque SourceRisque { get; private set; }
    public string DescriptionSourceRisque { get; private set; } = default!;
    public CategorieObjectifVise ObjectifVise { get; private set; }
    public string DescriptionObjectifVise { get; private set; } = default!;
    public string ContexteVulnerabilite { get; private set; } = default!;
    public string Theme { get; private set; } = default!;
    public int Motivation { get; private set; }
    public int Ressources { get; private set; }

    /// <summary>Pertinence calculée par ServiceCalculPertinence, jamais saisie manuellement.</summary>
    public NiveauPertinence PertinenceCalculee { get; private set; }

    /// <summary>Écart de jugement d'expert de l'analyste, le cas échéant.</summary>
    public NiveauPertinence? PertinenceRetenue { get; private set; }
    public string? JustificationPertinence { get; private set; }

    /// <summary>Valeur effective : la retenue si l'analyste s'est écarté du calcul, sinon la calculée.</summary>
    public NiveauPertinence Pertinence => PertinenceRetenue ?? PertinenceCalculee;

    public DateTime CreeLeUtc { get; private set; }

    private CoupleSourceRisqueObjectifVise() { }

    public static CoupleSourceRisqueObjectifVise Creer(
        Guid etudeId,
        CategorieSourceRisque sourceRisque,
        string descriptionSourceRisque,
        CategorieObjectifVise objectifVise,
        string descriptionObjectifVise,
        string contexteVulnerabilite,
        string theme,
        int motivation,
        int ressources,
        NiveauPertinence pertinenceCalculee)
    {
        if (etudeId == Guid.Empty)
            throw new ArgumentException("Le couple SR/OV doit être rattaché à une étude.", nameof(etudeId));
        if (string.IsNullOrWhiteSpace(descriptionSourceRisque))
            throw new ArgumentException("La description de la source de risque est obligatoire.", nameof(descriptionSourceRisque));
        if (string.IsNullOrWhiteSpace(descriptionObjectifVise))
            throw new ArgumentException("La description de l'objectif visé est obligatoire.", nameof(descriptionObjectifVise));
        if (string.IsNullOrWhiteSpace(contexteVulnerabilite))
            throw new ArgumentException("Le contexte/vulnérabilité associée est obligatoire.", nameof(contexteVulnerabilite));
        if (string.IsNullOrWhiteSpace(theme))
            throw new ArgumentException("Le thème est obligatoire.", nameof(theme));
        if (motivation < EchelleMin || motivation > EchelleMax)
            throw new ArgumentOutOfRangeException(nameof(motivation), motivation,
                $"La motivation doit être comprise entre {EchelleMin} et {EchelleMax}.");
        if (ressources < EchelleMin || ressources > EchelleMax)
            throw new ArgumentOutOfRangeException(nameof(ressources), ressources,
                $"Les ressources doivent être comprises entre {EchelleMin} et {EchelleMax}.");

        return new CoupleSourceRisqueObjectifVise
        {
            Id = Guid.NewGuid(),
            EtudeId = etudeId,
            SourceRisque = sourceRisque,
            DescriptionSourceRisque = descriptionSourceRisque.Trim(),
            ObjectifVise = objectifVise,
            DescriptionObjectifVise = descriptionObjectifVise.Trim(),
            ContexteVulnerabilite = contexteVulnerabilite.Trim(),
            Theme = theme.Trim(),
            Motivation = motivation,
            Ressources = ressources,
            PertinenceCalculee = pertinenceCalculee,
            CreeLeUtc = DateTime.UtcNow
        };
    }

    public void Modifier(
        CategorieSourceRisque sourceRisque,
        string descriptionSourceRisque,
        CategorieObjectifVise objectifVise,
        string descriptionObjectifVise,
        string contexteVulnerabilite,
        string theme,
        int motivation,
        int ressources,
        NiveauPertinence pertinenceCalculee)
    {
        if (string.IsNullOrWhiteSpace(descriptionSourceRisque))
            throw new ArgumentException("La description de la source de risque est obligatoire.", nameof(descriptionSourceRisque));
        if (string.IsNullOrWhiteSpace(descriptionObjectifVise))
            throw new ArgumentException("La description de l'objectif visé est obligatoire.", nameof(descriptionObjectifVise));
        if (string.IsNullOrWhiteSpace(contexteVulnerabilite))
            throw new ArgumentException("Le contexte/vulnérabilité associée est obligatoire.", nameof(contexteVulnerabilite));
        if (string.IsNullOrWhiteSpace(theme))
            throw new ArgumentException("Le thème est obligatoire.", nameof(theme));
        if (motivation < EchelleMin || motivation > EchelleMax)
            throw new ArgumentOutOfRangeException(nameof(motivation), motivation,
                $"La motivation doit être comprise entre {EchelleMin} et {EchelleMax}.");
        if (ressources < EchelleMin || ressources > EchelleMax)
            throw new ArgumentOutOfRangeException(nameof(ressources), ressources,
                $"Les ressources doivent être comprises entre {EchelleMin} et {EchelleMax}.");

        SourceRisque = sourceRisque;
        DescriptionSourceRisque = descriptionSourceRisque.Trim();
        ObjectifVise = objectifVise;
        DescriptionObjectifVise = descriptionObjectifVise.Trim();
        ContexteVulnerabilite = contexteVulnerabilite.Trim();
        Theme = theme.Trim();
        Motivation = motivation;
        Ressources = ressources;
        PertinenceCalculee = pertinenceCalculee;
        // L'override (PertinenceRetenue/JustificationPertinence), s'il existe, n'est
        // jamais effacé par une modification des entrées -- sticky jusqu'à DefinirPertinenceRetenue/
        // ReinitialiserPertinence explicites.
    }

    /// <summary>
    /// Enregistre le jugement d'expert de l'analyste, qui prévaut sur le calcul
    /// mécanique (méthode EBIOS RM : le jugement d'expert est une méthode
    /// d'évaluation valable). Justification obligatoire.
    /// </summary>
    public void DefinirPertinenceRetenue(NiveauPertinence pertinenceRetenue, string justification)
    {
        if (string.IsNullOrWhiteSpace(justification))
            throw new ArgumentException("Une justification est obligatoire pour retenir une pertinence différente du calcul.", nameof(justification));

        PertinenceRetenue = pertinenceRetenue;
        JustificationPertinence = justification.Trim();
    }

    /// <summary>Revient à la valeur calculée, efface l'écart de l'analyste.</summary>
    public void ReinitialiserPertinence()
    {
        PertinenceRetenue = null;
        JustificationPertinence = null;
    }
}
