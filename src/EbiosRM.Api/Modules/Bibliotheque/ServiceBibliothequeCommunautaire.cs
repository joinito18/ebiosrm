using EbiosRM.Api.Infrastructure.Persistence;
using EbiosRM.Api.Modules.Bibliotheque.Domain;
using EbiosRM.Api.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace EbiosRM.Api.Modules.Bibliotheque;

/// <summary>
/// Bibliothèque communautaire : publier une entrée personnelle pour la rendre
/// visible de tous, en importer une dans sa propre bibliothèque, signaler une
/// entrée abusive (masquée automatiquement au-delà de
/// <see cref="PublicationBibliotheque.SeuilMasquage"/> signalements distincts).
/// </summary>
public sealed class ServiceBibliothequeCommunautaire
{
    /// <summary>Slugs de type acceptés dans les routes.</summary>
    public static readonly IReadOnlyList<string> Types = new[]
    {
        "mesure", "source-risque", "partie-prenante", "valeur-metier",
        "bien-support", "evenement-redoute", "mode-operatoire",
    };

    private readonly EbiosDbContext _db;
    private readonly IBibliothequeRepository _repo;
    private readonly IUtilisateurRepository _utilisateurs;

    public ServiceBibliothequeCommunautaire(EbiosDbContext db, IBibliothequeRepository repo, IUtilisateurRepository utilisateurs)
    {
        _db = db;
        _repo = repo;
        _utilisateurs = utilisateurs;
    }

    public sealed record Resultat(bool Ok, string? Erreur, Guid? EntiteId = null);

    public sealed record EntreeCommunautaire(
        Guid Id, string TypeEntite, Guid ProprietaireId, string ProprietaireNom,
        DateTime PublieLeUtc, int Signalements, bool PublieParMoi, object Entree);

    private static async Task<IEntreeBibliotheque?> Cast<T>(Task<T?> t) where T : class, IEntreeBibliotheque => await t;

    private Task<IEntreeBibliotheque?> ChargerAsync(string type, Guid id, CancellationToken ct) => type switch
    {
        "mesure" => Cast(_repo.ObtenirAsync<MesureBibliotheque>(id, ct)),
        "source-risque" => Cast(_repo.ObtenirAsync<SourceRisqueBibliotheque>(id, ct)),
        "partie-prenante" => Cast(_repo.ObtenirAsync<PartiePrenanteBibliotheque>(id, ct)),
        "valeur-metier" => Cast(_repo.ObtenirAsync<ValeurMetierBibliotheque>(id, ct)),
        "bien-support" => Cast(_repo.ObtenirAsync<BienSupportBibliotheque>(id, ct)),
        "evenement-redoute" => Cast(_repo.ObtenirAsync<EvenementRedouteBibliotheque>(id, ct)),
        "mode-operatoire" => Cast(_repo.ObtenirAsync<ModeOperatoireBibliotheque>(id, ct)),
        _ => Task.FromResult<IEntreeBibliotheque?>(null),
    };

    private async Task<List<IEntreeBibliotheque>> ChargerPlusieursAsync(string type, IReadOnlyCollection<Guid> ids, CancellationToken ct) => type switch
    {
        "mesure" => (await _repo.ListerParIdsAsync<MesureBibliotheque>(ids, ct)).Cast<IEntreeBibliotheque>().ToList(),
        "source-risque" => (await _repo.ListerParIdsAsync<SourceRisqueBibliotheque>(ids, ct)).Cast<IEntreeBibliotheque>().ToList(),
        "partie-prenante" => (await _repo.ListerParIdsAsync<PartiePrenanteBibliotheque>(ids, ct)).Cast<IEntreeBibliotheque>().ToList(),
        "valeur-metier" => (await _repo.ListerParIdsAsync<ValeurMetierBibliotheque>(ids, ct)).Cast<IEntreeBibliotheque>().ToList(),
        "bien-support" => (await _repo.ListerParIdsAsync<BienSupportBibliotheque>(ids, ct)).Cast<IEntreeBibliotheque>().ToList(),
        "evenement-redoute" => (await _repo.ListerParIdsAsync<EvenementRedouteBibliotheque>(ids, ct)).Cast<IEntreeBibliotheque>().ToList(),
        "mode-operatoire" => (await _repo.ListerParIdsAsync<ModeOperatoireBibliotheque>(ids, ct)).Cast<IEntreeBibliotheque>().ToList(),
        _ => new(),
    };

    public async Task<Resultat> PublierAsync(string type, Guid entiteId, Guid proprietaireId, CancellationToken ct)
    {
        if (!Types.Contains(type)) return new(false, "Type d'entrée inconnu.");

        var entree = await ChargerAsync(type, entiteId, ct);
        if (entree is null || entree.ProprietaireId != proprietaireId)
            return new(false, "Entrée introuvable dans votre bibliothèque.");

        var deja = await _db.PublicationsBibliotheque.FirstOrDefaultAsync(p => p.TypeEntite == type && p.EntiteId == entiteId, ct);
        if (deja is not null)
        {
            if (deja.Masquee) return new(false, "Cette entrée a été masquée à la suite de signalements.");
            return new(true, null, entiteId); // déjà publiée : idempotent
        }

        _db.PublicationsBibliotheque.Add(PublicationBibliotheque.Creer(type, entiteId, proprietaireId));
        await _db.SaveChangesAsync(ct);
        return new(true, null, entiteId);
    }

    public async Task<Resultat> RetirerAsync(string type, Guid entiteId, Guid proprietaireId, CancellationToken ct)
    {
        var pub = await _db.PublicationsBibliotheque.FirstOrDefaultAsync(p => p.TypeEntite == type && p.EntiteId == entiteId, ct);
        if (pub is null) return new(true, null); // rien à faire
        if (pub.ProprietaireId != proprietaireId) return new(false, "Vous n'êtes pas l'auteur de cette publication.");

        _db.PublicationsBibliotheque.Remove(pub);
        await _db.SaveChangesAsync(ct);
        return new(true, null);
    }

    public async Task<Resultat> ImporterAsync(string type, Guid entiteId, Guid nouveauProprietaireId, CancellationToken ct)
    {
        if (!Types.Contains(type)) return new(false, "Type d'entrée inconnu.");

        var pub = await _db.PublicationsBibliotheque.FirstOrDefaultAsync(p => p.TypeEntite == type && p.EntiteId == entiteId, ct);
        if (pub is null || pub.Masquee) return new(false, "Entrée communautaire introuvable.");

        var source = await ChargerAsync(type, entiteId, ct);
        if (source is null) return new(false, "Entrée communautaire introuvable.");

        var copie = source.CopiePrivee(nouveauProprietaireId);
        _db.Add(copie);
        await _db.SaveChangesAsync(ct);
        return new(true, null, copie.Id);
    }

    public async Task<Resultat> SignalerAsync(string type, Guid entiteId, Guid signalePar, string? motif, CancellationToken ct)
    {
        var pub = await _db.PublicationsBibliotheque
            .Include(p => p.Signalements)
            .FirstOrDefaultAsync(p => p.TypeEntite == type && p.EntiteId == entiteId, ct);
        if (pub is null) return new(false, "Entrée communautaire introuvable.");

        pub.Signaler(signalePar, motif);
        await _db.SaveChangesAsync(ct);
        return new(true, null);
    }

    public async Task<IReadOnlyList<EntreeCommunautaire>> ListerAsync(string type, Guid appelantId, string? q, CancellationToken ct)
    {
        if (!Types.Contains(type)) return Array.Empty<EntreeCommunautaire>();

        var pubs = await _db.PublicationsBibliotheque
            .Include(p => p.Signalements)
            .Where(p => p.TypeEntite == type && !p.Masquee)
            .OrderByDescending(p => p.PublieLeUtc)
            .ToListAsync(ct);
        if (pubs.Count == 0) return Array.Empty<EntreeCommunautaire>();

        var ids = pubs.Select(p => p.EntiteId).ToList();
        var entrees = (await ChargerPlusieursAsync(type, ids, ct)).ToDictionary(e => e.Id);

        var noms = new Dictionary<Guid, string>();
        foreach (var pid in pubs.Select(p => p.ProprietaireId).Distinct())
            noms[pid] = (await _utilisateurs.ObtenirParIdAsync(pid, ct))?.NomAffiche ?? "Compte supprimé";

        var resultat = new List<EntreeCommunautaire>();
        foreach (var p in pubs)
        {
            if (!entrees.TryGetValue(p.EntiteId, out var entree)) continue; // entrée supprimée entre-temps
            resultat.Add(new EntreeCommunautaire(
                p.EntiteId, type, p.ProprietaireId, noms[p.ProprietaireId],
                p.PublieLeUtc, p.Signalements.Count, p.ProprietaireId == appelantId, entree));
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var terme = q.Trim();
            resultat = resultat.Where(e => System.Text.Json.JsonSerializer.Serialize(e.Entree)
                .Contains(terme, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        return resultat;
    }

    /// <summary>Ids (par type) des entrées personnelles de l'appelant déjà publiées, pour afficher le bon bouton.</summary>
    public async Task<HashSet<Guid>> IdsPubliesAsync(Guid proprietaireId, CancellationToken ct)
        => (await _db.PublicationsBibliotheque
            .Where(p => p.ProprietaireId == proprietaireId)
            .Select(p => p.EntiteId)
            .ToListAsync(ct)).ToHashSet();
}
