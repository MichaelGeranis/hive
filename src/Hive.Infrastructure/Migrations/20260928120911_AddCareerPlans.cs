using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCareerPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CareerPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DirectReportId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RoadmapName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TargetRole = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TargetDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AchievedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CareerPlanSkills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CareerPlanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Section = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    IsOptional = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    Evidence = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    LastLevelUpAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TargetReachedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerPlanSkills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoadmapSkills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RoadmapId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Section = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    IsOptional = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadmapSkills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SkillRoadmaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    LibraryKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillRoadmaps", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CareerPlans_DirectReportId",
                table: "CareerPlans",
                column: "DirectReportId");

            migrationBuilder.CreateIndex(
                name: "IX_CareerPlans_Status",
                table: "CareerPlans",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CareerPlanSkills_CareerPlanId",
                table: "CareerPlanSkills",
                column: "CareerPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadmapSkills_RoadmapId",
                table: "RoadmapSkills",
                column: "RoadmapId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillRoadmaps_Name",
                table: "SkillRoadmaps",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CareerPlans");

            migrationBuilder.DropTable(
                name: "CareerPlanSkills");

            migrationBuilder.DropTable(
                name: "RoadmapSkills");

            migrationBuilder.DropTable(
                name: "SkillRoadmaps");
        }
    }
}
