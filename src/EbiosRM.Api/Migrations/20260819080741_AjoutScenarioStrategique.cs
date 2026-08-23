using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbiosRM.Api.Migrations
{
    /// <inheritdoc />
    public partial class AjoutScenarioStrategique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "scenarios_strategiques",
                schema: "core_engine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EtudeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CoupleSourceRisqueObjectifViseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreeLeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scenarios_strategiques", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_scenarios_strategiques_CoupleSourceRisqueObjectifViseId",
                schema: "core_engine",
                table: "scenarios_strategiques",
                column: "CoupleSourceRisqueObjectifViseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_scenarios_strategiques_EtudeId",
                schema: "core_engine",
                table: "scenarios_strategiques",
                column: "EtudeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scenarios_strategiques",
                schema: "core_engine");
        }
    }
}
