using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbiosRM.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenommageMenaceEnDangerosite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NiveauMenaceResiduel",
                schema: "core_engine",
                table: "parties_prenantes",
                newName: "NiveauDangerositeResiduel");

            migrationBuilder.RenameColumn(
                name: "NiveauMenace",
                schema: "core_engine",
                table: "parties_prenantes",
                newName: "NiveauDangerosite");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NiveauDangerositeResiduel",
                schema: "core_engine",
                table: "parties_prenantes",
                newName: "NiveauMenaceResiduel");

            migrationBuilder.RenameColumn(
                name: "NiveauDangerosite",
                schema: "core_engine",
                table: "parties_prenantes",
                newName: "NiveauMenace");
        }
    }
}
