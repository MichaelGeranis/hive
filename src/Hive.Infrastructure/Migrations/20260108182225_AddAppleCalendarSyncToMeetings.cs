using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppleCalendarSyncToMeetings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppleCalendarEventId",
                table: "OneOnOneMeetings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSyncedFromCalendar",
                table: "OneOnOneMeetings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AppleCalendarEventId",
                table: "OneOnOneMeetings");

            migrationBuilder.DropColumn(
                name: "IsSyncedFromCalendar",
                table: "OneOnOneMeetings");
        }
    }
}
