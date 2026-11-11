using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTimeDurationFromMeetings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert MeetingDate from datetime format to date-only format
            migrationBuilder.Sql(
                "UPDATE OneOnOneMeetings SET MeetingDate = date(MeetingDate)");

            migrationBuilder.DropColumn(
                name: "DurationMinutes",
                table: "OneOnOneMeetings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DurationMinutes",
                table: "OneOnOneMeetings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
