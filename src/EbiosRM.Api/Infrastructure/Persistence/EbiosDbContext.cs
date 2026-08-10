using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;
using Microsoft.EntityFrameworkCore;

namespace EbiosRM.Api.Infrastructure.Persistence;

public class EbiosDbContext : DbContext
{
    public EbiosDbContext(DbContextOptions<EbiosDbContext> options)
        : base(options)
    {
    }

    public DbSet<Etude> Etudes => Set<Etude>();
    public DbSet<ValeurMetier> ValeursMetier => Set<ValeurMetier>();
    public DbSet<BienSupport> BiensSupport => Set<BienSupport>();
    public DbSet<EvenementRedoute> EvenementsRedoutes => Set<EvenementRedoute>();
    public DbSet<SocleSecurite> SoclesSecurite => Set<SocleSecurite>();
    public DbSet<SnapshotAtelier1> SnapshotsAtelier1 => Set<SnapshotAtelier1>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("core_engine");

        modelBuilder.Entity<Etude>(entity =>
        {
            entity.ToTable("etudes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nom).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Perimetre).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.VersionReferentielId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Statut).IsRequired().HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.CreeLeUtc).IsRequired();
        });

        modelBuilder.Entity<ValeurMetier>(entity =>
        {
            entity.ToTable("valeurs_metier");
            entity.HasKey(v => v.Id);
            entity.Property(v => v.EtudeId).IsRequired();
            entity.Property(v => v.Description).IsRequired().HasMaxLength(1000);
            entity.Property(v => v.EntiteResponsable).IsRequired().HasMaxLength(200);
            entity.Property(v => v.CreeLeUtc).IsRequired();
            entity.HasIndex(v => v.EtudeId);
        });

        modelBuilder.Entity<BienSupport>(entity =>
        {
            entity.ToTable("biens_support");
            entity.HasKey(b => b.Id);
            entity.Property(b => b.EtudeId).IsRequired();
            entity.Property(b => b.ValeurMetierId).IsRequired();
            entity.Property(b => b.Description).IsRequired().HasMaxLength(1000);
            entity.Property(b => b.Type).IsRequired().HasConversion<string>().HasMaxLength(50);
            entity.Property(b => b.EntiteResponsable).IsRequired().HasMaxLength(200);
            entity.Property(b => b.CreeLeUtc).IsRequired();
            entity.HasIndex(b => b.EtudeId);
            entity.HasIndex(b => b.ValeurMetierId);
        });

        modelBuilder.Entity<EvenementRedoute>(entity =>
        {
            entity.ToTable("evenements_redoutes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EtudeId).IsRequired();
            entity.Property(e => e.ValeurMetierId).IsRequired();
            entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Gravite).IsRequired();
            entity.Property(e => e.CreeLeUtc).IsRequired();
            entity.HasIndex(e => e.EtudeId);
            entity.HasIndex(e => e.ValeurMetierId);
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_evenements_redoutes_gravite",
                $"\"Gravite\" >= {EvenementRedoute.GraviteMin} AND \"Gravite\" <= {EvenementRedoute.GraviteMax}"));
        });

        modelBuilder.Entity<SocleSecurite>(entity =>
        {
            entity.ToTable("socles_securite");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.EtudeId).IsRequired();
            entity.HasIndex(s => s.EtudeId).IsUnique();

            entity.OwnsMany(s => s.Referentiels, referentiel =>
            {
                referentiel.ToTable("referentiels_applicables");
                referentiel.WithOwner().HasForeignKey("SocleSecuriteId");
                referentiel.HasKey(r => r.Id);
                referentiel.Property(r => r.Id).ValueGeneratedOnAdd();
                referentiel.Property(r => r.Nom).IsRequired().HasMaxLength(300);
                referentiel.Property(r => r.Etat).IsRequired().HasConversion<string>().HasMaxLength(50);
            });
        });

        modelBuilder.Entity<SnapshotAtelier1>(entity =>
        {
            entity.ToTable("snapshots_atelier1");
            entity.HasKey(s => s.Id);

            // Id non assigné dans la factory métier (SnapshotAtelier1.Creer) --
            // même précaution que ReferentielApplicable, EF Core génère la clé
            // à l'insertion pour éviter un DbUpdateConcurrencyException.
            entity.Property(s => s.Id).ValueGeneratedOnAdd();

            entity.Property(s => s.EtudeId).IsRequired();
            entity.Property(s => s.Version).IsRequired();
            entity.Property(s => s.DateCreationUtc).IsRequired();
            entity.Property(s => s.ContenuJson).IsRequired().HasColumnType("jsonb");

            // Une seule version donnée par étude : jamais deux snapshots
            // avec le même (EtudeId, Version). Garantit l'immuabilité/l'ordre.
            entity.HasIndex(s => new { s.EtudeId, s.Version }).IsUnique();
        });

        base.OnModelCreating(modelBuilder);
    }
}
