using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWorklogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedHours",
                table: "TeamTasks");

            migrationBuilder.DropColumn(
                name: "TimeSpentMinutes",
                table: "TeamTasks");

            migrationBuilder.DropColumn(
                name: "TimeSpentMinutes",
                table: "Parents");

            migrationBuilder.DropColumn(
                name: "StoryPointMappings",
                table: "AppSettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EstimatedHours",
                table: "TeamTasks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimeSpentMinutes",
                table: "TeamTasks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimeSpentMinutes",
                table: "Parents",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoryPointMappings",
                table: "AppSettings",
                type: "TEXT",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");
        }
    }
}
