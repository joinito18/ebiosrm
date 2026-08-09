using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbiosRM.Api.Migrations
{
    /// <inheritdoc />
    public partial class AjoutEvenementRedoute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "evenements_redoutes",
                schema: "core_engine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EtudeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValeurMetierId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Gravite = table.Column<int>(type: "integer", nullable: false),
                    CreeLeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evenements_redoutes", x => x.Id);
                    table.CheckConstraint("CK_evenements_redoutes_gravite", "\"Gravite\" >= 1 AND \"Gravite\" <= 4");
                });

            migrationBuilder.CreateIndex(
                name: "IX_evenements_redoutes_EtudeId",
                schema: "core_engine",
                table: "evenements_redoutes",
                column: "EtudeId");

            migrationBuilder.CreateIndex(
                name: "IX_evenements_redoutes_ValeurMetierId",
                schema: "core_engine",
                table: "evenements_redoutes",
                column: "ValeurMetierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "evenements_redoutes",
                schema: "core_engine");
        }
    }
}
