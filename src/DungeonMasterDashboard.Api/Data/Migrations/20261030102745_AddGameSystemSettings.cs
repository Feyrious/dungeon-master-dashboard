using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DungeonMasterDashboard.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGameSystemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameSystemImages",
                columns: table => new
                {
                    SystemId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameSystemImages", x => x.SystemId);
                });

            migrationBuilder.CreateTable(
                name: "GameSystemSettings",
                columns: table => new
                {
                    SystemId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Tagline = table.Column<string>(type: "TEXT", nullable: true),
                    IsHidden = table.Column<bool>(type: "INTEGER", nullable: false),
                    HiddenToolIds = table.Column<string>(type: "TEXT", nullable: false),
                    IsDark = table.Column<bool>(type: "INTEGER", nullable: true),
                    Background = table.Column<string>(type: "TEXT", nullable: true),
                    Surface = table.Column<string>(type: "TEXT", nullable: true),
                    Text = table.Column<string>(type: "TEXT", nullable: true),
                    MutedText = table.Column<string>(type: "TEXT", nullable: true),
                    Accent = table.Column<string>(type: "TEXT", nullable: true),
                    AccentText = table.Column<string>(type: "TEXT", nullable: true),
                    Border = table.Column<string>(type: "TEXT", nullable: true),
                    HeadingFont = table.Column<string>(type: "TEXT", nullable: true),
                    BodyFont = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameSystemSettings", x => x.SystemId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameSystemImages");

            migrationBuilder.DropTable(
                name: "GameSystemSettings");
        }
    }
}
