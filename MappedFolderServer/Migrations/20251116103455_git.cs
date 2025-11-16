using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MappedFolderServer.Migrations
{
    /// <inheritdoc />
    public partial class git : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RepoId",
                table: "Slugs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GitRepo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    Branch = table.Column<string>(type: "TEXT", nullable: false),
                    CurrentCommitHash = table.Column<string>(type: "TEXT", nullable: false),
                    LastPulled = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: true),
                    EncryptedPassword = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GitRepo", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Slugs_RepoId",
                table: "Slugs",
                column: "RepoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Slugs_GitRepo_RepoId",
                table: "Slugs",
                column: "RepoId",
                principalTable: "GitRepo",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Slugs_GitRepo_RepoId",
                table: "Slugs");

            migrationBuilder.DropTable(
                name: "GitRepo");

            migrationBuilder.DropIndex(
                name: "IX_Slugs_RepoId",
                table: "Slugs");

            migrationBuilder.DropColumn(
                name: "RepoId",
                table: "Slugs");
        }
    }
}
