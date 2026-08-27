using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbiosRM.Api.Migrations
{
    /// <inheritdoc />
    public partial class AjoutProprietaireEtude : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProprietaireId",
                schema: "core_engine",
                table: "etudes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_etudes_ProprietaireId",
                schema: "core_engine",
                table: "etudes",
                column: "ProprietaireId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_etudes_ProprietaireId",
                schema: "core_engine",
                table: "etudes");

            migrationBuilder.DropColumn(
                name: "ProprietaireId",
                schema: "core_engine",
                table: "etudes");
        }
    }
}
