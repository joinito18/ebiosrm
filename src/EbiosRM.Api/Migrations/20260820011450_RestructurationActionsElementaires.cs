using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbiosRM.Api.Migrations
{
    /// <summary>
    /// Remplace les 4 champs texte libre du mode opératoire (ActionsConnaitre/
    /// Rentrer/Trouver/Exploiter) par une vraie collection ActionElementaire
    /// (1..*), chacune reliée à un BienSupport précis -- conforme à la doc
    /// officielle Atelier 4 et au diagramme de référence du projet. Perte de
    /// données assumée sur les colonnes texte supprimées (aucun backfill
    /// possible : aucun bien support n'a jamais été renseigné avant cette
    /// migration) -- les modes opératoires existants de l'étude BioGenTech
    /// sont recréés manuellement après coup via l'API.
    /// </summary>
    public partial class RestructurationActionsElementaires : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActionsConnaitre",
                schema: "core_engine",
                table: "modes_operatoires");

            migrationBuilder.DropColumn(
                name: "ActionsExploiter",
                schema: "core_engine",
                table: "modes_operatoires");

            migrationBuilder.DropColumn(
                name: "ActionsRentrer",
                schema: "core_engine",
                table: "modes_operatoires");

            migrationBuilder.DropColumn(
                name: "ActionsTrouver",
                schema: "core_engine",
                table: "modes_operatoires");

            migrationBuilder.CreateTable(
                name: "actions_elementaires",
                schema: "core_engine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Phase = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BienSupportId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModeOperatoireId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_actions_elementaires", x => x.Id);
                    table.ForeignKey(
                        name: "FK_actions_elementaires_modes_operatoires_ModeOperatoireId",
                        column: x => x.ModeOperatoireId,
                        principalSchema: "core_engine",
                        principalTable: "modes_operatoires",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_actions_elementaires_BienSupportId",
                schema: "core_engine",
                table: "actions_elementaires",
                column: "BienSupportId");

            migrationBuilder.CreateIndex(
                name: "IX_actions_elementaires_ModeOperatoireId",
                schema: "core_engine",
                table: "actions_elementaires",
                column: "ModeOperatoireId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "actions_elementaires",
                schema: "core_engine");

            migrationBuilder.AddColumn<string>(
                name: "ActionsConnaitre",
                schema: "core_engine",
                table: "modes_operatoires",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActionsExploiter",
                schema: "core_engine",
                table: "modes_operatoires",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActionsRentrer",
                schema: "core_engine",
                table: "modes_operatoires",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActionsTrouver",
                schema: "core_engine",
                table: "modes_operatoires",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }
    }
}
