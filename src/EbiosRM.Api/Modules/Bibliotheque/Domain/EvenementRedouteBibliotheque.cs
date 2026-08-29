using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

namespace EbiosRM.Api.Modules.Bibliotheque.Domain;

/// <summary>
/// Un événement redouté type (Atelier 1), réutilisable d'une étude à l'autre :
/// « indisponibilité prolongée du SI de production », « divulgation du fichier
/// clients », « altération des données de facturation »... Porte une gravité
/// <b>indicative</b> (échelle 1..4) et les types d'impacts généralement
/// associés (texte libre), que l'analyste ajuste à son contexte.
/// </summary>
public sealed class EvenementRedouteBibliotheque : IEntreeBibliotheque
{
    public Guid Id { get; private set; }
    public Guid? ProprietaireId { get; private set; }

    public string Intitule { get; private set; } = default!;

    /// <summary>Gravité indicative sur l'échelle EBIOS RM (1..4), ou null.</summary>
    public int? GraviteIndicative { get; private set; }

    /// <summary>Types d'impacts typiques (ex. « Financier, Juridique, Image »), texte libre.</summary>
    public string? ImpactsTypes { get; private set; }

    public DateTime CreeLeUtc { get; private set; }
    public bool EstSysteme => ProprietaireId is null;

    private EvenementRedouteBibliotheque() { }

    public static EvenementRedouteBibliotheque Creer(
        Guid proprietaireId, string intitule, int? graviteIndicative, string? impactsTypes)
    {
        if (string.IsNullOrWhiteSpace(intitule))
            throw new ArgumentException("L'intitulé de l'événement redouté est obligatoire.", nameof(intitule));
        if (graviteIndicative is not null && (graviteIndicative < EvenementRedoute.GraviteMin || graviteIndicative > EvenementRedoute.GraviteMax))
            throw new ArgumentOutOfRangeException(nameof(graviteIndicative), graviteIndicative,
                $"La gravité indicative doit être comprise entre {EvenementRedoute.GraviteMin} et {EvenementRedoute.GraviteMax}.");

        return new EvenementRedouteBibliotheque
        {
            Id = Guid.NewGuid(),
            ProprietaireId = proprietaireId,
            Intitule = intitule.Trim(),
            GraviteIndicative = graviteIndicative,
            ImpactsTypes = string.IsNullOrWhiteSpace(impactsTypes) ? null : impactsTypes.Trim(),
            CreeLeUtc = DateTime.UtcNow,
        };
    }

    public static EvenementRedouteBibliotheque Systeme(string cle, string intitule, int graviteIndicative, string impactsTypes)
        => new()
        {
            Id = MesureBibliotheque.IdDeterministe($"evenement-redoute:{cle}"),
            ProprietaireId = null,
            Intitule = intitule,
            GraviteIndicative = graviteIndicative,
            ImpactsTypes = impactsTypes,
            CreeLeUtc = default,
        };
}
