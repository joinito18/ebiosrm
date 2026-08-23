using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbiosRM.Api.Migrations
{
    /// <inheritdoc />
    public partial class OverridePertinence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Pertinence",
                schema: "core_engine",
                table: "couples_sr_ov",
                newName: "PertinenceCalculee");

            migrationBuilder.AddColumn<string>(
                name: "JustificationPertinence",
                schema: "core_engine",
                table: "couples_sr_ov",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PertinenceRetenue",
                schema: "core_engine",
                table: "couples_sr_ov",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JustificationPertinence",
                schema: "core_engine",
                table: "couples_sr_ov");

            migrationBuilder.DropColumn(
                name: "PertinenceRetenue",
                schema: "core_engine",
                table: "couples_sr_ov");

            migrationBuilder.RenameColumn(
                name: "PertinenceCalculee",
                schema: "core_engine",
                table: "couples_sr_ov",
                newName: "Pertinence");
        }
    }
}
