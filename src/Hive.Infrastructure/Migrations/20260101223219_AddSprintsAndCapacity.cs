using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSprintsAndCapacity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SprintCapacities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SprintId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TotalCapacityPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    AvailableMembers = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SprintCapacities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sprints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TeamName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Quarter = table.Column<int>(type: "INTEGER", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    SprintNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sprints", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SprintCapacities_SprintId",
                table: "SprintCapacities",
                column: "SprintId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sprints_Name",
                table: "Sprints",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sprints_TeamName",
                table: "Sprints",
                column: "TeamName");

            migrationBuilder.CreateIndex(
                name: "IX_Sprints_Year_Quarter",
                table: "Sprints",
                columns: new[] { "Year", "Quarter" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SprintCapacities");

            migrationBuilder.DropTable(
                name: "Sprints");
        }
    }
}
