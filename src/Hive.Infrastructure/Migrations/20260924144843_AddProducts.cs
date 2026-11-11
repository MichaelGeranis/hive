using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KnowledgePoints");

            migrationBuilder.DropColumn(
                name: "Labels",
                table: "Projects");

            migrationBuilder.RenameColumn(
                name: "MinProjectMembers",
                table: "AppSettings",
                newName: "MinProductMembers");

            migrationBuilder.AddColumn<string>(
                name: "Components",
                table: "Parents",
                type: "TEXT",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    JiraComponentId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectProducts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_JiraComponentId",
                table: "Products",
                column: "JiraComponentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name",
                table: "Products",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectProducts_ProductId",
                table: "ProjectProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectProducts_ProjectId",
                table: "ProjectProducts",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectProducts_ProjectId_ProductId",
                table: "ProjectProducts",
                columns: new[] { "ProjectId", "ProductId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "ProjectProducts");

            migrationBuilder.DropColumn(
                name: "Components",
                table: "Parents");

            migrationBuilder.RenameColumn(
                name: "MinProductMembers",
                table: "AppSettings",
                newName: "MinProjectMembers");

            migrationBuilder.AddColumn<string>(
                name: "Labels",
                table: "Projects",
                type: "TEXT",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "KnowledgePoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DirectReportId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ManualPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
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
    }
}
