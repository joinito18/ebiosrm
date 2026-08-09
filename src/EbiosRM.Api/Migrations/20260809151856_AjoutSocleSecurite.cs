using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbiosRM.Api.Migrations
{
    /// <inheritdoc />
    public partial class AjoutSocleSecurite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "socles_securite",
                schema: "core_engine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EtudeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_socles_securite", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "referentiels_applicables",
                schema: "core_engine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Etat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SocleSecuriteId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_referentiels_applicables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_referentiels_applicables_socles_securite_SocleSecuriteId",
                        column: x => x.SocleSecuriteId,
                        principalSchema: "core_engine",
                        principalTable: "socles_securite",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_referentiels_applicables_SocleSecuriteId",
                schema: "core_engine",
                table: "referentiels_applicables",
                column: "SocleSecuriteId");

            migrationBuilder.CreateIndex(
                name: "IX_socles_securite_EtudeId",
                schema: "core_engine",
                table: "socles_securite",
                column: "EtudeId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "referentiels_applicables",
                schema: "core_engine");

            migrationBuilder.DropTable(
                name: "socles_securite",
                schema: "core_engine");
        }
    }
}
