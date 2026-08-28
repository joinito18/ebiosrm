using EbiosRM.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

/// <summary>
/// Purge complète d'une étude et de tout ce qui lui est rattaché. Contrairement
/// aux suppressions en cascade des endpoints unitaires (ex. couples-sr-ov, qui
/// doivent rejouer la chaîne de traçabilité à la main faute de vraie FK entre
/// agrégats indépendants), chaque table ici porte directement EtudeId : une
/// purge table par table suffit, pas besoin de parcourir le graphe. Les
/// entités owned (Referentiels, Mesures, EvenementsIntermediaires,
/// ModesOperatoires/ActionsElementaires...) ont une vraie contrainte
/// ON DELETE CASCADE en base (comportement par défaut d'EF Core pour les
/// relations OwnsMany) : purger la table propriétaire suffit à les emporter.
/// </summary>
public sealed class ServiceSuppressionEtude
{
    private readonly EbiosDbContext _db;

    public ServiceSuppressionEtude(EbiosDbContext db)
    {
        _db = db;
    }

    public async Task<bool> SupprimerAsync(Guid etudeId, CancellationToken cancellationToken)
    {
        var existe = await _db.Etudes.AnyAsync(e => e.Id == etudeId, cancellationToken);
        if (!existe)
            return false;

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        await _db.ValeursMetier.Where(v => v.EtudeId == etudeId).ExecuteDeleteAsync(cancellationToken);
        await _db.BiensSupport.Where(b => b.EtudeId == etudeId).ExecuteDeleteAsync(cancellationToken);
        await _db.EvenementsRedoutes.Where(e => e.EtudeId == etudeId).ExecuteDeleteAsync(cancellationToken);
        await _db.SoclesSecurite.Where(s => s.EtudeId == etudeId).ExecuteDeleteAsync(cancellationToken);
        await _db.CouplesSrOv.Where(c => c.EtudeId == etudeId).ExecuteDeleteAsync(cancellationToken);
        await _db.PartiesPrenantes.Where(p => p.EtudeId == etudeId).ExecuteDeleteAsync(cancellationToken);
        await _db.ScenariosStrategiques.Where(s => s.EtudeId == etudeId).ExecuteDeleteAsync(cancellationToken);
        await _db.CheminsAttaque.Where(c => c.EtudeId == etudeId).ExecuteDeleteAsync(cancellationToken);
        await _db.ScenariosOperationnels.Where(s => s.EtudeId == etudeId).ExecuteDeleteAsync(cancellationToken);
        await _db.ScenariosDeRisque.Where(s => s.EtudeId == etudeId).ExecuteDeleteAsync(cancellationToken);
        await _db.PlansTraitementRisque.Where(p => p.EtudeId == etudeId).ExecuteDeleteAsync(cancellationToken);
        await _db.SnapshotsAtelier.Where(s => s.EtudeId == etudeId).ExecuteDeleteAsync(cancellationToken);
        await _db.JournalAudit.Where(j => j.EtudeId == etudeId).ExecuteDeleteAsync(cancellationToken);
        await _db.Etudes.Where(e => e.Id == etudeId).ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
