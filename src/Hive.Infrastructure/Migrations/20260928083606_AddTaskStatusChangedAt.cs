using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskStatusChangedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "StatusChangedAt",
                table: "TeamTasks",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Hive never recorded when an existing task's status changed. The last time it
            // changed at all is the closest it knows, so staleness is never overstated.
            migrationBuilder.Sql(
                "UPDATE TeamTasks SET StatusChangedAt = COALESCE(UpdatedAt, CreatedAt)");

            migrationBuilder.AddColumn<int>(
                name: "StaleTaskDays",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 5);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StatusChangedAt",
                table: "TeamTasks");

            migrationBuilder.DropColumn(
                name: "StaleTaskDays",
                table: "AppSettings");
        }
    }
}
