using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkTypeStartSprintAndInitiativeMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "StartSprintId",
                table: "Initiatives",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkType",
                table: "Initiatives",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "InitiativeMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InitiativeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DirectReportId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InitiativeMembers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InitiativeMembers_DirectReportId",
                table: "InitiativeMembers",
                column: "DirectReportId");

            migrationBuilder.CreateIndex(
                name: "IX_InitiativeMembers_InitiativeId",
                table: "InitiativeMembers",
                column: "InitiativeId");

            migrationBuilder.CreateIndex(
                name: "IX_InitiativeMembers_InitiativeId_DirectReportId",
                table: "InitiativeMembers",
                columns: new[] { "InitiativeId", "DirectReportId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InitiativeMembers");

            migrationBuilder.DropColumn(
                name: "StartSprintId",
                table: "Initiatives");

            migrationBuilder.DropColumn(
                name: "WorkType",
                table: "Initiatives");
        }
    }
}
