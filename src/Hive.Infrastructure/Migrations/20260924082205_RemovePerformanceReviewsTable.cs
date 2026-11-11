using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovePerformanceReviewsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PerformanceReviews");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PerformanceReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AreasForImprovement = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DirectReportId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ManagerNotes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Rating = table.Column<int>(type: "INTEGER", nullable: false),
                    ReviewDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReviewPeriod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Strengths = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceReviews", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceReviews_DirectReportId",
                table: "PerformanceReviews",
                column: "DirectReportId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceReviews_DirectReportId_ReviewPeriod",
                table: "PerformanceReviews",
                columns: new[] { "DirectReportId", "ReviewPeriod" },
                unique: true);
        }
    }
}
