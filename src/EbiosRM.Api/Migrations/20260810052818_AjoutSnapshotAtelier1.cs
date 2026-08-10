using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbiosRM.Api.Migrations
{
    /// <inheritdoc />
    public partial class AjoutSnapshotAtelier1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "snapshots_atelier1",
                schema: "core_engine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EtudeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    DateCreationUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ContenuJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_snapshots_atelier1", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_snapshots_atelier1_EtudeId_Version",
                schema: "core_engine",
                table: "snapshots_atelier1",
                columns: new[] { "EtudeId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "snapshots_atelier1",
                schema: "core_engine");
        }
    }
}
