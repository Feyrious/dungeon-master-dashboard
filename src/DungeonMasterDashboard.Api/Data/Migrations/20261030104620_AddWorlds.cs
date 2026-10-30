using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DungeonMasterDashboard.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorlds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Worlds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Tagline = table.Column<string>(type: "TEXT", nullable: true),
                    Label = table.Column<string>(type: "TEXT", nullable: true),
                    Url = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    WikiSoftware = table.Column<string>(type: "TEXT", nullable: true),
                    IsDark = table.Column<bool>(type: "INTEGER", nullable: false),
                    Background = table.Column<string>(type: "TEXT", nullable: false),
                    Surface = table.Column<string>(type: "TEXT", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    MutedText = table.Column<string>(type: "TEXT", nullable: false),
                    Accent = table.Column<string>(type: "TEXT", nullable: false),
                    AccentText = table.Column<string>(type: "TEXT", nullable: false),
                    Border = table.Column<string>(type: "TEXT", nullable: false),
                    HeadingFont = table.Column<string>(type: "TEXT", nullable: false),
                    BodyFont = table.Column<string>(type: "TEXT", nullable: false),
                    BuiltInImage = table.Column<string>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Worlds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorldImages",
                columns: table => new
                {
                    WorldId = table.Column<int>(type: "INTEGER", nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorldImages", x => x.WorldId);
                    table.ForeignKey(
                        name: "FK_WorldImages_Worlds_WorldId",
                        column: x => x.WorldId,
                        principalTable: "Worlds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Worlds",
                columns: new[] { "Id", "Accent", "AccentText", "Background", "BodyFont", "Border", "BuiltInImage", "HeadingFont", "IsDark", "Label", "MutedText", "Name", "SortOrder", "Surface", "Tagline", "Text", "UpdatedAt", "Url", "WikiSoftware" },
                values: new object[,]
                {
                    { 1, "#b0263f", "#fff3f5", "#120a11", "Georgia, serif", "#3b2337", "curse-of-strahd", "'Times New Roman', Times, serif", true, "Dungeons & Dragons 5e", "#a68fa1", "Curse of Strahd", 1, "#1e111c", "Gothic horror in the mist-shrouded land of Barovia.", "#ecdfe8", new DateTimeOffset(new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "foundry-export" },
                    { 2, "#7cc6f2", "#07121d", "#0b1622", "'Helvetica Neue', Arial, sans-serif", "#24405e", "rime-of-the-frostmaiden", "Georgia, 'Times New Roman', serif", true, "Dungeons & Dragons 5e", "#8eaec8", "Rime of the Frostmaiden", 2, "#122339", "Survival under endless night in frozen Icewind Dale.", "#e5f0f9", new DateTimeOffset(new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "foundry-export" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorldImages");

            migrationBuilder.DropTable(
                name: "Worlds");
        }
    }
}
