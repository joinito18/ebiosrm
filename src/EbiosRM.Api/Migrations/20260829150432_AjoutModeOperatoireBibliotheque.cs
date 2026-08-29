using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbiosRM.Api.Migrations
{
    /// <inheritdoc />
    public partial class AjoutModeOperatoireBibliotheque : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bibliotheque_modes_operatoires",
                schema: "core_engine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProprietaireId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ProbabiliteSuccesTypique = table.Column<int>(type: "integer", nullable: true),
                    DifficulteTechniqueTypique = table.Column<int>(type: "integer", nullable: true),
                    CreeLeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bibliotheque_modes_operatoires", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "bibliotheque_actions_elementaires",
                schema: "core_engine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Ordre = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Phase = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CibleBienSupport = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    TechniqueMitre = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ModeOperatoireBibliothequeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bibliotheque_actions_elementaires", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bibliotheque_actions_elementaires_bibliotheque_modes_operat~",
                        column: x => x.ModeOperatoireBibliothequeId,
                        principalSchema: "core_engine",
                        principalTable: "bibliotheque_modes_operatoires",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bibliotheque_actions_elementaires_ModeOperatoireBibliothequ~",
                schema: "core_engine",
                table: "bibliotheque_actions_elementaires",
                column: "ModeOperatoireBibliothequeId");

            migrationBuilder.CreateIndex(
                name: "IX_bibliotheque_modes_operatoires_ProprietaireId",
                schema: "core_engine",
                table: "bibliotheque_modes_operatoires",
                column: "ProprietaireId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bibliotheque_actions_elementaires",
                schema: "core_engine");

            migrationBuilder.DropTable(
                name: "bibliotheque_modes_operatoires",
                schema: "core_engine");
        }
    }
}
