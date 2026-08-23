using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbiosRM.Api.Migrations
{
    /// <inheritdoc />
    public partial class OverrideVraisemblance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JustificationVraisemblance",
                schema: "core_engine",
                table: "modes_operatoires",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VraisemblanceRetenue",
                schema: "core_engine",
                table: "modes_operatoires",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JustificationVraisemblance",
                schema: "core_engine",
                table: "modes_operatoires");

            migrationBuilder.DropColumn(
                name: "VraisemblanceRetenue",
                schema: "core_engine",
                table: "modes_operatoires");
        }
    }
}
