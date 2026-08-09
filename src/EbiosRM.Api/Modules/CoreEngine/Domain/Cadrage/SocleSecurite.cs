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

    private ReferentielApplicable() { }

    public static ReferentielApplicable Creer(string nom, EtatConformite etat)
    {
        if (string.IsNullOrWhiteSpace(nom))
            throw new ArgumentException("Le nom du référentiel est obligatoire.", nameof(nom));

        // Id volontairement non assigné ici : EF Core le génère à l'insertion.
        // Un Guid pré-assigné côté domaine ferait croire à EF Core que l'entité
        // existe déjà (UPDATE au lieu d'INSERT), causant un DbUpdateConcurrencyException.
        return new ReferentielApplicable
        {
            Nom = nom.Trim(),
            Etat = etat
        };
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

    public void AjouterReferentiel(string nom, EtatConformite etat)
    {
        _referentiels.Add(ReferentielApplicable.Creer(nom, etat));
    }
}
