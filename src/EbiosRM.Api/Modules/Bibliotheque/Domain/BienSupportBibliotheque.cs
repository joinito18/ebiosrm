using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

namespace EbiosRM.Api.Modules.Bibliotheque.Domain;

/// <summary>
/// Un bien support type (Atelier 1), réutilisable d'une étude à l'autre :
/// « Active Directory », « poste de travail bureautique », « ERP », « liaison
/// opérateur », « prestataire d'infogérance », « salle serveurs »... Porte le
/// type EBIOS RM (système d'information / réseau / ressources humaines / local)
/// et une entité propriétaire indicative.
/// </summary>
public sealed class BienSupportBibliotheque : IEntreeBibliotheque
{
    public Guid Id { get; private set; }
    public Guid? ProprietaireId { get; private set; }

    public string Intitule { get; private set; } = default!;
    public TypeBienSupport Type { get; private set; }
    public string? EntiteProprietaireTypique { get; private set; }
    public string? Description { get; private set; }

    public DateTime CreeLeUtc { get; private set; }
    public bool EstSysteme => ProprietaireId is null;

    private BienSupportBibliotheque() { }

    public static BienSupportBibliotheque Creer(
        Guid proprietaireId, string intitule, TypeBienSupport type, string? entiteProprietaireTypique, string? description)
    {
        if (string.IsNullOrWhiteSpace(intitule))
            throw new ArgumentException("L'intitulé du bien support est obligatoire.", nameof(intitule));

        return new BienSupportBibliotheque
        {
            Id = Guid.NewGuid(),
            ProprietaireId = proprietaireId,
            Intitule = intitule.Trim(),
            Type = type,
            EntiteProprietaireTypique = string.IsNullOrWhiteSpace(entiteProprietaireTypique) ? null : entiteProprietaireTypique.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            CreeLeUtc = DateTime.UtcNow,
        };
    }

    public static BienSupportBibliotheque Systeme(string cle, string intitule, TypeBienSupport type, string entiteProprietaireTypique, string? description = null)
        => new()
        {
            Id = MesureBibliotheque.IdDeterministe($"bien-support:{cle}"),
            ProprietaireId = null,
            Intitule = intitule,
            Type = type,
            EntiteProprietaireTypique = entiteProprietaireTypique,
            Description = description,
            CreeLeUtc = default,
        };

    public IEntreeBibliotheque CopiePrivee(Guid proprietaireId)
        => Creer(proprietaireId, Intitule, Type, EntiteProprietaireTypique, Description);
}
