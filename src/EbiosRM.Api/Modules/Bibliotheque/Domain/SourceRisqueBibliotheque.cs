using EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;

namespace EbiosRM.Api.Modules.Bibliotheque.Domain;

/// <summary>
/// Un couple « source de risque / objectif visé » réutilisable d'une étude à
/// l'autre (Atelier 2). Même double origine que <see cref="MesureBibliotheque"/> :
/// catalogue système (base type ANSSI, <see cref="ProprietaireId"/> null, non
/// persisté) ou bibliothèque personnelle.
///
/// Ne contient que la partie « identité » du couple (catégories + descriptions
/// + thème + motivation/ressources typiques). Le contexte de vulnérabilité
/// reste propre à chaque étude et n'est pas mémorisé ici.
/// </summary>
public sealed class SourceRisqueBibliotheque : IEntreeBibliotheque
{
    public Guid Id { get; private set; }

    /// <summary>null = entrée du catalogue système.</summary>
    public Guid? ProprietaireId { get; private set; }

    public CategorieSourceRisque SourceRisque { get; private set; }
    public string DescriptionSourceRisque { get; private set; } = default!;
    public CategorieObjectifVise ObjectifVise { get; private set; }
    public string DescriptionObjectifVise { get; private set; } = default!;
    public string? Theme { get; private set; }
    public int? MotivationTypique { get; private set; }
    public int? RessourcesTypiques { get; private set; }
    public DateTime CreeLeUtc { get; private set; }

    public bool EstSysteme => ProprietaireId is null;

    private SourceRisqueBibliotheque() { }

    public static SourceRisqueBibliotheque Creer(
        Guid proprietaireId,
        CategorieSourceRisque sourceRisque, string descriptionSourceRisque,
        CategorieObjectifVise objectifVise, string descriptionObjectifVise,
        string? theme, int? motivationTypique, int? ressourcesTypiques)
    {
        if (string.IsNullOrWhiteSpace(descriptionSourceRisque))
            throw new ArgumentException("La description de la source de risque est obligatoire.", nameof(descriptionSourceRisque));
        if (string.IsNullOrWhiteSpace(descriptionObjectifVise))
            throw new ArgumentException("La description de l'objectif visé est obligatoire.", nameof(descriptionObjectifVise));

        return new SourceRisqueBibliotheque
        {
            Id = Guid.NewGuid(),
            ProprietaireId = proprietaireId,
            SourceRisque = sourceRisque,
            DescriptionSourceRisque = descriptionSourceRisque.Trim(),
            ObjectifVise = objectifVise,
            DescriptionObjectifVise = descriptionObjectifVise.Trim(),
            Theme = string.IsNullOrWhiteSpace(theme) ? null : theme.Trim(),
            MotivationTypique = motivationTypique,
            RessourcesTypiques = ressourcesTypiques,
            CreeLeUtc = DateTime.UtcNow,
        };
    }

    public static SourceRisqueBibliotheque Systeme(
        string cle,
        CategorieSourceRisque sourceRisque, string descriptionSourceRisque,
        CategorieObjectifVise objectifVise, string descriptionObjectifVise,
        string theme, int motivationTypique, int ressourcesTypiques)
    {
        return new SourceRisqueBibliotheque
        {
            Id = MesureBibliotheque.IdDeterministe($"source-risque:{cle}"),
            ProprietaireId = null,
            SourceRisque = sourceRisque,
            DescriptionSourceRisque = descriptionSourceRisque,
            ObjectifVise = objectifVise,
            DescriptionObjectifVise = descriptionObjectifVise,
            Theme = theme,
            MotivationTypique = motivationTypique,
            RessourcesTypiques = ressourcesTypiques,
            CreeLeUtc = default,
        };
    }
}
