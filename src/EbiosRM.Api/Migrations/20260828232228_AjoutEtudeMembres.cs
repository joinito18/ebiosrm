using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbiosRM.Api.Migrations
{
    /// <inheritdoc />
    public partial class AjoutEtudeMembres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "etude_membres",
                schema: "core_engine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EtudeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UtilisateurId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AjouteLeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AjoutePar = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_etude_membres", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_etude_membres_EtudeId_UtilisateurId",
                schema: "core_engine",
                table: "etude_membres",
                columns: new[] { "EtudeId", "UtilisateurId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_etude_membres_UtilisateurId",
                schema: "core_engine",
                table: "etude_membres",
                column: "UtilisateurId");

            // Reprise : chaque etude existante (hors etude de demonstration
            // publique, ProprietaireId null) gagne un membre Proprietaire = son
            // createur.
            migrationBuilder.Sql(@"
                INSERT INTO core_engine.etude_membres (""Id"", ""EtudeId"", ""UtilisateurId"", ""Role"", ""AjouteLeUtc"", ""AjoutePar"")
                SELECT gen_random_uuid(), ""Id"", ""ProprietaireId"", 'Proprietaire', now(), NULL
                FROM core_engine.etudes
                WHERE ""ProprietaireId"" IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "etude_membres",
                schema: "core_engine");
        }
    }
}
