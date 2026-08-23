using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbiosRM.Api.Migrations
{
    /// <summary>
    /// Le scaffolder EF a d'abord mal apparié les renommages (colonne
    /// NiveauDangerositeResiduel -> NiveauDangerositeRetenu et NiveauDangerosite
    /// -> NiveauDangerositeResiduelRetenu, ce qui aurait mélangé les valeurs
    /// initiale/résiduelle existantes) -- corrigé manuellement ci-dessous pour
    /// suivre exactement le même schéma que OverridePertinence : renommage
    /// Calculee + ajout des colonnes Retenu/Justification, sans perte ni
    /// mélange de données sur l'étude BioGenTech.
    /// </summary>
    public partial class OverrideDangerosite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NiveauDangerositeResiduel",
                schema: "core_engine",
                table: "parties_prenantes",
                newName: "NiveauDangerositeResiduelCalcule");

            migrationBuilder.RenameColumn(
                name: "NiveauDangerosite",
                schema: "core_engine",
                table: "parties_prenantes",
                newName: "NiveauDangerositeCalcule");

            migrationBuilder.AddColumn<string>(
                name: "JustificationDangerosite",
                schema: "core_engine",
                table: "parties_prenantes",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JustificationDangerositeResiduelle",
                schema: "core_engine",
                table: "parties_prenantes",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "NiveauDangerositeRetenu",
                schema: "core_engine",
                table: "parties_prenantes",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "NiveauDangerositeResiduelRetenu",
                schema: "core_engine",
                table: "parties_prenantes",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JustificationDangerosite",
                schema: "core_engine",
                table: "parties_prenantes");

            migrationBuilder.DropColumn(
                name: "JustificationDangerositeResiduelle",
                schema: "core_engine",
                table: "parties_prenantes");

            migrationBuilder.DropColumn(
                name: "NiveauDangerositeRetenu",
                schema: "core_engine",
                table: "parties_prenantes");

            migrationBuilder.DropColumn(
                name: "NiveauDangerositeResiduelRetenu",
                schema: "core_engine",
                table: "parties_prenantes");

            migrationBuilder.RenameColumn(
                name: "NiveauDangerositeResiduelCalcule",
                schema: "core_engine",
                table: "parties_prenantes",
                newName: "NiveauDangerositeResiduel");

            migrationBuilder.RenameColumn(
                name: "NiveauDangerositeCalcule",
                schema: "core_engine",
                table: "parties_prenantes",
                newName: "NiveauDangerosite");
        }
    }
}
