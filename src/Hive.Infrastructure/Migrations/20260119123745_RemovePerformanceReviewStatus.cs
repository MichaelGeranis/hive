using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovePerformanceReviewStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcknowledgedAt",
                table: "PerformanceReviews");

            migrationBuilder.DropColumn(
                name: "EmployeeSelfAssessment",
                table: "PerformanceReviews");

            migrationBuilder.DropColumn(
                name: "GoalsForNextPeriod",
                table: "PerformanceReviews");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "PerformanceReviews");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "PerformanceReviews");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AcknowledgedAt",
                table: "PerformanceReviews",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployeeSelfAssessment",
                table: "PerformanceReviews",
                type: "TEXT",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GoalsForNextPeriod",
                table: "PerformanceReviews",
                type: "TEXT",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "PerformanceReviews",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "PerformanceReviews",
                type: "TEXT",
                nullable: true);
        }
    }
}
