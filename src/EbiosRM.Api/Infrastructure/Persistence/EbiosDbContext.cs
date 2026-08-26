using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;
using EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;
using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;
using EbiosRM.Api.Modules.Identity.Domain;
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
    public DbSet<SnapshotAtelier> SnapshotsAtelier => Set<SnapshotAtelier>();
    public DbSet<CoupleSourceRisqueObjectifVise> CouplesSrOv => Set<CoupleSourceRisqueObjectifVise>();
    public DbSet<PartiePrenante> PartiesPrenantes => Set<PartiePrenante>();
    public DbSet<ScenarioStrategique> ScenariosStrategiques => Set<ScenarioStrategique>();
    public DbSet<CheminAttaque> CheminsAttaque => Set<CheminAttaque>();
    public DbSet<ScenarioOperationnel> ScenariosOperationnels => Set<ScenarioOperationnel>();
    public DbSet<ScenarioDeRisque> ScenariosDeRisque => Set<ScenarioDeRisque>();
    public DbSet<PlanTraitementRisque> PlansTraitementRisque => Set<PlanTraitementRisque>();
    public DbSet<Utilisateur> Utilisateurs => Set<Utilisateur>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("core_engine");

        modelBuilder.Entity<Etude>(entity =>
        {
            entity.ToTable("etudes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nom).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Perimetre).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Mission).IsRequired().HasMaxLength(500);
            entity.Property(e => e.VersionReferentielId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Statut).IsRequired().HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.StatutAtelier2).IsRequired().HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.StatutAtelier3).IsRequired().HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.StatutAtelier4).IsRequired().HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.StatutAtelier5).IsRequired().HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.CreeLeUtc).IsRequired();
        });

        modelBuilder.Entity<ValeurMetier>(entity =>
        {
            entity.ToTable("valeurs_metier");
            entity.HasKey(v => v.Id);
            entity.Property(v => v.EtudeId).IsRequired();
            entity.Property(v => v.Description).IsRequired().HasMaxLength(1000);
            entity.Property(v => v.EntiteProprietaire).IsRequired().HasMaxLength(200);
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
            entity.Property(b => b.EntiteProprietaire).IsRequired().HasMaxLength(200);
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
                referentiel.Property(r => r.Theme).HasMaxLength(100);
                referentiel.Property(r => r.CodeControle).HasMaxLength(20);
                referentiel.Property(r => r.EtatActuel).HasMaxLength(2000);
            });
        });

        modelBuilder.Entity<SnapshotAtelier>(entity =>
        {
            entity.ToTable("snapshots_atelier");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Id).ValueGeneratedOnAdd();
            entity.Property(s => s.EtudeId).IsRequired();
            entity.Property(s => s.NumeroAtelier).IsRequired();
            entity.Property(s => s.Version).IsRequired();
            entity.Property(s => s.DateCreationUtc).IsRequired();
            entity.Property(s => s.ContenuJson).IsRequired().HasColumnType("jsonb");
            entity.HasIndex(s => new { s.EtudeId, s.NumeroAtelier, s.Version }).IsUnique();
        });

        modelBuilder.Entity<CoupleSourceRisqueObjectifVise>(entity =>
        {
            entity.ToTable("couples_sr_ov");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.EtudeId).IsRequired();
            entity.Property(c => c.SourceRisque).IsRequired().HasConversion<string>().HasMaxLength(50);
            entity.Property(c => c.DescriptionSourceRisque).IsRequired().HasMaxLength(500);
            entity.Property(c => c.ObjectifVise).IsRequired().HasConversion<string>().HasMaxLength(50);
            entity.Property(c => c.DescriptionObjectifVise).IsRequired().HasMaxLength(500);
            entity.Property(c => c.ContexteVulnerabilite).IsRequired().HasMaxLength(2000);
            entity.Property(c => c.Theme).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Motivation).IsRequired();
            entity.Property(c => c.Ressources).IsRequired();
            entity.Property(c => c.PertinenceCalculee).IsRequired().HasConversion<string>().HasMaxLength(50);
            entity.Property(c => c.PertinenceRetenue).HasConversion<string>().HasMaxLength(50);
            entity.Property(c => c.JustificationPertinence).HasMaxLength(2000);
            entity.Ignore(c => c.Pertinence);
            entity.Property(c => c.CreeLeUtc).IsRequired();
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_couples_sr_ov_motivation",
                $"\"Motivation\" >= {CoupleSourceRisqueObjectifVise.EchelleMin} AND \"Motivation\" <= {CoupleSourceRisqueObjectifVise.EchelleMax}"));
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_couples_sr_ov_ressources",
                $"\"Ressources\" >= {CoupleSourceRisqueObjectifVise.EchelleMin} AND \"Ressources\" <= {CoupleSourceRisqueObjectifVise.EchelleMax}"));
            entity.HasIndex(c => c.EtudeId);
            entity.HasIndex(c => c.Theme);
        });

        modelBuilder.Entity<PartiePrenante>(entity =>
        {
            entity.ToTable("parties_prenantes");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.EtudeId).IsRequired();
            entity.Property(p => p.Nom).IsRequired().HasMaxLength(300);
            entity.Property(p => p.RolesEtAttentes).IsRequired().HasMaxLength(1000);
            entity.Property(p => p.Representant).IsRequired().HasMaxLength(300);
            entity.Property(p => p.Categorie).IsRequired().HasConversion<string>().HasMaxLength(50);
            entity.Property(p => p.DescriptionCategorie).HasMaxLength(500);
            entity.Property(p => p.CreeLeUtc).IsRequired();
            entity.Property(p => p.JustificationDangerosite).HasMaxLength(2000);
            entity.Property(p => p.JustificationDangerositeResiduelle).HasMaxLength(2000);
            entity.Ignore(p => p.Zone);
            entity.Ignore(p => p.ZoneResiduelle);
            entity.Ignore(p => p.NiveauDangerosite);
            entity.Ignore(p => p.NiveauDangerositeResiduel);
            entity.HasIndex(p => p.EtudeId);

            entity.OwnsMany(p => p.Mesures, mesure =>
            {
                mesure.ToTable("mesures_ecosysteme");
                mesure.WithOwner().HasForeignKey("PartiePrenanteId");
                mesure.HasKey(m => m.Id);
                mesure.Property(m => m.Id).ValueGeneratedOnAdd();
                mesure.Property(m => m.Description).IsRequired().HasMaxLength(2000);
                mesure.Property(m => m.CreeLeUtc).IsRequired();
            });
        });

        modelBuilder.Entity<ScenarioStrategique>(entity =>
        {
            entity.ToTable("scenarios_strategiques");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.EtudeId).IsRequired();
            entity.Property(s => s.CoupleSourceRisqueObjectifViseId).IsRequired();
            entity.Property(s => s.EvenementRedouteId).IsRequired();
            entity.Property(s => s.Description).IsRequired().HasMaxLength(2000);
            entity.Property(s => s.CreeLeUtc).IsRequired();
            entity.HasIndex(s => s.EtudeId);
            entity.HasIndex(s => s.CoupleSourceRisqueObjectifViseId).IsUnique();
        });

        modelBuilder.Entity<CheminAttaque>(entity =>
        {
            entity.ToTable("chemins_attaque");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.EtudeId).IsRequired();
            entity.Property(c => c.ScenarioStrategiqueId).IsRequired();
            entity.Property(c => c.Description).IsRequired().HasMaxLength(2000);
            entity.Property(c => c.CreeLeUtc).IsRequired();
            entity.HasIndex(c => c.EtudeId);
            entity.HasIndex(c => c.ScenarioStrategiqueId);

            entity.OwnsMany(c => c.EvenementsIntermediaires, ei =>
            {
                ei.ToTable("evenements_intermediaires");
                ei.WithOwner().HasForeignKey("CheminAttaqueId");
                ei.HasKey(e => e.Id);
                ei.Property(e => e.Id).ValueGeneratedOnAdd();
                ei.Property(e => e.PartiePrenanteId).IsRequired();
                ei.Property(e => e.Description).IsRequired().HasMaxLength(1000);
                ei.Property(e => e.Ordre).IsRequired();
            });
        });

        modelBuilder.Entity<ScenarioOperationnel>(entity =>
        {
            entity.ToTable("scenarios_operationnels");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.EtudeId).IsRequired();
            entity.Property(s => s.CheminAttaqueId).IsRequired();
            entity.Property(s => s.CreeLeUtc).IsRequired();
            entity.Ignore(s => s.VraisemblanceGlobale);
            entity.HasIndex(s => s.EtudeId);
            entity.HasIndex(s => s.CheminAttaqueId).IsUnique();

            entity.OwnsMany(s => s.ModesOperatoires, mo =>
            {
                mo.ToTable("modes_operatoires");
                mo.WithOwner().HasForeignKey("ScenarioOperationnelId");
                mo.HasKey(m => m.Id);
                mo.Property(m => m.Id).ValueGeneratedOnAdd();
                mo.Property(m => m.Description).IsRequired().HasMaxLength(2000);
                mo.Property(m => m.ProbabiliteSucces).IsRequired();
                mo.Property(m => m.DifficulteTechnique).IsRequired();
                mo.Property(m => m.VraisemblanceRetenue).HasConversion<string>().HasMaxLength(50);
                mo.Property(m => m.JustificationVraisemblance).HasMaxLength(2000);
                mo.Ignore(m => m.VraisemblanceCalculee);
                mo.Ignore(m => m.Vraisemblance);

                mo.OwnsMany(m => m.ActionsElementaires, ae =>
                {
                    ae.ToTable("actions_elementaires");
                    ae.WithOwner().HasForeignKey("ModeOperatoireId");
                    ae.HasKey(a => a.Id);
                    ae.Property(a => a.Id).ValueGeneratedOnAdd();
                    ae.Property(a => a.Description).IsRequired().HasMaxLength(1000);
                    ae.Property(a => a.Phase).IsRequired().HasConversion<string>().HasMaxLength(50);
                    ae.Property(a => a.BienSupportId).IsRequired();
                    ae.HasIndex("ModeOperatoireId");
                    ae.HasIndex(a => a.BienSupportId);
                });
            });
        });

        modelBuilder.Entity<ScenarioDeRisque>(entity =>
        {
            entity.ToTable("scenarios_de_risque");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.EtudeId).IsRequired();
            entity.Property(s => s.CheminAttaqueId).IsRequired();
            entity.Property(s => s.CreeLeUtc).IsRequired();
            entity.Property(s => s.NiveauRisqueInitialRetenu).HasConversion<string>().HasMaxLength(50);
            entity.Property(s => s.JustificationNiveauRisqueInitial).HasMaxLength(2000);
            entity.Property(s => s.VraisemblanceResiduelle).HasConversion<string>().HasMaxLength(50);
            entity.Property(s => s.NiveauRisqueResiduelCalcule).HasConversion<string>().HasMaxLength(50);
            entity.Property(s => s.NiveauRisqueResiduelRetenu).HasConversion<string>().HasMaxLength(50);
            entity.Property(s => s.JustificationNiveauRisqueResiduel).HasMaxLength(2000);
            entity.Property(s => s.NomProprietaireRisque).HasMaxLength(300);
            entity.Property(s => s.NomValidateurSecurite).HasMaxLength(300);
            entity.Property(s => s.NomSponsorExecutif).HasMaxLength(300);
            entity.Property(s => s.JustificationAcceptation).HasMaxLength(2000);
            entity.Ignore(s => s.NiveauRisqueResiduel);
            entity.Ignore(s => s.ClasseAcceptationResiduelle);
            entity.HasIndex(s => s.EtudeId);
            entity.HasIndex(s => s.CheminAttaqueId).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_scenarios_de_risque_gravite_residuelle",
                $"\"GraviteResiduelle\" IS NULL OR (\"GraviteResiduelle\" >= {EvenementRedoute.GraviteMin} AND \"GraviteResiduelle\" <= {EvenementRedoute.GraviteMax})"));
        });

        modelBuilder.Entity<PlanTraitementRisque>(entity =>
        {
            entity.ToTable("plans_traitement_risque");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.EtudeId).IsRequired();
            entity.HasIndex(p => p.EtudeId).IsUnique();

            entity.OwnsMany(p => p.Mesures, mesure =>
            {
                mesure.ToTable("mesures_traitement_risque");
                mesure.WithOwner().HasForeignKey("PlanTraitementRisqueId");
                mesure.HasKey(m => m.Id);
                mesure.Property(m => m.Id).ValueGeneratedOnAdd();
                mesure.Property(m => m.Description).IsRequired().HasMaxLength(2000);
                mesure.Property(m => m.Axe).IsRequired().HasConversion<string>().HasMaxLength(50);
                mesure.PrimitiveCollection<List<Guid>>("_scenariosDeRisqueIds").HasColumnName("scenarios_de_risque_ids");
                mesure.Property(m => m.Responsable).IsRequired().HasMaxLength(300);
                mesure.Property(m => m.FreinsEtDifficultes).HasMaxLength(2000);
                mesure.Property(m => m.CoutComplexite).IsRequired().HasConversion<string>().HasMaxLength(20);
                mesure.Property(m => m.Echeance).HasMaxLength(100);
                mesure.Property(m => m.Statut).IsRequired().HasConversion<string>().HasMaxLength(50);
                mesure.Property(m => m.CreeLeUtc).IsRequired();
            });
        });

        modelBuilder.Entity<Utilisateur>(entity =>
        {
            entity.ToTable("utilisateurs");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(320);
            entity.Property(u => u.NomAffiche).IsRequired().HasMaxLength(200);
            entity.Property(u => u.MotDePasseHache).IsRequired();
            entity.Property(u => u.CreeLeUtc).IsRequired();
            entity.HasIndex(u => u.Email).IsUnique();
        });

        base.OnModelCreating(modelBuilder);
    }
}
