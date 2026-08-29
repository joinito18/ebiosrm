namespace EbiosRM.Api.Modules.Bibliotheque.Domain;

/// <summary>
/// Contrat commun à toutes les entrées de bibliothèque (mesures, sources de
/// risque, parties prenantes, valeurs métier, biens support, événements
/// redoutés, modes opératoires). Permet au dépôt d'être générique plutôt que
/// d'avoir un quadruplet de méthodes par type.
///
/// Deux origines, comme <see cref="MesureBibliotheque"/> l'a introduit :
///   - <b>catalogue système</b> : <see cref="ProprietaireId"/> null, Id
///     déterministe, jamais persisté (construit en mémoire par
///     <see cref="CatalogueSysteme"/>) ;
///   - <b>bibliothèque personnelle</b> : ajouté par un utilisateur, persisté,
///     visible de lui seul (jusqu'à publication communautaire, cf. étape 3).
/// </summary>
public interface IEntreeBibliotheque
{
    Guid Id { get; }

    /// <summary>null = entrée du catalogue système, non modifiable.</summary>
    Guid? ProprietaireId { get; }

    DateTime CreeLeUtc { get; }

    bool EstSysteme { get; }

    /// <summary>
    /// Renvoie une copie privée de cette entrée pour le compte
    /// <paramref name="proprietaireId"/> (nouvel Id, non publiée). Sert à
    /// « importer » une entrée de la bibliothèque communautaire dans sa
    /// propre bibliothèque.
    /// </summary>
    IEntreeBibliotheque CopiePrivee(Guid proprietaireId);
}
