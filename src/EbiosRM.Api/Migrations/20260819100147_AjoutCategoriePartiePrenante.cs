using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbiosRM.Api.Migrations
{
    /// <inheritdoc />
    public partial class AjoutCategoriePartiePrenante : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Categorie",
                schema: "core_engine",
                table: "parties_prenantes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Autre");

            migrationBuilder.AddColumn<string>(
                name: "DescriptionCategorie",
                schema: "core_engine",
                table: "parties_prenantes",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                defaultValue: "Non renseignee (donnee creee avant l ajout de la categorisation)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Categorie",
                schema: "core_engine",
                table: "parties_prenantes");

            migrationBuilder.DropColumn(
                name: "DescriptionCategorie",
                schema: "core_engine",
                table: "parties_prenantes");
        }
    }
}
