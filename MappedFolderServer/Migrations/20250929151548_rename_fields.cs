using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MappedFolderServer.Migrations
{
    /// <inheritdoc />
    public partial class rename_fields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Mappings_UserId",
                table: "Mappings");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Mappings",
                newName: "CreatedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "Mappings",
                newName: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Mappings_UserId",
                table: "Mappings",
                column: "UserId");
        }
    }
}
