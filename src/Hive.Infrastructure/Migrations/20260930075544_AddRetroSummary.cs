using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRetroSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RetroSummaryCount",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 6);

            migrationBuilder.AddColumn<bool>(
                name: "RetroSummaryEnabled",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "RetroSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    WarningsJson = table.Column<string>(type: "TEXT", maxLength: 16000, nullable: false),
                    ActionItemsJson = table.Column<string>(type: "TEXT", maxLength: 16000, nullable: false),
                    SuggestionsJson = table.Column<string>(type: "TEXT", maxLength: 16000, nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RetrosAnalyzed = table.Column<int>(type: "INTEGER", nullable: false),
                    LatestRetroDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    SourceFingerprint = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    AnalyzedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetroSummaries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RetroSummaries_AnalyzedAt",
                table: "RetroSummaries",
                column: "AnalyzedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RetroSummaries");

            migrationBuilder.DropColumn(
                name: "RetroSummaryCount",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "RetroSummaryEnabled",
                table: "AppSettings");
        }
    }
}
