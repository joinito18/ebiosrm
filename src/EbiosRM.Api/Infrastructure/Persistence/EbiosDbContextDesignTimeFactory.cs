using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EbiosRM.Api.Infrastructure.Persistence;

/// <summary>
/// Utilisé uniquement par les outils <c>dotnet ef</c> (migrations). Force
/// PostgreSQL : les migrations de ce projet sont PostgreSQL. Le mode bureau
/// (SQLite) crée son schéma via <c>EnsureCreated</c>, sans migration.
/// </summary>
public sealed class EbiosDbContextDesignTimeFactory : IDesignTimeDbContextFactory<EbiosDbContext>
{
    public EbiosDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<EbiosDbContext>()
            .UseNpgsql("Host=localhost;Port=5433;Database=ebiosrm;Username=ebiosrm;Password=ebiosrm_dev")
            .Options;
        return new EbiosDbContext(options);
    }
}
