using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbiosRM.Api.Migrations
{
    /// <inheritdoc />
    public partial class AjoutJournalAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "journal_audit",
                schema: "core_engine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EtudeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UtilisateurId = table.Column<Guid>(type: "uuid", nullable: true),
                    NomUtilisateur = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Action = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Methode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Chemin = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StatutHttp = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journal_audit", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_journal_audit_EtudeId_DateUtc",
                schema: "core_engine",
                table: "journal_audit",
                columns: new[] { "EtudeId", "DateUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "journal_audit",
                schema: "core_engine");
        }
    }
}
