using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJiraApiCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JiraApiToken",
                table: "AppSettings",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "JiraBoardId",
                table: "AppSettings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JiraEmail",
                table: "AppSettings",
                type: "TEXT",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JiraApiToken",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "JiraBoardId",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "JiraEmail",
                table: "AppSettings");
        }
    }
}
