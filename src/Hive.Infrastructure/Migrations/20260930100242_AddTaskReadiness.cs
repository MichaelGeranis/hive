using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskReadiness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TaskReviewEnabled",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "TaskReadinessReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TeamTaskId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Verdict = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    MissingJson = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SourceFingerprint = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    AnalyzedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskReadinessReviews", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskReadinessReviews_TeamTaskId",
                table: "TaskReadinessReviews",
                column: "TeamTaskId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskReadinessReviews");

            migrationBuilder.DropColumn(
                name: "TaskReviewEnabled",
                table: "AppSettings");
        }
    }
}
