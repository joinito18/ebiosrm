using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbiosRM.Api.Migrations
{
    /// <inheritdoc />
    public partial class AjoutEtude : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "core_engine");

            migrationBuilder.CreateTable(
                name: "etudes",
                schema: "core_engine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Perimetre = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    VersionReferentielId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreeLeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_etudes", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "etudes",
                schema: "core_engine");
        }
    }
}
