using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MappedFolderServer.Migrations
{
    /// <inheritdoc />
    public partial class reveal_data : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mappings_Sessions_AuthenticatedSessionId",
                table: "Mappings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Mappings",
                table: "Mappings");

            migrationBuilder.RenameTable(
                name: "Mappings",
                newName: "Slugs");

            migrationBuilder.RenameIndex(
                name: "IX_Mappings_Slug",
                table: "Slugs",
                newName: "IX_Slugs_Slug");

            migrationBuilder.RenameIndex(
                name: "IX_Mappings_AuthenticatedSessionId",
                table: "Slugs",
                newName: "IX_Slugs_AuthenticatedSessionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Slugs",
                table: "Slugs",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Reveal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ForSlug = table.Column<Guid>(type: "TEXT", nullable: false),
                    SlugName = table.Column<string>(type: "TEXT", nullable: false),
                    ForUser = table.Column<Guid>(type: "TEXT", nullable: false),
                    RemoteUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reveal", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Slugs_Sessions_AuthenticatedSessionId",
                table: "Slugs",
                column: "AuthenticatedSessionId",
                principalTable: "Sessions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Slugs_Sessions_AuthenticatedSessionId",
                table: "Slugs");

            migrationBuilder.DropTable(
                name: "Reveal");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Slugs",
                table: "Slugs");

            migrationBuilder.RenameTable(
                name: "Slugs",
                newName: "Mappings");

            migrationBuilder.RenameIndex(
                name: "IX_Slugs_Slug",
                table: "Mappings",
                newName: "IX_Mappings_Slug");

            migrationBuilder.RenameIndex(
                name: "IX_Slugs_AuthenticatedSessionId",
                table: "Mappings",
                newName: "IX_Mappings_AuthenticatedSessionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Mappings",
                table: "Mappings",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Mappings_Sessions_AuthenticatedSessionId",
                table: "Mappings",
                column: "AuthenticatedSessionId",
                principalTable: "Sessions",
                principalColumn: "Id");
        }
    }
}
