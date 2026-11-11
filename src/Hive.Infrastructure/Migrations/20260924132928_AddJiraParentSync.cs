using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJiraParentSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JiraKey",
                table: "Parents",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JiraProjectKey",
                table: "AppSettings",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Parents_JiraKey",
                table: "Parents",
                column: "JiraKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Parents_JiraKey",
                table: "Parents");

            migrationBuilder.DropColumn(
                name: "JiraKey",
                table: "Parents");

            migrationBuilder.DropColumn(
                name: "JiraProjectKey",
                table: "AppSettings");
        }
    }
}
