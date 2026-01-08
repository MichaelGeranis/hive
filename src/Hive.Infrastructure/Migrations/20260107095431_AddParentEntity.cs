using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddParentEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentId",
                table: "TeamTasks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Parents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Labels = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeamTasks_ParentId",
                table: "TeamTasks",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Parents_Name",
                table: "Parents",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Parents");

            migrationBuilder.DropIndex(
                name: "IX_TeamTasks_ParentId",
                table: "TeamTasks");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "TeamTasks");
        }
    }
}
