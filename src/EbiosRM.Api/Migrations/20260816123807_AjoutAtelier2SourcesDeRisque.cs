using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbiosRM.Api.Migrations
{
    /// <inheritdoc />
    public partial class AjoutAtelier2SourcesDeRisque : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "couples_sr_ov",
                schema: "core_engine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EtudeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceRisque = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DescriptionSourceRisque = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ObjectifVise = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DescriptionObjectifVise = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContexteVulnerabilite = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Theme = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Motivation = table.Column<int>(type: "integer", nullable: false),
                    Ressources = table.Column<int>(type: "integer", nullable: false),
                    Pertinence = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreeLeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_couples_sr_ov", x => x.Id);
                    table.CheckConstraint("CK_couples_sr_ov_motivation", "\"Motivation\" >= 1 AND \"Motivation\" <= 4");
                    table.CheckConstraint("CK_couples_sr_ov_ressources", "\"Ressources\" >= 1 AND \"Ressources\" <= 4");
                });

            migrationBuilder.CreateTable(
                name: "parties_prenantes",
                schema: "core_engine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EtudeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    RolesEtAttentes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Representant = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CreeLeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parties_prenantes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_couples_sr_ov_EtudeId",
                schema: "core_engine",
                table: "couples_sr_ov",
                column: "EtudeId");

            migrationBuilder.CreateIndex(
                name: "IX_couples_sr_ov_Theme",
                schema: "core_engine",
                table: "couples_sr_ov",
                column: "Theme");

            migrationBuilder.CreateIndex(
                name: "IX_parties_prenantes_EtudeId",
                schema: "core_engine",
                table: "parties_prenantes",
                column: "EtudeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "couples_sr_ov",
                schema: "core_engine");

            migrationBuilder.DropTable(
                name: "parties_prenantes",
                schema: "core_engine");
        }
    }
}
