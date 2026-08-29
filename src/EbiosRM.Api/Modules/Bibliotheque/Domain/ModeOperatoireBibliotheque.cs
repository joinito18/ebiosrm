using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;

namespace EbiosRM.Api.Modules.Bibliotheque.Domain;

/// <summary>
/// Une action élémentaire d'un <see cref="ModeOperatoireBibliotheque"/>.
/// Contrairement à l'action élémentaire d'une étude (qui cible un bien support
/// par son Id), on ne mémorise ici qu'un <b>libellé de cible</b> (« poste de
/// travail bureautique », « annuaire Active Directory »…) : au moment de
/// reprendre le mode opératoire dans une étude, l'analyste associe chaque
/// action au bien support réel correspondant.
/// </summary>
public sealed class ActionElementaireBibliotheque
{
    public Guid Id { get; private set; }

    /// <summary>Position dans la séquence, à partir de 1.</summary>
    public int Ordre { get; private set; }
    public string Description { get; private set; } = default!;
    public PhaseActionElementaire Phase { get; private set; }

    /// <summary>Libellé du bien support type visé (pas un Id d'étude).</summary>
    public string? CibleBienSupport { get; private set; }
    public string? TechniqueMitre { get; private set; }

    private ActionElementaireBibliotheque() { }

    internal static ActionElementaireBibliotheque Creer(
        int ordre, string description, PhaseActionElementaire phase, string? cibleBienSupport, string? techniqueMitre)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La description de l'action élémentaire est obligatoire.", nameof(description));

        return new ActionElementaireBibliotheque
        {
            Ordre = ordre,
            Description = description.Trim(),
            Phase = phase,
            CibleBienSupport = string.IsNullOrWhiteSpace(cibleBienSupport) ? null : cibleBienSupport.Trim(),
            TechniqueMitre = string.IsNullOrWhiteSpace(techniqueMitre) ? null : techniqueMitre.Trim(),
        };
    }
}

/// <summary>Entrée immuable pour (re)construire la liste d'actions d'un mode opératoire de bibliothèque.</summary>
public readonly record struct ActionElementaireBiblioEntree(
    string Description, PhaseActionElementaire Phase, string? CibleBienSupport = null, string? TechniqueMitre = null);

/// <summary>
/// Un mode opératoire technique type (Atelier 4), réutilisable d'une étude à
/// l'autre : « rançongiciel par hameçonnage », « intrusion par un accès
/// distant exposé », « rebond par un prestataire »… Se décompose en actions
/// élémentaires réparties sur la séquence CONNAÎTRE / RENTRER / TROUVER /
/// EXPLOITER, avec des techniques MITRE ATT&amp;CK indicatives. Les cotations
/// (probabilité de succès / difficulté technique) sont indicatives.
/// </summary>
public sealed class ModeOperatoireBibliotheque : IEntreeBibliotheque
{
    public Guid Id { get; private set; }
    public Guid? ProprietaireId { get; private set; }

    public string Nom { get; private set; } = default!;
    public string? Description { get; private set; }
    public int? ProbabiliteSuccesTypique { get; private set; }
    public int? DifficulteTechniqueTypique { get; private set; }

    private readonly List<ActionElementaireBibliotheque> _actions = new();
    public IReadOnlyList<ActionElementaireBibliotheque> Actions => _actions;

    public DateTime CreeLeUtc { get; private set; }
    public bool EstSysteme => ProprietaireId is null;

    private ModeOperatoireBibliotheque() { }

    private static int? Borner(int? v) => v is null ? null : Math.Clamp(v.Value, 1, 4);

    private void RemplacerActions(IReadOnlyList<ActionElementaireBiblioEntree> actions)
    {
        if (actions.Count == 0)
            throw new ArgumentException("Au moins une action élémentaire est requise.", nameof(actions));

        _actions.Clear();
        var ordre = 1;
        foreach (var a in actions)
            _actions.Add(ActionElementaireBibliotheque.Creer(ordre++, a.Description, a.Phase, a.CibleBienSupport, a.TechniqueMitre));
    }

    public static ModeOperatoireBibliotheque Creer(
        Guid proprietaireId, string nom, string? description,
        int? probabiliteSuccesTypique, int? difficulteTechniqueTypique,
        IReadOnlyList<ActionElementaireBiblioEntree> actions)
    {
        if (string.IsNullOrWhiteSpace(nom))
            throw new ArgumentException("Le nom du mode opératoire est obligatoire.", nameof(nom));

        var mode = new ModeOperatoireBibliotheque
        {
            Id = Guid.NewGuid(),
            ProprietaireId = proprietaireId,
            Nom = nom.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            ProbabiliteSuccesTypique = Borner(probabiliteSuccesTypique),
            DifficulteTechniqueTypique = Borner(difficulteTechniqueTypique),
            CreeLeUtc = DateTime.UtcNow,
        };
        mode.RemplacerActions(actions);
        return mode;
    }

    public static ModeOperatoireBibliotheque Systeme(
        string cle, string nom, string description, int probabilite, int difficulte,
        params ActionElementaireBiblioEntree[] actions)
    {
        var mode = new ModeOperatoireBibliotheque
        {
            Id = MesureBibliotheque.IdDeterministe($"mode-operatoire:{cle}"),
            ProprietaireId = null,
            Nom = nom,
            Description = description,
            ProbabiliteSuccesTypique = probabilite,
            DifficulteTechniqueTypique = difficulte,
            CreeLeUtc = default,
        };
        var ordre = 1;
        foreach (var a in actions)
            mode._actions.Add(ActionElementaireBibliotheque.Creer(ordre++, a.Description, a.Phase, a.CibleBienSupport, a.TechniqueMitre));
        return mode;
    }

    public IEntreeBibliotheque CopiePrivee(Guid proprietaireId)
        => Creer(proprietaireId, Nom, Description, ProbabiliteSuccesTypique, DifficulteTechniqueTypique,
            _actions.OrderBy(a => a.Ordre)
                .Select(a => new ActionElementaireBiblioEntree(a.Description, a.Phase, a.CibleBienSupport, a.TechniqueMitre))
                .ToList());
}
