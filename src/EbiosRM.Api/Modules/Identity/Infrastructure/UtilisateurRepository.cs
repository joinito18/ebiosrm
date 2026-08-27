using EbiosRM.Api.Infrastructure.Persistence;
using EbiosRM.Api.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace EbiosRM.Api.Modules.Identity.Infrastructure;

public sealed class UtilisateurRepository : IUtilisateurRepository
{
    private readonly EbiosDbContext _db;

    public UtilisateurRepository(EbiosDbContext db)
    {
        _db = db;
    }

    public async Task AjouterAsync(Utilisateur utilisateur, CancellationToken cancellationToken)
    {
        _db.Utilisateurs.Add(utilisateur);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Utilisateur?> ObtenirParIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.Utilisateurs.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<Utilisateur?> ObtenirParEmailAsync(string email, CancellationToken cancellationToken)
    {
        return await _db.Utilisateurs.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<Utilisateur?> ObtenirParJetonReinitialisationHacheAsync(string jetonHache, CancellationToken cancellationToken)
    {
        return await _db.Utilisateurs.FirstOrDefaultAsync(u => u.JetonReinitialisationHache == jetonHache, cancellationToken);
    }

    public async Task MettreAJourAsync(Utilisateur utilisateur, CancellationToken cancellationToken)
    {
        _db.Utilisateurs.Update(utilisateur);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
