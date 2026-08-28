namespace EbiosRM.Api.Modules.Audit.Domain;

/// <summary>
/// Une ligne du journal d'audit d'une étude : trace horodatée d'une écriture
/// (création, modification, suppression, validation d'atelier...) avec son
/// auteur. Le nom de l'auteur est dénormalisé pour survivre à la suppression
/// d'un compte -- une analyse de risque est un livrable opposable, sa
/// traçabilité ne doit pas dépendre de l'existence du compte.
/// </summary>
public sealed class EntreeJournal
{
    public Guid Id { get; private set; }
    public Guid EtudeId { get; private set; }
    public Guid? UtilisateurId { get; private set; }
    public string NomUtilisateur { get; private set; } = default!;
    public DateTime DateUtc { get; private set; }
    public string Action { get; private set; } = default!;
    public string Methode { get; private set; } = default!;
    public string Chemin { get; private set; } = default!;
    public int StatutHttp { get; private set; }

    private EntreeJournal() { }

    public static EntreeJournal Creer(
        Guid etudeId, Guid? utilisateurId, string nomUtilisateur,
        string action, string methode, string chemin, int statutHttp)
    {
        return new EntreeJournal
        {
            Id = Guid.NewGuid(),
            EtudeId = etudeId,
            UtilisateurId = utilisateurId,
            NomUtilisateur = string.IsNullOrWhiteSpace(nomUtilisateur) ? "inconnu" : nomUtilisateur.Trim(),
            DateUtc = DateTime.UtcNow,
            Action = action,
            Methode = methode,
            Chemin = chemin,
            StatutHttp = statutHttp,
        };
    }
}
