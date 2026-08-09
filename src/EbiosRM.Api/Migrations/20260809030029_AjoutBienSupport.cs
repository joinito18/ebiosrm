using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbiosRM.Api.Migrations
{
    /// <inheritdoc />
    public partial class AjoutBienSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "biens_support",
                schema: "core_engine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EtudeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValeurMetierId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntiteResponsable = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreeLeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_biens_support", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_biens_support_EtudeId",
                schema: "core_engine",
                table: "biens_support",
                column: "EtudeId");

            migrationBuilder.CreateIndex(
                name: "IX_biens_support_ValeurMetierId",
                schema: "core_engine",
                table: "biens_support",
                column: "ValeurMetierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "biens_support",
                schema: "core_engine");
        }
    }
}
