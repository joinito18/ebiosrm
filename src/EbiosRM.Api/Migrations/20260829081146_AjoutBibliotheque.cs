using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbiosRM.Api.Migrations
{
    /// <inheritdoc />
    public partial class AjoutBibliotheque : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bibliotheque_mesures",
                schema: "core_engine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProprietaireId = table.Column<Guid>(type: "uuid", nullable: false),
                    Referentiel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Titre = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Categorie = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreeLeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bibliotheque_mesures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "bibliotheque_sources_risque",
                schema: "core_engine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProprietaireId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceRisque = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DescriptionSourceRisque = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ObjectifVise = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DescriptionObjectifVise = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Theme = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MotivationTypique = table.Column<int>(type: "integer", nullable: true),
                    RessourcesTypiques = table.Column<int>(type: "integer", nullable: true),
                    CreeLeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bibliotheque_sources_risque", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bibliotheque_mesures_ProprietaireId",
                schema: "core_engine",
                table: "bibliotheque_mesures",
                column: "ProprietaireId");

            migrationBuilder.CreateIndex(
                name: "IX_bibliotheque_sources_risque_ProprietaireId",
                schema: "core_engine",
                table: "bibliotheque_sources_risque",
                column: "ProprietaireId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bibliotheque_mesures",
                schema: "core_engine");

            migrationBuilder.DropTable(
                name: "bibliotheque_sources_risque",
                schema: "core_engine");
        }
    }
}
