namespace EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

public enum EtatConformite
{
    Conforme,
    NonConforme,
    NonApplicable
}

public sealed class ReferentielApplicable
{
    public Guid Id { get; private set; }
    public string Nom { get; private set; } = default!;
    public EtatConformite Etat { get; private set; }

    // Optionnels : renseignes quand le referentiel correspond a un controle
    // officiel de l'Annexe A ISO/IEC 27001:2022 (catalogue fige cote frontend).
    // Restent null pour un referentiel libre (ex. PSSI, RGPD).
    public string? Theme { get; private set; }
    public string? CodeControle { get; private set; }

    // Description libre de l'etat reel observe (ex. "Supports amovibles non
    // chiffres", "Architecture 3 couches, 20 VLAN, DMZ, Pare-feu Cisco ASA").
    // Distinct du statut Etat (Conforme/NonConforme/NonApplicable) : Etat
    // reste la valeur structuree utilisee pour la couleur/le tri/les futurs
    // calculs de taux de conformite ; EtatActuel est du texte libre factuel.
    public string? EtatActuel { get; private set; }

    private ReferentielApplicable() { }

    public static ReferentielApplicable Creer(string nom, EtatConformite etat, string? theme = null, string? codeControle = null, string? etatActuel = null)
    {
        if (string.IsNullOrWhiteSpace(nom))
            throw new ArgumentException("Le nom du référentiel est obligatoire.", nameof(nom));

        // Id volontairement non assigné ici : EF Core le génère à l'insertion.
        return new ReferentielApplicable
        {
            Nom = nom.Trim(),
            Etat = etat,
            Theme = string.IsNullOrWhiteSpace(theme) ? null : theme.Trim(),
            CodeControle = string.IsNullOrWhiteSpace(codeControle) ? null : codeControle.Trim(),
            EtatActuel = string.IsNullOrWhiteSpace(etatActuel) ? null : etatActuel.Trim()
        };
    }

    public void Modifier(string nom, EtatConformite etat, string? theme = null, string? codeControle = null, string? etatActuel = null)
    {
        if (string.IsNullOrWhiteSpace(nom))
            throw new ArgumentException("Le nom du référentiel est obligatoire.", nameof(nom));

        Nom = nom.Trim();
        Etat = etat;
        Theme = string.IsNullOrWhiteSpace(theme) ? null : theme.Trim();
        CodeControle = string.IsNullOrWhiteSpace(codeControle) ? null : codeControle.Trim();
        EtatActuel = string.IsNullOrWhiteSpace(etatActuel) ? null : etatActuel.Trim();
    }
}

public sealed class SocleSecurite
{
    public Guid Id { get; private set; }
    public Guid EtudeId { get; private set; }
    private readonly List<ReferentielApplicable> _referentiels = new();
    public IReadOnlyList<ReferentielApplicable> Referentiels => _referentiels;

    private SocleSecurite() { }

    public static SocleSecurite Creer(Guid etudeId)
    {
        if (etudeId == Guid.Empty)
            throw new ArgumentException("Le socle de sécurité doit être rattaché à une étude.", nameof(etudeId));

        return new SocleSecurite { Id = Guid.NewGuid(), EtudeId = etudeId };
    }

    public void AjouterReferentiel(string nom, EtatConformite etat, string? theme = null, string? codeControle = null, string? etatActuel = null)
    {
        _referentiels.Add(ReferentielApplicable.Creer(nom, etat, theme, codeControle, etatActuel));
    }

    public void ModifierReferentiel(Guid referentielId, string nom, EtatConformite etat, string? theme = null, string? codeControle = null, string? etatActuel = null)
    {
        var referentiel = _referentiels.FirstOrDefault(r => r.Id == referentielId);
        if (referentiel is null)
            throw new ArgumentException("Référentiel introuvable dans ce socle de sécurité.", nameof(referentielId));

        referentiel.Modifier(nom, etat, theme, codeControle, etatActuel);
    }

    public void SupprimerReferentiel(Guid referentielId)
    {
        var referentiel = _referentiels.FirstOrDefault(r => r.Id == referentielId);
        if (referentiel is null)
            throw new ArgumentException("Référentiel introuvable dans ce socle de sécurité.", nameof(referentielId));

        _referentiels.Remove(referentiel);
    }
}
