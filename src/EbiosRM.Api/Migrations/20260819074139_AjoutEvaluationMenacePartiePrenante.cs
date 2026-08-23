using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbiosRM.Api.Migrations
{
    /// <inheritdoc />
    public partial class AjoutEvaluationMenacePartiePrenante : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Confiance",
                schema: "core_engine",
                table: "parties_prenantes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Dependance",
                schema: "core_engine",
                table: "parties_prenantes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaturiteCyber",
                schema: "core_engine",
                table: "parties_prenantes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "NiveauMenace",
                schema: "core_engine",
                table: "parties_prenantes",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Penetration",
                schema: "core_engine",
                table: "parties_prenantes",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Confiance",
                schema: "core_engine",
                table: "parties_prenantes");

            migrationBuilder.DropColumn(
                name: "Dependance",
                schema: "core_engine",
                table: "parties_prenantes");

            migrationBuilder.DropColumn(
                name: "MaturiteCyber",
                schema: "core_engine",
                table: "parties_prenantes");

            migrationBuilder.DropColumn(
                name: "NiveauMenace",
                schema: "core_engine",
                table: "parties_prenantes");

            migrationBuilder.DropColumn(
                name: "Penetration",
                schema: "core_engine",
                table: "parties_prenantes");
        }
    }
}
