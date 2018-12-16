using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DevBots.Data.Migrations
{
    public partial class removePathFromScript : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsValid",
                table: "Scripts");

            migrationBuilder.RenameColumn(
                name: "Path",
                table: "Scripts",
                newName: "ServerPath");

            migrationBuilder.AddColumn<int>(
                name: "Lines",
                table: "Scripts",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Scripts",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Scripts",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Lines",
                table: "Scripts");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Scripts");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Scripts");

            migrationBuilder.RenameColumn(
                name: "ServerPath",
                table: "Scripts",
                newName: "Path");

            migrationBuilder.AddColumn<bool>(
                name: "IsValid",
                table: "Scripts",
                nullable: false,
                defaultValue: false);
        }
    }
}
