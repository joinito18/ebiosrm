using EbiosRM.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

/// <summary>
/// Copie complète d'une étude vers une nouvelle étude (duplication / "modèle").
///
/// Principe : chaque agrégat porte directement <c>EtudeId</c> et référence les
/// autres agrégats par ID nu (pas de vraie FK inter-agrégats, cf.
/// <see cref="ServiceSuppressionEtude"/>). Dupliquer revient donc à :
///   1. réserver un nouvel <c>Id</c> pour chaque agrégat racine (table de
///      correspondance ancien -> nouveau) ;
///   2. rattacher chaque agrégat détaché comme "Added", réécrire son <c>Id</c>,
///      son <c>EtudeId</c> et toutes ses clés étrangères via la table ;
///   3. régénérer les clés des entités owned (Referentiels, Mesures,
///      EvenementsIntermediaires, ModesOperatoires/ActionsElementaires...) et
///      remapper leurs FK vers d'autres agrégats.
///
/// Ce qui n'est PAS copié :
///   - les snapshots figés (déjà des rapports PDF dérivés, pas des données
///     sources -- même périmètre que l'export JSON) ;
///   - le journal d'audit et la liste des membres (propres à l'étude d'origine) ;
///   - les statuts d'atelier : la copie repart en brouillon sur les 5 ateliers
///     (une copie sans snapshot ne peut pas se prétendre "validée").
/// </summary>
public sealed class ServiceDuplicationEtude
{
    private readonly EbiosDbContext _db;

    public ServiceDuplicationEtude(EbiosDbContext db)
    {
        _db = db;
    }

    public async Task<Guid?> DupliquerAsync(Guid etudeSourceId, string? nouveauNom, Guid? proprietaireId, CancellationToken ct)
    {
        var source = await _db.Etudes.AsNoTracking().FirstOrDefaultAsync(e => e.Id == etudeSourceId, ct);
        if (source is null)
            return null;

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        var nom = string.IsNullOrWhiteSpace(nouveauNom) ? source.Nom + " (copie)" : nouveauNom.Trim();
        var nouvelleEtude = Etude.Creer(nom, source.Perimetre, source.Mission, proprietaireId);
        _db.Etudes.Add(nouvelleEtude);

        var map = new Dictionary<Guid, Guid> { [etudeSourceId] = nouvelleEtude.Id };

        var valeursMetier = await ChargerAsync(_db.ValeursMetier, etudeSourceId, ct);
        var biensSupport = await ChargerAsync(_db.BiensSupport, etudeSourceId, ct);
        var evenementsRedoutes = await ChargerAsync(_db.EvenementsRedoutes, etudeSourceId, ct);
        var socles = await ChargerAsync(_db.SoclesSecurite, etudeSourceId, ct);
        var couples = await ChargerAsync(_db.CouplesSrOv, etudeSourceId, ct);
        var partiesPrenantes = await ChargerAsync(_db.PartiesPrenantes, etudeSourceId, ct);
        var scenariosStrategiques = await ChargerAsync(_db.ScenariosStrategiques, etudeSourceId, ct);
        var cheminsAttaque = await ChargerAsync(_db.CheminsAttaque, etudeSourceId, ct);
        var scenariosOperationnels = await ChargerAsync(_db.ScenariosOperationnels, etudeSourceId, ct);
        var scenariosDeRisque = await ChargerAsync(_db.ScenariosDeRisque, etudeSourceId, ct);
        var plansTraitement = await ChargerAsync(_db.PlansTraitementRisque, etudeSourceId, ct);

        // Toutes les nouvelles clés racines sont réservées avant la moindre
        // réécriture : un chemin d'attaque référence un scénario stratégique et
        // une partie prenante qui peuvent être traités après lui.
        var tousAgregats = new IEnumerable<object>[]
        {
            valeursMetier, biensSupport, evenementsRedoutes, socles, couples, partiesPrenantes,
            scenariosStrategiques, cheminsAttaque, scenariosOperationnels, scenariosDeRisque, plansTraitement,
        }.SelectMany(liste => liste).ToList();

        foreach (var entite in tousAgregats)
            map[LireId(entite)] = Guid.NewGuid();

        foreach (var vm in valeursMetier) Rattacher(vm, map);
        foreach (var b in biensSupport) Rattacher(b, map, "ValeurMetierId");
        foreach (var er in evenementsRedoutes) Rattacher(er, map, "ValeurMetierId");
        foreach (var s in socles) Rattacher(s, map);
        foreach (var c in couples) Rattacher(c, map);
        foreach (var p in partiesPrenantes) Rattacher(p, map);
        foreach (var ss in scenariosStrategiques) Rattacher(ss, map, "CoupleSourceRisqueObjectifViseId", "EvenementRedouteId");
        foreach (var ca in cheminsAttaque) Rattacher(ca, map, "ScenarioStrategiqueId");
        foreach (var so in scenariosOperationnels) Rattacher(so, map, "CheminAttaqueId");
        foreach (var sr in scenariosDeRisque) Rattacher(sr, map, "CheminAttaqueId");
        foreach (var pt in plansTraitement) Rattacher(pt, map);

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return nouvelleEtude.Id;
    }

    private static async Task<List<T>> ChargerAsync<T>(DbSet<T> set, Guid etudeId, CancellationToken ct) where T : class
        => await set.AsNoTracking().Where(e => EF.Property<Guid>(e, "EtudeId") == etudeId).ToListAsync(ct);

    private Guid LireId(object entite) => (Guid)_db.Entry(entite).Property("Id").CurrentValue!;

    /// <summary>
    /// Rattache l'entité détachée comme "Added", réécrit son Id + son EtudeId +
    /// les clés étrangères nommées (toujours remappées via la table), puis
    /// régénère récursivement les clés de ses entités owned.
    /// </summary>
    private void Rattacher(object entite, Dictionary<Guid, Guid> map, params string[] clesEtrangeres)
    {
        _db.Add(entite);
        var entry = _db.Entry(entite);

        entry.Property("Id").CurrentValue = map[(Guid)entry.Property("Id").CurrentValue!];

        if (entry.Metadata.FindProperty("EtudeId") is not null)
            entry.Property("EtudeId").CurrentValue = Remap(map, (Guid)entry.Property("EtudeId").CurrentValue!);

        foreach (var nom in clesEtrangeres)
            entry.Property(nom).CurrentValue = Remap(map, (Guid)entry.Property(nom).CurrentValue!);

        RegenererOwned(entry, map);
    }

    private static Guid Remap(Dictionary<Guid, Guid> map, Guid ancienne)
        => map.TryGetValue(ancienne, out var nouvelle) ? nouvelle : ancienne;

    private void RegenererOwned(EntityEntry entry, Dictionary<Guid, Guid> map)
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

                foreach (var prop in new[] { "PartiePrenanteId", "BienSupportId" })
                {
                    if (enfantEntry.Metadata.FindProperty(prop) is not null)
                        enfantEntry.Property(prop).CurrentValue = Remap(map, (Guid)enfantEntry.Property(prop).CurrentValue!);
                }

                if (enfantEntry.Metadata.FindProperty("_scenariosDeRisqueIds") is not null
                    && enfantEntry.Property("_scenariosDeRisqueIds").CurrentValue is List<Guid> ids)
                {
                    enfantEntry.Property("_scenariosDeRisqueIds").CurrentValue = ids.Select(id => Remap(map, id)).ToList();
                }

                RegenererOwned(enfantEntry, map);
            }
        }
    }
}
