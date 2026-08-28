namespace EbiosRM.Api.Modules.Collaboration.Domain;

/// <summary>
/// Rôle d'un membre sur une étude. La séparation des tâches (ISO 27001 A.5.3)
/// est assurée non pas en bridant la validation d'atelier (simple point de
/// contrôle de l'analyste), mais par le journal d'audit et par les signataires
/// nommés de l'acceptation formelle du risque (Atelier 5).
/// </summary>
public enum RoleEtude
{
    /// <summary>Consultation + téléchargement des rapports.</summary>
    Lecteur,

    /// <summary>Tout le contenu des ateliers + valider / rouvrir les ateliers.</summary>
    Editeur,

    /// <summary>+ gérer les membres + supprimer l'étude + acceptation formelle.</summary>
    Proprietaire,
}

/// <summary>
/// Lie un utilisateur à une étude avec un rôle. Le créateur de l'étude est
/// automatiquement Propriétaire. Une étude a toujours au moins un Propriétaire.
/// </summary>
public sealed class EtudeMembre
{
    public Guid Id { get; private set; }
    public Guid EtudeId { get; private set; }
    public Guid UtilisateurId { get; private set; }
    public RoleEtude Role { get; private set; }
    public DateTime AjouteLeUtc { get; private set; }
    public Guid? AjoutePar { get; private set; }

    private EtudeMembre() { }

    public static EtudeMembre Creer(Guid etudeId, Guid utilisateurId, RoleEtude role, Guid? ajoutePar)
    {
        return new EtudeMembre
        {
            Id = Guid.NewGuid(),
            EtudeId = etudeId,
            UtilisateurId = utilisateurId,
            Role = role,
            AjouteLeUtc = DateTime.UtcNow,
            AjoutePar = ajoutePar,
        };
    }

    public void ChangerRole(RoleEtude role) => Role = role;
}
