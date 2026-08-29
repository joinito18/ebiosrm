using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbiosRM.Api.Migrations
{
    /// <inheritdoc />
    public partial class AjoutSuiviIndicateursEtLibelleSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Libelle",
                schema: "core_engine",
                table: "snapshots_atelier",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "indicateurs_suivi",
                schema: "core_engine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EtudeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Categorie = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Unite = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Cible = table.Column<double>(type: "double precision", nullable: true),
                    SeuilAlerte = table.Column<double>(type: "double precision", nullable: true),
                    Sens = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CreeLeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_indicateurs_suivi", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "points_mesure_indicateur",
                schema: "core_engine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Valeur = table.Column<double>(type: "double precision", nullable: false),
                    Commentaire = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IndicateurSuiviId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_points_mesure_indicateur", x => x.Id);
                    table.ForeignKey(
                        name: "FK_points_mesure_indicateur_indicateurs_suivi_IndicateurSuiviId",
                        column: x => x.IndicateurSuiviId,
                        principalSchema: "core_engine",
                        principalTable: "indicateurs_suivi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_indicateurs_suivi_EtudeId",
                schema: "core_engine",
                table: "indicateurs_suivi",
                column: "EtudeId");

            migrationBuilder.CreateIndex(
                name: "IX_points_mesure_indicateur_IndicateurSuiviId",
                schema: "core_engine",
                table: "points_mesure_indicateur",
                column: "IndicateurSuiviId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "points_mesure_indicateur",
                schema: "core_engine");

            migrationBuilder.DropTable(
                name: "indicateurs_suivi",
                schema: "core_engine");

            migrationBuilder.DropColumn(
                name: "Libelle",
                schema: "core_engine",
                table: "snapshots_atelier");
        }
    }
}
