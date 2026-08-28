using EbiosRM.Api.Infrastructure.Persistence;
using EbiosRM.Api.Modules.Collaboration.Domain;
using Microsoft.EntityFrameworkCore;

namespace EbiosRM.Api.Modules.Collaboration.Infrastructure;

public sealed class EtudeMembreRepository : IEtudeMembreRepository
{
    private readonly EbiosDbContext _db;

    public EtudeMembreRepository(EbiosDbContext db)
    {
        _db = db;
    }

    public Task<EtudeMembre?> ObtenirAsync(Guid etudeId, Guid utilisateurId, CancellationToken cancellationToken) =>
        _db.EtudeMembres.FirstOrDefaultAsync(m => m.EtudeId == etudeId && m.UtilisateurId == utilisateurId, cancellationToken);

    public async Task<List<EtudeMembre>> ListerParEtudeAsync(Guid etudeId, CancellationToken cancellationToken) =>
        await _db.EtudeMembres.Where(m => m.EtudeId == etudeId).OrderBy(m => m.AjouteLeUtc).ToListAsync(cancellationToken);

    public async Task<List<EtudeMembre>> ListerParUtilisateurAsync(Guid utilisateurId, CancellationToken cancellationToken) =>
        await _db.EtudeMembres.Where(m => m.UtilisateurId == utilisateurId).ToListAsync(cancellationToken);

    public Task<int> CompterProprietairesAsync(Guid etudeId, CancellationToken cancellationToken) =>
        _db.EtudeMembres.CountAsync(m => m.EtudeId == etudeId && m.Role == RoleEtude.Proprietaire, cancellationToken);

    public async Task AjouterAsync(EtudeMembre membre, CancellationToken cancellationToken)
    {
        _db.EtudeMembres.Add(membre);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task MettreAJourAsync(EtudeMembre membre, CancellationToken cancellationToken)
    {
        _db.EtudeMembres.Update(membre);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SupprimerAsync(EtudeMembre membre, CancellationToken cancellationToken)
    {
        _db.EtudeMembres.Remove(membre);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
