using EbiosRM.Api.Modules.Audit.Domain;
using EbiosRM.Api.Modules.Bibliotheque.Domain;
using EbiosRM.Api.Modules.Collaboration.Domain;
using EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;
using EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;
using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;
using EbiosRM.Api.Modules.Identity.Domain;
using EbiosRM.Api.Modules.Suivi.Domain;
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
    public DbSet<EntreeJournal> JournalAudit => Set<EntreeJournal>();
    public DbSet<EtudeMembre> EtudeMembres => Set<EtudeMembre>();
    public DbSet<MesureBibliotheque> MesuresBibliotheque => Set<MesureBibliotheque>();
    public DbSet<SourceRisqueBibliotheque> SourcesRisqueBibliotheque => Set<SourceRisqueBibliotheque>();
    public DbSet<PartiePrenanteBibliotheque> PartiesPrenantesBibliotheque => Set<PartiePrenanteBibliotheque>();
    public DbSet<ValeurMetierBibliotheque> ValeursMetierBibliotheque => Set<ValeurMetierBibliotheque>();
    public DbSet<BienSupportBibliotheque> BiensSupportBibliotheque => Set<BienSupportBibliotheque>();
    public DbSet<EvenementRedouteBibliotheque> EvenementsRedoutesBibliotheque => Set<EvenementRedouteBibliotheque>();
    public DbSet<IndicateurSuivi> IndicateursSuivi => Set<IndicateurSuivi>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // SQLite (mode bureau) ne connait pas les schemas : ils deviendraient un
        // prefixe litteral dans le nom des tables. Uniquement sur PostgreSQL.
        if (Database.IsNpgsql())
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
            entity.Property(e => e.ProprietaireId);
            entity.HasIndex(e => e.ProprietaireId);
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
            // Stocke une chaine JSON serialisee cote C# (jamais interrogee en SQL) :
            // jsonb sur PostgreSQL, TEXT par defaut sur SQLite.
            var contenuJson = entity.Property(s => s.ContenuJson).IsRequired();
            if (Database.IsNpgsql())
                contenuJson.HasColumnType("jsonb");
            entity.Property(s => s.Libelle).HasMaxLength(200);
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
                    ae.Property(a => a.TechniqueMitre).HasMaxLength(20);
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
                mesure.PrimitiveCollection<List<string>>("_codesConformite").HasColumnName("codes_conformite");
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

        modelBuilder.Entity<EntreeJournal>(entity =>
        {
            entity.ToTable("journal_audit");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EtudeId).IsRequired();
            entity.Property(e => e.NomUtilisateur).IsRequired().HasMaxLength(200);
            entity.Property(e => e.DateUtc).IsRequired();
            entity.Property(e => e.Action).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Methode).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Chemin).IsRequired().HasMaxLength(500);
            entity.Property(e => e.StatutHttp).IsRequired();
            entity.HasIndex(e => new { e.EtudeId, e.DateUtc });
        });

        modelBuilder.Entity<EtudeMembre>(entity =>
        {
            entity.ToTable("etude_membres");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.EtudeId).IsRequired();
            entity.Property(m => m.UtilisateurId).IsRequired();
            entity.Property(m => m.Role).IsRequired().HasConversion<string>().HasMaxLength(20);
            entity.Property(m => m.AjouteLeUtc).IsRequired();
            entity.HasIndex(m => new { m.EtudeId, m.UtilisateurId }).IsUnique();
            entity.HasIndex(m => m.UtilisateurId);
        });

        // Bibliotheque : seules les entrees personnelles sont persistees
        // (ProprietaireId non null). Le catalogue systeme vit dans le code.
        modelBuilder.Entity<MesureBibliotheque>(entity =>
        {
            entity.ToTable("bibliotheque_mesures");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.ProprietaireId).IsRequired();
            entity.Property(m => m.Referentiel).IsRequired().HasConversion<string>().HasMaxLength(30);
            entity.Property(m => m.Code).HasMaxLength(30);
            entity.Property(m => m.Titre).IsRequired().HasMaxLength(500);
            entity.Property(m => m.Description).HasMaxLength(4000);
            entity.Property(m => m.Categorie).HasMaxLength(200);
            entity.Property(m => m.CreeLeUtc).IsRequired();
            entity.Ignore(m => m.EstSysteme);
            entity.HasIndex(m => m.ProprietaireId);
        });

        modelBuilder.Entity<IndicateurSuivi>(entity =>
        {
            entity.ToTable("indicateurs_suivi");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.EtudeId).IsRequired();
            entity.Property(i => i.Nom).IsRequired().HasMaxLength(300);
            entity.Property(i => i.Categorie).HasMaxLength(100);
            entity.Property(i => i.Unite).HasMaxLength(30);
            entity.Property(i => i.Sens).IsRequired().HasConversion<string>().HasMaxLength(10);
            entity.Property(i => i.CreeLeUtc).IsRequired();
            entity.HasIndex(i => i.EtudeId);

            entity.OwnsMany(i => i.Points, point =>
            {
                point.ToTable("points_mesure_indicateur");
                point.WithOwner().HasForeignKey("IndicateurSuiviId");
                point.HasKey(p => p.Id);
                point.Property(p => p.Id).ValueGeneratedOnAdd();
                point.Property(p => p.Date).IsRequired();
                point.Property(p => p.Valeur).IsRequired();
                point.Property(p => p.Commentaire).HasMaxLength(1000);
                point.HasIndex("IndicateurSuiviId");
            });
        });

        modelBuilder.Entity<SourceRisqueBibliotheque>(entity =>
        {
            entity.ToTable("bibliotheque_sources_risque");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.ProprietaireId).IsRequired();
            entity.Property(s => s.SourceRisque).IsRequired().HasConversion<string>().HasMaxLength(50);
            entity.Property(s => s.DescriptionSourceRisque).IsRequired().HasMaxLength(500);
            entity.Property(s => s.ObjectifVise).IsRequired().HasConversion<string>().HasMaxLength(50);
            entity.Property(s => s.DescriptionObjectifVise).IsRequired().HasMaxLength(500);
            entity.Property(s => s.Theme).HasMaxLength(100);
            entity.Property(s => s.CreeLeUtc).IsRequired();
            entity.Ignore(s => s.EstSysteme);
            entity.HasIndex(s => s.ProprietaireId);
        });

        modelBuilder.Entity<PartiePrenanteBibliotheque>(entity =>
        {
            entity.ToTable("bibliotheque_parties_prenantes");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.ProprietaireId).IsRequired();
            entity.Property(p => p.Nom).IsRequired().HasMaxLength(300);
            entity.Property(p => p.Categorie).IsRequired().HasConversion<string>().HasMaxLength(30);
            entity.Property(p => p.DescriptionCategorie).HasMaxLength(200);
            entity.Property(p => p.RolesEtAttentes).IsRequired().HasMaxLength(2000);
            entity.Property(p => p.Representant).HasMaxLength(200);
            entity.Property(p => p.CreeLeUtc).IsRequired();
            entity.Ignore(p => p.EstSysteme);
            entity.HasIndex(p => p.ProprietaireId);
        });

        modelBuilder.Entity<ValeurMetierBibliotheque>(entity =>
        {
            entity.ToTable("bibliotheque_valeurs_metier");
            entity.HasKey(v => v.Id);
            entity.Property(v => v.ProprietaireId).IsRequired();
            entity.Property(v => v.Intitule).IsRequired().HasMaxLength(500);
            entity.Property(v => v.NatureOuFinalite).HasMaxLength(200);
            entity.Property(v => v.EntiteProprietaireTypique).HasMaxLength(300);
            entity.Property(v => v.CreeLeUtc).IsRequired();
            entity.Ignore(v => v.EstSysteme);
            entity.HasIndex(v => v.ProprietaireId);
        });

        modelBuilder.Entity<BienSupportBibliotheque>(entity =>
        {
            entity.ToTable("bibliotheque_biens_support");
            entity.HasKey(b => b.Id);
            entity.Property(b => b.ProprietaireId).IsRequired();
            entity.Property(b => b.Intitule).IsRequired().HasMaxLength(500);
            entity.Property(b => b.Type).IsRequired().HasConversion<string>().HasMaxLength(30);
            entity.Property(b => b.EntiteProprietaireTypique).HasMaxLength(300);
            entity.Property(b => b.Description).HasMaxLength(2000);
            entity.Property(b => b.CreeLeUtc).IsRequired();
            entity.Ignore(b => b.EstSysteme);
            entity.HasIndex(b => b.ProprietaireId);
        });

        modelBuilder.Entity<EvenementRedouteBibliotheque>(entity =>
        {
            entity.ToTable("bibliotheque_evenements_redoutes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProprietaireId).IsRequired();
            entity.Property(e => e.Intitule).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ImpactsTypes).HasMaxLength(500);
            entity.Property(e => e.CreeLeUtc).IsRequired();
            entity.Ignore(e => e.EstSysteme);
            entity.HasIndex(e => e.ProprietaireId);
        });

        base.OnModelCreating(modelBuilder);
    }
}
