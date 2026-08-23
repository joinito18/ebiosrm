using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbiosRM.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenommageResponsableEnProprietaire : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EntiteResponsable",
                schema: "core_engine",
                table: "valeurs_metier",
                newName: "EntiteProprietaire");

            migrationBuilder.RenameColumn(
                name: "EntiteResponsable",
                schema: "core_engine",
                table: "biens_support",
                newName: "EntiteProprietaire");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EntiteProprietaire",
                schema: "core_engine",
                table: "valeurs_metier",
                newName: "EntiteResponsable");

            migrationBuilder.RenameColumn(
                name: "EntiteProprietaire",
                schema: "core_engine",
                table: "biens_support",
                newName: "EntiteResponsable");
        }
    }
}
