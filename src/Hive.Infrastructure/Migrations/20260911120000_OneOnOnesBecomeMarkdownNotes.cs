using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations
{
    /// <summary>
    /// A 1:1 becomes a single markdown note. The agenda and the notes taken inside each
    /// meeting are folded into one body before the MeetingNotes table is dropped, so no
    /// writing is lost.
    /// </summary>
    /// <inheritdoc />
    public partial class OneOnOnesBecomeMarkdownNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "OneOnOneMeetings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "OneOnOneMeetings",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "OneOnOneMeetings",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            // The body is the agenda followed by every note taken in that meeting, oldest
            // first, each as a markdown bullet.
            migrationBuilder.Sql(@"
                UPDATE OneOnOneMeetings
                SET Content = TRIM(
                    COALESCE(NULLIF(TRIM(Agenda), ''), '') ||
                    CASE
                        WHEN (SELECT COUNT(*) FROM MeetingNotes WHERE MeetingId = OneOnOneMeetings.Id) = 0 THEN ''
                        WHEN TRIM(COALESCE(Agenda, '')) = '' THEN
                            (SELECT GROUP_CONCAT('- ' || Content, char(10))
                             FROM (SELECT Content FROM MeetingNotes WHERE MeetingId = OneOnOneMeetings.Id ORDER BY CreatedAt))
                        ELSE char(10) || char(10) ||
                            (SELECT GROUP_CONCAT('- ' || Content, char(10))
                             FROM (SELECT Content FROM MeetingNotes WHERE MeetingId = OneOnOneMeetings.Id ORDER BY CreatedAt))
                    END
                );");

            // The title is the first line of the body with its markdown marker removed.
            // It is re-derived properly the next time the note is saved.
            migrationBuilder.Sql(@"
                UPDATE OneOnOneMeetings
                SET Title = COALESCE(
                    NULLIF(
                        TRIM(
                            LTRIM(
                                CASE
                                    WHEN INSTR(Content, char(10)) > 0
                                        THEN SUBSTR(Content, 1, INSTR(Content, char(10)) - 1)
                                    ELSE Content
                                END,
                                '#>-* '
                            )
                        ),
                        ''
                    ),
                    'New 1:1');");

            // Seed each meeting's tags with the person it was already linked to, so the
            // tag that now carries the link matches the link the row already had.
            migrationBuilder.Sql(@"
                UPDATE OneOnOneMeetings
                SET Tags = COALESCE(
                    (SELECT LOWER(
                                REPLACE(REPLACE(REPLACE(REPLACE(dr.FirstName || dr.LastName, ' ', ''), '-', ''), '''', ''), '.', '')
                            )
                     FROM DirectReports dr
                     WHERE dr.Id = OneOnOneMeetings.DirectReportId),
                    '');");

            migrationBuilder.DropTable(
                name: "MeetingNotes");

            migrationBuilder.DropColumn(
                name: "Agenda",
                table: "OneOnOneMeetings");

            migrationBuilder.DropColumn(
                name: "IsSyncedFromCalendar",
                table: "OneOnOneMeetings");

            // A 1:1 whose tags name nobody is kept, unlinked, rather than lost.
            migrationBuilder.AlterColumn<Guid>(
                name: "DirectReportId",
                table: "OneOnOneMeetings",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "DirectReportId",
                table: "OneOnOneMeetings",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Agenda",
                table: "OneOnOneMeetings",
                type: "TEXT",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsSyncedFromCalendar",
                table: "OneOnOneMeetings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // The body survives as the agenda; it was one block of text either way.
            migrationBuilder.Sql("UPDATE OneOnOneMeetings SET Agenda = SUBSTR(Content, 1, 4000);");

            migrationBuilder.CreateTable(
                name: "MeetingNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MeetingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    ActionStatus = table.Column<int>(type: "INTEGER", nullable: true),
                    ActionDueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ActionAssignee = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingNotes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingNotes_MeetingId",
                table: "MeetingNotes",
                column: "MeetingId");

            migrationBuilder.DropColumn(
                name: "Content",
                table: "OneOnOneMeetings");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "OneOnOneMeetings");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "OneOnOneMeetings");
        }
    }
}
