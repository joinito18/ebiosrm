using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbiosRM.Api.Migrations
{
    /// <inheritdoc />
    public partial class AjoutThemeCodeControleReferentiel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodeControle",
                schema: "core_engine",
                table: "referentiels_applicables",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Theme",
                schema: "core_engine",
                table: "referentiels_applicables",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodeControle",
                schema: "core_engine",
                table: "referentiels_applicables");

            migrationBuilder.DropColumn(
                name: "Theme",
                schema: "core_engine",
                table: "referentiels_applicables");
        }
    }
}
