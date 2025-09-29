using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MappedFolderServer.Migrations
{
    /// <inheritdoc />
    public partial class add_users : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Mappings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OidcId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    IsAdmin = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Mappings_Slug",
                table: "Mappings",
                column: "Slug");

            migrationBuilder.CreateIndex(
                name: "IX_Mappings_UserId",
                table: "Mappings",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Mappings_Slug",
                table: "Mappings");

            migrationBuilder.DropIndex(
                name: "IX_Mappings_UserId",
                table: "Mappings");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Mappings");
        }
    }
}
