using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveValueIncrements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ValueIncrements");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ValueIncrements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    InitiativeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    JiraKey = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Outcome = table.Column<int>(type: "INTEGER", nullable: false),
                    OutcomeAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    OutcomeNote = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetSprintId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValueIncrements", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ValueIncrements_InitiativeId",
                table: "ValueIncrements",
                column: "InitiativeId");

            migrationBuilder.CreateIndex(
                name: "IX_ValueIncrements_TargetSprintId",
                table: "ValueIncrements",
                column: "TargetSprintId");
        }
    }
}
