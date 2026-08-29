namespace EbiosRM.Api.Modules.Bibliotheque.Domain;

/// <summary>
/// Une valeur métier type (Atelier 1), réutilisable d'une étude à l'autre :
/// « processus de paie », « dossier patient », « chaîne de production »,
/// « référentiel clients »... On mémorise l'intitulé, la nature ou finalité
/// (processus / information) et une entité propriétaire indicative.
/// </summary>
public sealed class ValeurMetierBibliotheque : IEntreeBibliotheque
{
    public Guid Id { get; private set; }
    public Guid? ProprietaireId { get; private set; }

    public string Intitule { get; private set; } = default!;

    /// <summary>Nature ou finalité (ex. « Processus », « Information », texte libre).</summary>
    public string? NatureOuFinalite { get; private set; }
    public string? EntiteProprietaireTypique { get; private set; }

    public DateTime CreeLeUtc { get; private set; }
    public bool EstSysteme => ProprietaireId is null;

    private ValeurMetierBibliotheque() { }

    public static ValeurMetierBibliotheque Creer(
        Guid proprietaireId, string intitule, string? natureOuFinalite, string? entiteProprietaireTypique)
    {
        if (string.IsNullOrWhiteSpace(intitule))
            throw new ArgumentException("L'intitulé de la valeur métier est obligatoire.", nameof(intitule));

        return new ValeurMetierBibliotheque
        {
            Id = Guid.NewGuid(),
            ProprietaireId = proprietaireId,
            Intitule = intitule.Trim(),
            NatureOuFinalite = string.IsNullOrWhiteSpace(natureOuFinalite) ? null : natureOuFinalite.Trim(),
            EntiteProprietaireTypique = string.IsNullOrWhiteSpace(entiteProprietaireTypique) ? null : entiteProprietaireTypique.Trim(),
            CreeLeUtc = DateTime.UtcNow,
        };
    }

    public static ValeurMetierBibliotheque Systeme(string cle, string intitule, string natureOuFinalite, string entiteProprietaireTypique)
        => new()
        {
            Id = MesureBibliotheque.IdDeterministe($"valeur-metier:{cle}"),
            ProprietaireId = null,
            Intitule = intitule,
            NatureOuFinalite = natureOuFinalite,
            EntiteProprietaireTypique = entiteProprietaireTypique,
            CreeLeUtc = default,
        };
}
