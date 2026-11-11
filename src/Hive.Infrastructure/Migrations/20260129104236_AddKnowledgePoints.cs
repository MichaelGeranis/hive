using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgePoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KnowledgePoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DirectReportId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ManualPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgePoints", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgePoints_DirectReportId",
                table: "KnowledgePoints",
                column: "DirectReportId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgePoints_DirectReportId_ProjectId",
                table: "KnowledgePoints",
                columns: new[] { "DirectReportId", "ProjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgePoints_ProjectId",
                table: "KnowledgePoints",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KnowledgePoints");
        }
    }
}
