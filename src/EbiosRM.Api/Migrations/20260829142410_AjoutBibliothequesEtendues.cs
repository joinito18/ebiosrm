using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbiosRM.Api.Migrations
{
    /// <inheritdoc />
    public partial class AjoutBibliothequesEtendues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bibliotheque_biens_support",
                schema: "core_engine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProprietaireId = table.Column<Guid>(type: "uuid", nullable: false),
                    Intitule = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EntiteProprietaireTypique = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreeLeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bibliotheque_biens_support", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "bibliotheque_evenements_redoutes",
                schema: "core_engine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProprietaireId = table.Column<Guid>(type: "uuid", nullable: false),
                    Intitule = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    GraviteIndicative = table.Column<int>(type: "integer", nullable: true),
                    ImpactsTypes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreeLeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bibliotheque_evenements_redoutes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "bibliotheque_parties_prenantes",
                schema: "core_engine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProprietaireId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Categorie = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DescriptionCategorie = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RolesEtAttentes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Representant = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DependanceTypique = table.Column<int>(type: "integer", nullable: true),
                    PenetrationTypique = table.Column<int>(type: "integer", nullable: true),
                    MaturiteCyberTypique = table.Column<int>(type: "integer", nullable: true),
                    ConfianceTypique = table.Column<int>(type: "integer", nullable: true),
                    CreeLeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bibliotheque_parties_prenantes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "bibliotheque_valeurs_metier",
                schema: "core_engine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProprietaireId = table.Column<Guid>(type: "uuid", nullable: false),
                    Intitule = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NatureOuFinalite = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EntiteProprietaireTypique = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreeLeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bibliotheque_valeurs_metier", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bibliotheque_biens_support_ProprietaireId",
                schema: "core_engine",
                table: "bibliotheque_biens_support",
                column: "ProprietaireId");

            migrationBuilder.CreateIndex(
                name: "IX_bibliotheque_evenements_redoutes_ProprietaireId",
                schema: "core_engine",
                table: "bibliotheque_evenements_redoutes",
                column: "ProprietaireId");

            migrationBuilder.CreateIndex(
                name: "IX_bibliotheque_parties_prenantes_ProprietaireId",
                schema: "core_engine",
                table: "bibliotheque_parties_prenantes",
                column: "ProprietaireId");

            migrationBuilder.CreateIndex(
                name: "IX_bibliotheque_valeurs_metier_ProprietaireId",
                schema: "core_engine",
                table: "bibliotheque_valeurs_metier",
                column: "ProprietaireId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bibliotheque_biens_support",
                schema: "core_engine");

            migrationBuilder.DropTable(
                name: "bibliotheque_evenements_redoutes",
                schema: "core_engine");

            migrationBuilder.DropTable(
                name: "bibliotheque_parties_prenantes",
                schema: "core_engine");

            migrationBuilder.DropTable(
                name: "bibliotheque_valeurs_metier",
                schema: "core_engine");
        }
    }
}
