using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbiosRM.Api.Migrations
{
    /// <inheritdoc />
    public partial class AjoutStatutEtude : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Statut",
                schema: "core_engine",
                table: "etudes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Statut",
                schema: "core_engine",
                table: "etudes");
        }
    }
}
