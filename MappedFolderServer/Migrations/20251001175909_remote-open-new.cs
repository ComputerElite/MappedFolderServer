using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MappedFolderServer.Migrations
{
    /// <inheritdoc />
    public partial class remoteopennew : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RemoteWebsocketData",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    OpenSecret = table.Column<string>(type: "TEXT", nullable: true),
                    OpensSlugId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Expires = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemoteWebsocketData", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RemoteWebsocketData");
        }
    }
}
