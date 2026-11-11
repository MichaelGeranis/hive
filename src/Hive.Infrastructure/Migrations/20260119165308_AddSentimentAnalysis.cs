using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSentimentAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClaudeApiKey",
                table: "AppSettings",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SentimentAnalysisDays",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 90);

            migrationBuilder.AddColumn<bool>(
                name: "SentimentAnalysisEnabled",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "SentimentAnalysisCache",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DirectReportId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PositiveScore = table.Column<double>(type: "REAL", nullable: false),
                    NeutralScore = table.Column<double>(type: "REAL", nullable: false),
                    NegativeScore = table.Column<double>(type: "REAL", nullable: false),
                    OverallSentiment = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    KeyThemesJson = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    TrendDataJson = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: false),
                    NotesAnalyzed = table.Column<int>(type: "INTEGER", nullable: false),
                    DaysAnalyzed = table.Column<int>(type: "INTEGER", nullable: false),
                    LatestNoteDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AnalyzedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SentimentAnalysisCache", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SentimentAnalysisCache_AnalyzedAt",
                table: "SentimentAnalysisCache",
                column: "AnalyzedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SentimentAnalysisCache_DirectReportId",
                table: "SentimentAnalysisCache",
                column: "DirectReportId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SentimentAnalysisCache");

            migrationBuilder.DropColumn(
                name: "ClaudeApiKey",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "SentimentAnalysisDays",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "SentimentAnalysisEnabled",
                table: "AppSettings");
        }
    }
}
