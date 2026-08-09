using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbiosRM.Api.Migrations
{
    /// <inheritdoc />
    public partial class AjoutValeurMetier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "valeurs_metier",
                schema: "core_engine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EtudeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    EntiteResponsable = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreeLeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_valeurs_metier", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_valeurs_metier_EtudeId",
                schema: "core_engine",
                table: "valeurs_metier",
                column: "EtudeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "valeurs_metier",
                schema: "core_engine");
        }
    }
}
