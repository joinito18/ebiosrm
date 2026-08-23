using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbiosRM.Api.Migrations
{
    /// <summary>
    /// Généralise SnapshotAtelier1 (spécifique à l'Atelier 1) en SnapshotAtelier
    /// générique (NumeroAtelier), conformément au diagramme de classes de
    /// référence du projet. Le scaffolder EF a d'abord proposé un DropTable +
    /// CreateTable qui aurait perdu les snapshots déjà existants (étude
    /// BioGenTech, Atelier 1) -- corrigé manuellement en RenameTable + ajout
    /// de colonne avec backfill explicite (NumeroAtelier = 1 pour toutes les
    /// lignes existantes, puisqu'elles proviennent toutes de l'ancien
    /// mécanisme spécifique à l'Atelier 1), sans aucune perte de données.
    /// </summary>
    public partial class GeneralisationSnapshotAtelier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "snapshots_atelier1",
                schema: "core_engine",
                newName: "snapshots_atelier",
                newSchema: "core_engine");

            migrationBuilder.AddColumn<int>(
                name: "NumeroAtelier",
                schema: "core_engine",
                table: "snapshots_atelier",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.DropIndex(
                name: "IX_snapshots_atelier1_EtudeId_Version",
                schema: "core_engine",
                table: "snapshots_atelier");

            migrationBuilder.Sql(
                "ALTER TABLE core_engine.snapshots_atelier RENAME CONSTRAINT \"PK_snapshots_atelier1\" TO \"PK_snapshots_atelier\";");

            migrationBuilder.CreateIndex(
                name: "IX_snapshots_atelier_EtudeId_NumeroAtelier_Version",
                schema: "core_engine",
                table: "snapshots_atelier",
                columns: new[] { "EtudeId", "NumeroAtelier", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_snapshots_atelier_EtudeId_NumeroAtelier_Version",
                schema: "core_engine",
                table: "snapshots_atelier");

            migrationBuilder.DropColumn(
                name: "NumeroAtelier",
                schema: "core_engine",
                table: "snapshots_atelier");

            migrationBuilder.Sql(
                "ALTER TABLE core_engine.snapshots_atelier RENAME CONSTRAINT \"PK_snapshots_atelier\" TO \"PK_snapshots_atelier1\";");

            migrationBuilder.RenameTable(
                name: "snapshots_atelier",
                schema: "core_engine",
                newName: "snapshots_atelier1",
                newSchema: "core_engine");

            migrationBuilder.CreateIndex(
                name: "IX_snapshots_atelier1_EtudeId_Version",
                schema: "core_engine",
                table: "snapshots_atelier1",
                columns: new[] { "EtudeId", "Version" },
                unique: true);
        }
    }
}
