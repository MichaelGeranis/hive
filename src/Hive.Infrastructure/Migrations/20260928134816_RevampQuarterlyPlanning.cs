using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RevampQuarterlyPlanning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Initiatives_ProjectId",
                table: "Initiatives");

            migrationBuilder.RenameColumn(
                name: "StartSprintId",
                table: "Initiatives",
                newName: "EarliestSprintId");

            // An initiative now names its products (InitiativeProducts) instead of one project
            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Initiatives");

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Initiatives",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Baseline",
                table: "Quarters",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CommittedAt",
                table: "Quarters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaintenancePercent",
                table: "Quarters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<int>(
                name: "ProductRoadmapPercent",
                table: "Quarters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 50);

            migrationBuilder.AddColumn<int>(
                name: "TechRoadmapPercent",
                table: "Quarters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 20);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArrivedOn",
                table: "Initiatives",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DecisionNote",
                table: "Initiatives",
                type: "TEXT",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "Initiatives",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Rank",
                table: "Initiatives",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Readiness",
                table: "Initiatives",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Initiatives",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "InitiativeMembers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Percent",
                table: "Allocations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.CreateTable(
                name: "ExternalDependencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InitiativeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Team = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Need = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalDependencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InitiativeProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InitiativeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InitiativeProducts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuarterMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    QuarterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DirectReportId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CapacityPercent = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuarterMembers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ValueIncrements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InitiativeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetSprintId = table.Column<Guid>(type: "TEXT", nullable: true),
                    JiraKey = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Outcome = table.Column<int>(type: "INTEGER", nullable: false),
                    OutcomeNote = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    OutcomeAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValueIncrements", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalDependencies_InitiativeId",
                table: "ExternalDependencies",
                column: "InitiativeId");

            migrationBuilder.CreateIndex(
                name: "IX_InitiativeProducts_InitiativeId_ProductId",
                table: "InitiativeProducts",
                columns: new[] { "InitiativeId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InitiativeProducts_ProductId",
                table: "InitiativeProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_QuarterMembers_QuarterId_DirectReportId",
                table: "QuarterMembers",
                columns: new[] { "QuarterId", "DirectReportId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ValueIncrements_InitiativeId",
                table: "ValueIncrements",
                column: "InitiativeId");

            migrationBuilder.CreateIndex(
                name: "IX_ValueIncrements_TargetSprintId",
                table: "ValueIncrements",
                column: "TargetSprintId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalDependencies");

            migrationBuilder.DropTable(
                name: "InitiativeProducts");

            migrationBuilder.DropTable(
                name: "QuarterMembers");

            migrationBuilder.DropTable(
                name: "ValueIncrements");

            migrationBuilder.DropColumn(
                name: "Baseline",
                table: "Quarters");

            migrationBuilder.DropColumn(
                name: "CommittedAt",
                table: "Quarters");

            migrationBuilder.DropColumn(
                name: "MaintenancePercent",
                table: "Quarters");

            migrationBuilder.DropColumn(
                name: "ProductRoadmapPercent",
                table: "Quarters");

            migrationBuilder.DropColumn(
                name: "TechRoadmapPercent",
                table: "Quarters");

            migrationBuilder.DropColumn(
                name: "ArrivedOn",
                table: "Initiatives");

            migrationBuilder.DropColumn(
                name: "DecisionNote",
                table: "Initiatives");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Initiatives");

            migrationBuilder.DropColumn(
                name: "Rank",
                table: "Initiatives");

            migrationBuilder.DropColumn(
                name: "Readiness",
                table: "Initiatives");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Initiatives");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "InitiativeMembers");

            migrationBuilder.DropColumn(
                name: "Percent",
                table: "Allocations");

            migrationBuilder.RenameColumn(
                name: "EarliestSprintId",
                table: "Initiatives",
                newName: "StartSprintId");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Initiatives");

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "Initiatives",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Initiatives_ProjectId",
                table: "Initiatives",
                column: "ProjectId");
        }
    }
}
