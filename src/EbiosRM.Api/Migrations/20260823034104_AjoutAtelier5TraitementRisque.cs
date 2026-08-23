using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbiosRM.Api.Migrations
{
    /// <inheritdoc />
    public partial class AjoutAtelier5TraitementRisque : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StatutAtelier5",
                schema: "core_engine",
                table: "etudes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Brouillon");

            migrationBuilder.CreateTable(
                name: "plans_traitement_risque",
                schema: "core_engine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EtudeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plans_traitement_risque", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "scenarios_de_risque",
                schema: "core_engine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EtudeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CheminAttaqueId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreeLeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NiveauRisqueInitialRetenu = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    JustificationNiveauRisqueInitial = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    GraviteResiduelle = table.Column<int>(type: "integer", nullable: true),
                    VraisemblanceResiduelle = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    NiveauRisqueResiduelCalcule = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    NiveauRisqueResiduelRetenu = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    JustificationNiveauRisqueResiduel = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AccepteParDirection = table.Column<bool>(type: "boolean", nullable: false),
                    NomProprietaireRisque = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    NomValidateurSecurite = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    NomSponsorExecutif = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    JustificationAcceptation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DateAcceptationUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scenarios_de_risque", x => x.Id);
                    table.CheckConstraint("CK_scenarios_de_risque_gravite_residuelle", "\"GraviteResiduelle\" IS NULL OR (\"GraviteResiduelle\" >= 1 AND \"GraviteResiduelle\" <= 4)");
                });

            migrationBuilder.CreateTable(
                name: "mesures_traitement_risque",
                schema: "core_engine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Axe = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Responsable = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    FreinsEtDifficultes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CoutComplexite = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Echeance = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Statut = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreeLeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlanTraitementRisqueId = table.Column<Guid>(type: "uuid", nullable: false),
                    scenarios_de_risque_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mesures_traitement_risque", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mesures_traitement_risque_plans_traitement_risque_PlanTrait~",
                        column: x => x.PlanTraitementRisqueId,
                        principalSchema: "core_engine",
                        principalTable: "plans_traitement_risque",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_mesures_traitement_risque_PlanTraitementRisqueId",
                schema: "core_engine",
                table: "mesures_traitement_risque",
                column: "PlanTraitementRisqueId");

            migrationBuilder.CreateIndex(
                name: "IX_plans_traitement_risque_EtudeId",
                schema: "core_engine",
                table: "plans_traitement_risque",
                column: "EtudeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_scenarios_de_risque_CheminAttaqueId",
                schema: "core_engine",
                table: "scenarios_de_risque",
                column: "CheminAttaqueId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_scenarios_de_risque_EtudeId",
                schema: "core_engine",
                table: "scenarios_de_risque",
                column: "EtudeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mesures_traitement_risque",
                schema: "core_engine");

            migrationBuilder.DropTable(
                name: "scenarios_de_risque",
                schema: "core_engine");

            migrationBuilder.DropTable(
                name: "plans_traitement_risque",
                schema: "core_engine");

            migrationBuilder.DropColumn(
                name: "StatutAtelier5",
                schema: "core_engine",
                table: "etudes");
        }
    }
}
