using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIsPrivateAndSimplifyCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert old category values to new simplified categories:
            // Old: Discussion=0, ActionItem=1, Feedback=2, CareerDevelopment=3, Blocker=4, Achievement=5, Personal=6, FollowUp=7, Agenda=8
            // New: Discussion=0, ActionItem=1, Feedback=2, Achievement=3
            // Convert old Achievement (5) to new Achievement (3)
            migrationBuilder.Sql("UPDATE MeetingNotes SET Category = 3 WHERE Category = 5");
            // Convert removed categories (3,4,6,7,8) to Discussion (0)
            migrationBuilder.Sql("UPDATE MeetingNotes SET Category = 0 WHERE Category IN (3, 4, 6, 7, 8)");

            migrationBuilder.DropColumn(
                name: "IsPrivate",
                table: "MeetingNotes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPrivate",
                table: "MeetingNotes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
