using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbiosRM.Api.Migrations
{
    /// <inheritdoc />
    public partial class AjoutCheminAttaqueEtCibleScenario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EvenementRedouteId",
                schema: "core_engine",
                table: "scenarios_strategiques",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "chemins_attaque",
                schema: "core_engine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EtudeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScenarioStrategiqueId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreeLeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chemins_attaque", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "evenements_intermediaires",
                schema: "core_engine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartiePrenanteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Ordre = table.Column<int>(type: "integer", nullable: false),
                    CheminAttaqueId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evenements_intermediaires", x => x.Id);
                    table.ForeignKey(
                        name: "FK_evenements_intermediaires_chemins_attaque_CheminAttaqueId",
                        column: x => x.CheminAttaqueId,
                        principalSchema: "core_engine",
                        principalTable: "chemins_attaque",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_chemins_attaque_EtudeId",
                schema: "core_engine",
                table: "chemins_attaque",
                column: "EtudeId");

            migrationBuilder.CreateIndex(
                name: "IX_chemins_attaque_ScenarioStrategiqueId",
                schema: "core_engine",
                table: "chemins_attaque",
                column: "ScenarioStrategiqueId");

            migrationBuilder.CreateIndex(
                name: "IX_evenements_intermediaires_CheminAttaqueId",
                schema: "core_engine",
                table: "evenements_intermediaires",
                column: "CheminAttaqueId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "evenements_intermediaires",
                schema: "core_engine");

            migrationBuilder.DropTable(
                name: "chemins_attaque",
                schema: "core_engine");

            migrationBuilder.DropColumn(
                name: "EvenementRedouteId",
                schema: "core_engine",
                table: "scenarios_strategiques");
        }
    }
}
