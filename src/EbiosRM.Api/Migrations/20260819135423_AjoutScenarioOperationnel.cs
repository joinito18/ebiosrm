using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbiosRM.Api.Migrations
{
    /// <inheritdoc />
    public partial class AjoutScenarioOperationnel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "scenarios_operationnels",
                schema: "core_engine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EtudeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CheminAttaqueId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreeLeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scenarios_operationnels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "modes_operatoires",
                schema: "core_engine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ActionsConnaitre = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ActionsRentrer = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ActionsTrouver = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ActionsExploiter = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProbabiliteSucces = table.Column<int>(type: "integer", nullable: false),
                    DifficulteTechnique = table.Column<int>(type: "integer", nullable: false),
                    ScenarioOperationnelId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_modes_operatoires", x => x.Id);
                    table.ForeignKey(
                        name: "FK_modes_operatoires_scenarios_operationnels_ScenarioOperation~",
                        column: x => x.ScenarioOperationnelId,
                        principalSchema: "core_engine",
                        principalTable: "scenarios_operationnels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_modes_operatoires_ScenarioOperationnelId",
                schema: "core_engine",
                table: "modes_operatoires",
                column: "ScenarioOperationnelId");

            migrationBuilder.CreateIndex(
                name: "IX_scenarios_operationnels_CheminAttaqueId",
                schema: "core_engine",
                table: "scenarios_operationnels",
                column: "CheminAttaqueId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_scenarios_operationnels_EtudeId",
                schema: "core_engine",
                table: "scenarios_operationnels",
                column: "EtudeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "modes_operatoires",
                schema: "core_engine");

            migrationBuilder.DropTable(
                name: "scenarios_operationnels",
                schema: "core_engine");
        }
    }
}
