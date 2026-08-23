using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbiosRM.Api.Migrations
{
    /// <inheritdoc />
    public partial class AjoutMesuresEcosystemeEtMenaceResiduelle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConfianceResiduelle",
                schema: "core_engine",
                table: "parties_prenantes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DependanceResiduelle",
                schema: "core_engine",
                table: "parties_prenantes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaturiteCyberResiduelle",
                schema: "core_engine",
                table: "parties_prenantes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "NiveauMenaceResiduel",
                schema: "core_engine",
                table: "parties_prenantes",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PenetrationResiduelle",
                schema: "core_engine",
                table: "parties_prenantes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "mesures_ecosysteme",
                schema: "core_engine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreeLeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PartiePrenanteId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mesures_ecosysteme", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mesures_ecosysteme_parties_prenantes_PartiePrenanteId",
                        column: x => x.PartiePrenanteId,
                        principalSchema: "core_engine",
                        principalTable: "parties_prenantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_mesures_ecosysteme_PartiePrenanteId",
                schema: "core_engine",
                table: "mesures_ecosysteme",
                column: "PartiePrenanteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mesures_ecosysteme",
                schema: "core_engine");

            migrationBuilder.DropColumn(
                name: "ConfianceResiduelle",
                schema: "core_engine",
                table: "parties_prenantes");

            migrationBuilder.DropColumn(
                name: "DependanceResiduelle",
                schema: "core_engine",
                table: "parties_prenantes");

            migrationBuilder.DropColumn(
                name: "MaturiteCyberResiduelle",
                schema: "core_engine",
                table: "parties_prenantes");

            migrationBuilder.DropColumn(
                name: "NiveauMenaceResiduel",
                schema: "core_engine",
                table: "parties_prenantes");

            migrationBuilder.DropColumn(
                name: "PenetrationResiduelle",
                schema: "core_engine",
                table: "parties_prenantes");
        }
    }
}
