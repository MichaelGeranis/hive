using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuarterlyPlanning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Allocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InitiativeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DirectReportId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SprintId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Allocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InitiativeDependencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DependentInitiativeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DependencyInitiativeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InitiativeDependencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Initiatives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    QuarterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    OkrObjective = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Initiatives", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Quarters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    QuarterNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    OkrReference = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quarters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SprintGoals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    QuarterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SprintId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Goal = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SprintGoals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Allocations_DirectReportId",
                table: "Allocations",
                column: "DirectReportId");

            migrationBuilder.CreateIndex(
                name: "IX_Allocations_InitiativeId",
                table: "Allocations",
                column: "InitiativeId");

            migrationBuilder.CreateIndex(
                name: "IX_Allocations_InitiativeId_DirectReportId_SprintId",
                table: "Allocations",
                columns: new[] { "InitiativeId", "DirectReportId", "SprintId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Allocations_SprintId",
                table: "Allocations",
                column: "SprintId");

            migrationBuilder.CreateIndex(
                name: "IX_InitiativeDependencies_DependencyInitiativeId",
                table: "InitiativeDependencies",
                column: "DependencyInitiativeId");

            migrationBuilder.CreateIndex(
                name: "IX_InitiativeDependencies_DependentInitiativeId",
                table: "InitiativeDependencies",
                column: "DependentInitiativeId");

            migrationBuilder.CreateIndex(
                name: "IX_InitiativeDependencies_DependentInitiativeId_DependencyInitiativeId",
                table: "InitiativeDependencies",
                columns: new[] { "DependentInitiativeId", "DependencyInitiativeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Initiatives_Priority",
                table: "Initiatives",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_Initiatives_ProjectId",
                table: "Initiatives",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Initiatives_QuarterId",
                table: "Initiatives",
                column: "QuarterId");

            migrationBuilder.CreateIndex(
                name: "IX_Initiatives_Status",
                table: "Initiatives",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Quarters_Status",
                table: "Quarters",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Quarters_Year_QuarterNumber",
                table: "Quarters",
                columns: new[] { "Year", "QuarterNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SprintGoals_QuarterId",
                table: "SprintGoals",
                column: "QuarterId");

            migrationBuilder.CreateIndex(
                name: "IX_SprintGoals_QuarterId_SprintId",
                table: "SprintGoals",
                columns: new[] { "QuarterId", "SprintId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SprintGoals_SprintId",
                table: "SprintGoals",
                column: "SprintId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Allocations");

            migrationBuilder.DropTable(
                name: "InitiativeDependencies");

            migrationBuilder.DropTable(
                name: "Initiatives");

            migrationBuilder.DropTable(
                name: "Quarters");

            migrationBuilder.DropTable(
                name: "SprintGoals");
        }
    }
}
