using EbiosRM.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

/// <summary>
/// Ré-attribue toutes les clés d'un graphe d'agrégats d'étude vers une nouvelle
/// étude : chaque agrégat porte directement <c>EtudeId</c> et référence les
/// autres par ID nu (pas de vraie FK inter-agrégats en base). Partagé par la
/// duplication (<see cref="ServiceDuplicationEtude"/>, source = base) et
/// l'import (<see cref="ServiceImportEtude"/>, source = fichier JSON).
///
/// Usage : <see cref="EnregistrerEtude"/>, puis <see cref="ReserverId"/> sur
/// chaque agrégat racine (réserve tous les nouveaux Id avant la moindre
/// réécriture -- un chemin d'attaque référence un scénario stratégique qui
/// peut être traité après lui), puis <see cref="Rattacher"/> sur chacun.
/// </summary>
internal sealed class RecableurClesEtude
{
    private readonly EbiosDbContext _db;
    private readonly bool _strict;
    private readonly Dictionary<Guid, Guid> _map = new();

    /// <param name="strict">
    /// true (import) : une référence introuvable dans la table de correspondance
    /// lève <see cref="ReferenceIntrouvableException"/> (fichier incohérent).
    /// false (duplication, tout vient de la même base) : la référence est
    /// laissée telle quelle.
    /// </param>
    public RecableurClesEtude(EbiosDbContext db, bool strict)
    {
        _db = db;
        _strict = strict;
    }

    public void EnregistrerEtude(Guid ancienneEtudeId, Guid nouvelleEtudeId) => _map[ancienneEtudeId] = nouvelleEtudeId;

    public void ReserverId(object entite) => _map[LireId(entite)] = Guid.NewGuid();

    /// <summary>
    /// Rattache l'entité comme "Added", réécrit son Id + son EtudeId + les clés
    /// étrangères nommées, puis régénère récursivement les clés de ses entités
    /// owned et remappe leurs FK (<c>PartiePrenanteId</c>, <c>BienSupportId</c>,
    /// <c>_scenariosDeRisqueIds</c>).
    /// </summary>
    public void Rattacher(object entite, params string[] clesEtrangeres)
    {
        _db.Add(entite);
        var entry = _db.Entry(entite);

        entry.Property("Id").CurrentValue = _map[(Guid)entry.Property("Id").CurrentValue!];

        if (entry.Metadata.FindProperty("EtudeId") is not null)
            entry.Property("EtudeId").CurrentValue = Remap("EtudeId", (Guid)entry.Property("EtudeId").CurrentValue!);

        foreach (var nom in clesEtrangeres)
            entry.Property(nom).CurrentValue = Remap(nom, (Guid)entry.Property(nom).CurrentValue!);

        RegenererOwned(entry);
    }

    private Guid LireId(object entite) => (Guid)_db.Entry(entite).Property("Id").CurrentValue!;

    private Guid Remap(string champ, Guid ancienne)
    {
        if (_map.TryGetValue(ancienne, out var nouvelle))
            return nouvelle;
        if (_strict)
            throw new ReferenceIntrouvableException(champ, ancienne);
        return ancienne;
    }

    private void RegenererOwned(EntityEntry entry)
    {
        foreach (var collection in entry.Collections)
        {
            if (!collection.Metadata.TargetEntityType.IsOwned() || collection.CurrentValue is null)
                continue;

            foreach (var enfant in collection.CurrentValue)
            {
                var enfantEntry = _db.Entry(enfant);

                if (enfantEntry.Metadata.FindProperty("Id") is not null)
                    enfantEntry.Property("Id").CurrentValue = Guid.NewGuid();

                // Références inter-agrégats d'une entité owned :
                //   EvenementIntermediaire.PartiePrenanteId -> PartiePrenante
                //   ActionElementaire.BienSupportId          -> BienSupport
                // Uniquement les vraies propriétés CLR : la FK vers le
                // propriétaire (shadow "PartiePrenanteId" de MesureEcosysteme...)
                // est gérée par EF à partir de la clé du propriétaire, déjà
                // réécrite.
                foreach (var prop in new[] { "PartiePrenanteId", "BienSupportId" })
                {
                    var metadata = enfantEntry.Metadata.FindProperty(prop);
                    if (metadata is not null && !metadata.IsShadowProperty())
                        enfantEntry.Property(prop).CurrentValue = Remap(prop, (Guid)enfantEntry.Property(prop).CurrentValue!);
                }

                if (enfantEntry.Metadata.FindProperty("_scenariosDeRisqueIds") is not null
                    && enfantEntry.Property("_scenariosDeRisqueIds").CurrentValue is List<Guid> ids)
                {
                    enfantEntry.Property("_scenariosDeRisqueIds").CurrentValue =
                        ids.Select(id => Remap("scenariosDeRisqueIds", id)).ToList();
                }

                RegenererOwned(enfantEntry);
            }
        }
    }
}

/// <summary>Une référence d'un fichier importé ne correspond à aucun élément du fichier.</summary>
public sealed class ReferenceIntrouvableException : Exception
{
    public ReferenceIntrouvableException(string champ, Guid valeur)
        : base($"Le fichier est incohérent : la référence {champ} = {valeur} ne correspond à aucun élément du fichier.")
    {
    }
}
