namespace EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

/// <summary>
/// Copie versionnée et immuable de l'état de l'Atelier 1 au moment de sa validation.
/// Conforme au principe P13 (versionnement de l'état de l'étude) et P16
/// (le Reporting ne lit jamais un brouillon, uniquement des snapshots figés).
/// L'Id est laissé à EF Core (ValueGeneratedOnAdd) pour éviter le bug déjà rencontré
/// avec ReferentielApplicable (Guid attribué en factory => faux "existing entity").
/// </summary>
public class SnapshotAtelier1
{
    public Guid Id { get; private set; }
    public Guid EtudeId { get; private set; }
    public int Version { get; private set; }
    public DateTime DateCreationUtc { get; private set; }
    public string ContenuJson { get; private set; } = string.Empty;

    private SnapshotAtelier1() { }

    public static SnapshotAtelier1 Creer(Guid etudeId, int version, string contenuJson)
    {
        if (etudeId == Guid.Empty)
            throw new ArgumentException("EtudeId invalide.", nameof(etudeId));
        if (version < 1)
            throw new ArgumentException("La version d'un snapshot doit être >= 1.", nameof(version));
        if (string.IsNullOrWhiteSpace(contenuJson))
            throw new ArgumentException("Le contenu du snapshot ne peut pas être vide.", nameof(contenuJson));

        return new SnapshotAtelier1
        {
            EtudeId = etudeId,
            Version = version,
            DateCreationUtc = DateTime.UtcNow,
            ContenuJson = contenuJson
        };
    }
}
