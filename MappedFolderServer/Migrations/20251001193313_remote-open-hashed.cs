using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MappedFolderServer.Migrations
{
    /// <inheritdoc />
    public partial class remoteopenhashed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OpenSecret",
                table: "RemoteWebsocketData",
                newName: "Secret");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "RemoteWebsocketData",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "RemoteWebsocketData");

            migrationBuilder.RenameColumn(
                name: "Secret",
                table: "RemoteWebsocketData",
                newName: "OpenSecret");
        }
    }
}
