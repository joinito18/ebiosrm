namespace EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

/// <summary>
/// Copie versionnée et immuable de l'état d'un atelier au moment de sa validation.
/// Conforme au principe P13 (versionnement de l'état de l'étude) et P16
/// (le Reporting ne lit jamais un brouillon, uniquement des snapshots figés).
/// Générique sur les 5 ateliers via NumeroAtelier -- conforme au diagramme de
/// classes de référence du projet (docs/architecture/uml-source/02-classes.puml),
/// qui prévoyait une seule classe SnapshotAtelier depuis le départ.
/// L'Id est laissé à EF Core (ValueGeneratedOnAdd) pour éviter le bug déjà rencontré
/// avec ReferentielApplicable (Guid attribué en factory => faux "existing entity").
/// </summary>
public class SnapshotAtelier
{
    public Guid Id { get; private set; }
    public Guid EtudeId { get; private set; }
    public int NumeroAtelier { get; private set; }
    public int Version { get; private set; }
    public DateTime DateCreationUtc { get; private set; }
    public string ContenuJson { get; private set; } = string.Empty;

    /// <summary>
    /// Libellé optionnel de la campagne (ex. « Revue annuelle 2026 »), saisi
    /// à la (re)validation. Sert à nommer les points de comparaison N / N-1
    /// dans le suivi de l'étude.
    /// </summary>
    public string? Libelle { get; private set; }

    private SnapshotAtelier() { }

    public static SnapshotAtelier Creer(Guid etudeId, int numeroAtelier, int version, string contenuJson, string? libelle = null)
    {
        if (etudeId == Guid.Empty)
            throw new ArgumentException("EtudeId invalide.", nameof(etudeId));
        if (numeroAtelier < 1 || numeroAtelier > 5)
            throw new ArgumentException("Le numéro d'atelier doit être compris entre 1 et 5.", nameof(numeroAtelier));
        if (version < 1)
            throw new ArgumentException("La version d'un snapshot doit être >= 1.", nameof(version));
        if (string.IsNullOrWhiteSpace(contenuJson))
            throw new ArgumentException("Le contenu du snapshot ne peut pas être vide.", nameof(contenuJson));

        return new SnapshotAtelier
        {
            EtudeId = etudeId,
            NumeroAtelier = numeroAtelier,
            Version = version,
            DateCreationUtc = DateTime.UtcNow,
            ContenuJson = contenuJson,
            Libelle = string.IsNullOrWhiteSpace(libelle) ? null : libelle.Trim(),
        };
    }
}
