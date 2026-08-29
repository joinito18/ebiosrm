namespace EbiosRM.Api.Modules.Bibliotheque.Domain;

/// <summary>
/// Rend une entrée de bibliothèque personnelle visible par tous les comptes
/// (bibliothèque communautaire). Table à part : ne touche aucune des 7 tables
/// d'entrées, on publie/retire en créant/supprimant une ligne ici.
///
/// <see cref="Masquee"/> passe à vrai automatiquement quand
/// <see cref="SeuilMasquage"/> comptes distincts ont signalé l'entrée : elle
/// disparaît alors de la bibliothèque communautaire sans être supprimée de la
/// bibliothèque personnelle de son auteur.
/// </summary>
public sealed class PublicationBibliotheque
{
    public const int SeuilMasquage = 3;

    public Guid Id { get; private set; }

    /// <summary>Type d'entrée : « mesure », « source-risque », « partie-prenante », « valeur-metier », « bien-support », « evenement-redoute », « mode-operatoire ».</summary>
    public string TypeEntite { get; private set; } = default!;
    public Guid EntiteId { get; private set; }
    public Guid ProprietaireId { get; private set; }
    public DateTime PublieLeUtc { get; private set; }
    public bool Masquee { get; private set; }

    private readonly List<SignalementBibliotheque> _signalements = new();
    public IReadOnlyList<SignalementBibliotheque> Signalements => _signalements;

    private PublicationBibliotheque() { }

    public static PublicationBibliotheque Creer(string typeEntite, Guid entiteId, Guid proprietaireId)
    {
        if (string.IsNullOrWhiteSpace(typeEntite))
            throw new ArgumentException("Le type d'entrée est obligatoire.", nameof(typeEntite));
        if (entiteId == Guid.Empty || proprietaireId == Guid.Empty)
            throw new ArgumentException("Entrée et propriétaire obligatoires.");

        return new PublicationBibliotheque
        {
            Id = Guid.NewGuid(),
            TypeEntite = typeEntite,
            EntiteId = entiteId,
            ProprietaireId = proprietaireId,
            PublieLeUtc = DateTime.UtcNow,
            Masquee = false,
        };
    }

    /// <summary>
    /// Enregistre un signalement. Sans effet si ce compte a déjà signalé cette
    /// publication ou si c'est l'auteur lui-même. Repasse
    /// <see cref="Masquee"/> à vrai au-delà du seuil.
    /// </summary>
    public void Signaler(Guid signalePar, string? motif)
    {
        if (signalePar == ProprietaireId) return;
        if (_signalements.Any(s => s.SignalePar == signalePar)) return;

        _signalements.Add(SignalementBibliotheque.Creer(signalePar, motif));
        if (_signalements.Select(s => s.SignalePar).Distinct().Count() >= SeuilMasquage)
            Masquee = true;
    }
}

/// <summary>Signalement d'une publication communautaire (entité owned de <see cref="PublicationBibliotheque"/>).</summary>
public sealed class SignalementBibliotheque
{
    public Guid Id { get; private set; }
    public Guid SignalePar { get; private set; }
    public string? Motif { get; private set; }
    public DateTime CreeLeUtc { get; private set; }

    private SignalementBibliotheque() { }

    internal static SignalementBibliotheque Creer(Guid signalePar, string? motif)
        => new()
        {
            SignalePar = signalePar,
            Motif = string.IsNullOrWhiteSpace(motif) ? null : motif.Trim(),
            CreeLeUtc = DateTime.UtcNow,
        };
}
