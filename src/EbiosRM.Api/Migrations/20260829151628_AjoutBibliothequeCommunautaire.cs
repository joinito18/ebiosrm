using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbiosRM.Api.Migrations
{
    /// <inheritdoc />
    public partial class AjoutBibliothequeCommunautaire : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bibliotheque_publications",
                schema: "core_engine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TypeEntite = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    EntiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProprietaireId = table.Column<Guid>(type: "uuid", nullable: false),
                    PublieLeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Masquee = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bibliotheque_publications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "bibliotheque_signalements",
                schema: "core_engine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SignalePar = table.Column<Guid>(type: "uuid", nullable: false),
                    Motif = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreeLeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublicationBibliothequeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bibliotheque_signalements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bibliotheque_signalements_bibliotheque_publications_Publica~",
                        column: x => x.PublicationBibliothequeId,
                        principalSchema: "core_engine",
                        principalTable: "bibliotheque_publications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bibliotheque_publications_ProprietaireId",
                schema: "core_engine",
                table: "bibliotheque_publications",
                column: "ProprietaireId");

            migrationBuilder.CreateIndex(
                name: "IX_bibliotheque_publications_TypeEntite_EntiteId",
                schema: "core_engine",
                table: "bibliotheque_publications",
                columns: new[] { "TypeEntite", "EntiteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bibliotheque_signalements_PublicationBibliothequeId_Signale~",
                schema: "core_engine",
                table: "bibliotheque_signalements",
                columns: new[] { "PublicationBibliothequeId", "SignalePar" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bibliotheque_signalements",
                schema: "core_engine");

            migrationBuilder.DropTable(
                name: "bibliotheque_publications",
                schema: "core_engine");
        }
    }
}
