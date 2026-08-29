using System.Security.Cryptography;
using System.Text;

namespace EbiosRM.Api.Modules.Bibliotheque.Domain;

/// <summary>
/// Référentiel d'origine d'une mesure de bibliothèque. <see cref="Libre"/> =
/// mesure saisie par l'utilisateur (ou remontée depuis une étude), sans
/// rattachement à un catalogue.
/// </summary>
public enum ReferentielMesure
{
    Libre,
    Iso27002,
    HygieneAnssi,
}

/// <summary>
/// Une mesure de sécurité réutilisable d'une étude à l'autre. Deux origines :
///   - <b>catalogue système</b> (<see cref="ProprietaireId"/> null) : ISO/IEC
///     27002:2022 et guide d'hygiène ANSSI, fournis d'office, non modifiables,
///     jamais en base -- construits en mémoire par <c>CatalogueSysteme</c> ;
///   - <b>bibliothèque personnelle</b> : mesures ajoutées par un utilisateur,
///     persistées, visibles de lui seul.
/// </summary>
public sealed class MesureBibliotheque
{
    public Guid Id { get; private set; }

    /// <summary>null = entrée du catalogue système.</summary>
    public Guid? ProprietaireId { get; private set; }

    public ReferentielMesure Referentiel { get; private set; }

    /// <summary>Code du référentiel (ex. « A.8.24 », « 3 »). Absent pour une mesure libre.</summary>
    public string? Code { get; private set; }

    public string Titre { get; private set; } = default!;
    public string? Description { get; private set; }

    /// <summary>Thème ISO 27002 / rubrique du guide d'hygiène / axe de traitement.</summary>
    public string? Categorie { get; private set; }

    public DateTime CreeLeUtc { get; private set; }

    public bool EstSysteme => ProprietaireId is null;

    private MesureBibliotheque() { }

    public static MesureBibliotheque Creer(
        Guid proprietaireId, ReferentielMesure referentiel, string? code, string titre, string? description, string? categorie)
    {
        if (string.IsNullOrWhiteSpace(titre))
            throw new ArgumentException("Le titre de la mesure est obligatoire.", nameof(titre));

        return new MesureBibliotheque
        {
            Id = Guid.NewGuid(),
            ProprietaireId = proprietaireId,
            Referentiel = referentiel,
            Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim(),
            Titre = titre.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Categorie = string.IsNullOrWhiteSpace(categorie) ? null : categorie.Trim(),
            CreeLeUtc = DateTime.UtcNow,
        };
    }

    /// <summary>Entrée du catalogue système : Id déterministe (stable entre requêtes et installations), jamais persistée.</summary>
    public static MesureBibliotheque Systeme(ReferentielMesure referentiel, string code, string titre, string categorie, string? description = null)
    {
        return new MesureBibliotheque
        {
            Id = IdDeterministe($"mesure:{referentiel}:{code}"),
            ProprietaireId = null,
            Referentiel = referentiel,
            Code = code,
            Titre = titre,
            Description = description,
            Categorie = categorie,
            CreeLeUtc = default,
        };
    }

    internal static Guid IdDeterministe(string cle) => new(MD5.HashData(Encoding.UTF8.GetBytes(cle)));
}
