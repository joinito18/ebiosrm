using EbiosRM.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

/// <summary>
/// Copie complète d'une étude vers une nouvelle étude (duplication / "modèle").
/// Recharge chaque agrégat détaché et délègue la ré-attribution des clés à
/// <see cref="RecableurClesEtude"/>.
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

        var recableur = new RecableurClesEtude(_db, strict: false);
        recableur.EnregistrerEtude(etudeSourceId, nouvelleEtude.Id);

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

        foreach (var entite in new IEnumerable<object>[]
        {
            valeursMetier, biensSupport, evenementsRedoutes, socles, couples, partiesPrenantes,
            scenariosStrategiques, cheminsAttaque, scenariosOperationnels, scenariosDeRisque, plansTraitement,
        }.SelectMany(liste => liste))
        {
            recableur.ReserverId(entite);
        }

        foreach (var vm in valeursMetier) recableur.Rattacher(vm);
        foreach (var b in biensSupport) recableur.Rattacher(b, "ValeurMetierId");
        foreach (var er in evenementsRedoutes) recableur.Rattacher(er, "ValeurMetierId");
        foreach (var s in socles) recableur.Rattacher(s);
        foreach (var c in couples) recableur.Rattacher(c);
        foreach (var p in partiesPrenantes) recableur.Rattacher(p);
        foreach (var ss in scenariosStrategiques) recableur.Rattacher(ss, "CoupleSourceRisqueObjectifViseId", "EvenementRedouteId");
        foreach (var ca in cheminsAttaque) recableur.Rattacher(ca, "ScenarioStrategiqueId");
        foreach (var so in scenariosOperationnels) recableur.Rattacher(so, "CheminAttaqueId");
        foreach (var sr in scenariosDeRisque) recableur.Rattacher(sr, "CheminAttaqueId");
        foreach (var pt in plansTraitement) recableur.Rattacher(pt);

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return nouvelleEtude.Id;
    }

    private static async Task<List<T>> ChargerAsync<T>(DbSet<T> set, Guid etudeId, CancellationToken ct) where T : class
        => await set.AsNoTracking().Where(e => EF.Property<Guid>(e, "EtudeId") == etudeId).ToListAsync(ct);
}
