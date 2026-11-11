using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyCareerPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CareerPlans_DirectReportId",
                table: "CareerPlans");

            migrationBuilder.DropIndex(
                name: "IX_CareerPlans_Status",
                table: "CareerPlans");

            migrationBuilder.DropColumn(
                name: "TargetReachedAt",
                table: "CareerPlanSkills");

            migrationBuilder.DropColumn(
                name: "AchievedAt",
                table: "CareerPlans");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CareerPlans");

            migrationBuilder.DropColumn(
                name: "TargetDate",
                table: "CareerPlans");

            migrationBuilder.DropColumn(
                name: "TargetRole",
                table: "CareerPlans");

            migrationBuilder.DropColumn(
                name: "TargetLevel",
                table: "CareerPlanSkills");

            migrationBuilder.AddColumn<bool>(
                name: "IsFocus",
                table: "CareerPlanSkills",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.RenameColumn(
                name: "Stage",
                table: "CareerPlans",
                newName: "CurrentStage");

            migrationBuilder.AddColumn<int>(
                name: "FromStage",
                table: "CareerPlanSkills",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Skills already on a plan take the stage their roadmap skill is expected from
            migrationBuilder.Sql(
                """
                UPDATE "CareerPlanSkills"
                SET "FromStage" = COALESCE((
                    SELECT rs."FromStage" FROM "RoadmapSkills" rs
                    JOIN "SkillRoadmaps" r ON rs."RoadmapId" = r."Id"
                    JOIN "CareerPlans" p ON p."RoadmapName" = r."Name"
                    WHERE p."Id" = "CareerPlanSkills"."CareerPlanId"
                      AND lower(rs."Title") = lower("CareerPlanSkills"."Title")
                    LIMIT 1), 0);
                """);

            // A direct report now has one plan: keep their newest, drop the rest with their skills
            migrationBuilder.Sql(
                """
                DELETE FROM "CareerPlans"
                WHERE "Id" NOT IN (
                    SELECT p."Id" FROM "CareerPlans" p
                    WHERE p."CreatedAt" = (
                        SELECT MAX(q."CreatedAt") FROM "CareerPlans" q WHERE q."DirectReportId" = p."DirectReportId"));
                DELETE FROM "CareerPlanSkills" WHERE "CareerPlanId" NOT IN (SELECT "Id" FROM "CareerPlans");
                """);

            migrationBuilder.CreateIndex(
                name: "IX_CareerPlans_DirectReportId",
                table: "CareerPlans",
                column: "DirectReportId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CareerPlans_DirectReportId",
                table: "CareerPlans");

            migrationBuilder.DropColumn(
                name: "FromStage",
                table: "CareerPlanSkills");

            migrationBuilder.DropColumn(
                name: "IsFocus",
                table: "CareerPlanSkills");

            migrationBuilder.AddColumn<int>(
                name: "TargetLevel",
                table: "CareerPlanSkills",
                type: "INTEGER",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.RenameColumn(
                name: "CurrentStage",
                table: "CareerPlans",
                newName: "Stage");

            migrationBuilder.AddColumn<DateTime>(
                name: "TargetReachedAt",
                table: "CareerPlanSkills",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AchievedAt",
                table: "CareerPlans",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "CareerPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "TargetDate",
                table: "CareerPlans",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetRole",
                table: "CareerPlans",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_CareerPlans_DirectReportId",
                table: "CareerPlans",
                column: "DirectReportId");

            migrationBuilder.CreateIndex(
                name: "IX_CareerPlans_Status",
                table: "CareerPlans",
                column: "Status");
        }
    }
}
