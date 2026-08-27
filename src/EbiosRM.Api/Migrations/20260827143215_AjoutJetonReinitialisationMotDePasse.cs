using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbiosRM.Api.Migrations
{
    /// <inheritdoc />
    public partial class AjoutJetonReinitialisationMotDePasse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "JetonReinitialisationExpireLeUtc",
                schema: "core_engine",
                table: "utilisateurs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JetonReinitialisationHache",
                schema: "core_engine",
                table: "utilisateurs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_utilisateurs_JetonReinitialisationHache",
                schema: "core_engine",
                table: "utilisateurs",
                column: "JetonReinitialisationHache");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_utilisateurs_JetonReinitialisationHache",
                schema: "core_engine",
                table: "utilisateurs");

            migrationBuilder.DropColumn(
                name: "JetonReinitialisationExpireLeUtc",
                schema: "core_engine",
                table: "utilisateurs");

            migrationBuilder.DropColumn(
                name: "JetonReinitialisationHache",
                schema: "core_engine",
                table: "utilisateurs");
        }
    }
}
