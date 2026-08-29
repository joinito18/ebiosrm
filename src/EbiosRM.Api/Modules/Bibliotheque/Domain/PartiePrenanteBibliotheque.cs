using EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;

namespace EbiosRM.Api.Modules.Bibliotheque.Domain;

/// <summary>
/// Une partie prenante type de l'écosystème (Atelier 3), réutilisable d'une
/// étude à l'autre : infogéreur, hébergeur cloud, éditeur SaaS, mainteneur
/// industriel, autorité de tutelle... On ne mémorise que l'« identité » et des
/// niveaux <b>indicatifs</b> (dépendance / pénétration / maturité cyber /
/// confiance) : ils pré-remplissent l'évaluation de la dangerosité mais
/// l'analyste doit toujours les confirmer dans le contexte réel de son étude.
/// </summary>
public sealed class PartiePrenanteBibliotheque : IEntreeBibliotheque
{
    public Guid Id { get; private set; }
    public Guid? ProprietaireId { get; private set; }

    public string Nom { get; private set; } = default!;
    public CategoriePartiePrenante Categorie { get; private set; }
    public string? DescriptionCategorie { get; private set; }
    public string RolesEtAttentes { get; private set; } = default!;
    public string? Representant { get; private set; }

    /// <summary>Niveaux indicatifs sur l'échelle 1..4, ou null si non renseignés.</summary>
    public int? DependanceTypique { get; private set; }
    public int? PenetrationTypique { get; private set; }
    public int? MaturiteCyberTypique { get; private set; }
    public int? ConfianceTypique { get; private set; }

    public DateTime CreeLeUtc { get; private set; }
    public bool EstSysteme => ProprietaireId is null;

    private PartiePrenanteBibliotheque() { }

    private static int? BornerEchelle(int? v)
        => v is null ? null : Math.Clamp(v.Value, PartiePrenante.EchelleMin, PartiePrenante.EchelleMax);

    public static PartiePrenanteBibliotheque Creer(
        Guid proprietaireId, string nom, CategoriePartiePrenante categorie, string? descriptionCategorie,
        string rolesEtAttentes, string? representant,
        int? dependanceTypique, int? penetrationTypique, int? maturiteCyberTypique, int? confianceTypique)
    {
        if (string.IsNullOrWhiteSpace(nom))
            throw new ArgumentException("Le nom de la partie prenante est obligatoire.", nameof(nom));
        if (string.IsNullOrWhiteSpace(rolesEtAttentes))
            throw new ArgumentException("Les rôles et attentes sont obligatoires.", nameof(rolesEtAttentes));
        if (categorie == CategoriePartiePrenante.Autre && string.IsNullOrWhiteSpace(descriptionCategorie))
            throw new ArgumentException("Une description est obligatoire pour la catégorie 'Autre'.", nameof(descriptionCategorie));

        return new PartiePrenanteBibliotheque
        {
            Id = Guid.NewGuid(),
            ProprietaireId = proprietaireId,
            Nom = nom.Trim(),
            Categorie = categorie,
            DescriptionCategorie = string.IsNullOrWhiteSpace(descriptionCategorie) ? null : descriptionCategorie.Trim(),
            RolesEtAttentes = rolesEtAttentes.Trim(),
            Representant = string.IsNullOrWhiteSpace(representant) ? null : representant.Trim(),
            DependanceTypique = BornerEchelle(dependanceTypique),
            PenetrationTypique = BornerEchelle(penetrationTypique),
            MaturiteCyberTypique = BornerEchelle(maturiteCyberTypique),
            ConfianceTypique = BornerEchelle(confianceTypique),
            CreeLeUtc = DateTime.UtcNow,
        };
    }

    public static PartiePrenanteBibliotheque Systeme(
        string cle, string nom, CategoriePartiePrenante categorie, string? descriptionCategorie,
        string rolesEtAttentes, int dependance, int penetration, int maturiteCyber, int confiance)
        => new()
        {
            Id = MesureBibliotheque.IdDeterministe($"partie-prenante:{cle}"),
            ProprietaireId = null,
            Nom = nom,
            Categorie = categorie,
            DescriptionCategorie = descriptionCategorie,
            RolesEtAttentes = rolesEtAttentes,
            Representant = null,
            DependanceTypique = dependance,
            PenetrationTypique = penetration,
            MaturiteCyberTypique = maturiteCyber,
            ConfianceTypique = confiance,
            CreeLeUtc = default,
        };
}
