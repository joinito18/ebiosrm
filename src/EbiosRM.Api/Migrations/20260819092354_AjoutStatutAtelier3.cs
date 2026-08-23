using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbiosRM.Api.Migrations
{
    /// <inheritdoc />
    public partial class AjoutStatutAtelier3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StatutAtelier3",
                schema: "core_engine",
                table: "etudes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Brouillon");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StatutAtelier3",
                schema: "core_engine",
                table: "etudes");
        }
    }
}
