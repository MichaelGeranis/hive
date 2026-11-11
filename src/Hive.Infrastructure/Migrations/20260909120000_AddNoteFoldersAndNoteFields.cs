using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNoteFoldersAndNoteFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "ManagerNotes",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 4000);

            migrationBuilder.AddColumn<Guid>(
                name: "FolderId",
                table: "ManagerNotes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPinned",
                table: "ManagerNotes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTodo",
                table: "ManagerNotes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // Every note that existed before folders were introduced was a TODO.
            migrationBuilder.Sql("UPDATE ManagerNotes SET IsTodo = 1;");

            migrationBuilder.CreateTable(
                name: "NoteFolders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ParentFolderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoteFolders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManagerNotes_FolderId",
                table: "ManagerNotes",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerNotes_IsPinned",
                table: "ManagerNotes",
                column: "IsPinned");

            migrationBuilder.CreateIndex(
                name: "IX_NoteFolders_ParentFolderId",
                table: "NoteFolders",
                column: "ParentFolderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NoteFolders");

            migrationBuilder.DropIndex(
                name: "IX_ManagerNotes_FolderId",
                table: "ManagerNotes");

            migrationBuilder.DropIndex(
                name: "IX_ManagerNotes_IsPinned",
                table: "ManagerNotes");

            migrationBuilder.DropColumn(
                name: "FolderId",
                table: "ManagerNotes");

            migrationBuilder.DropColumn(
                name: "IsPinned",
                table: "ManagerNotes");

            migrationBuilder.DropColumn(
                name: "IsTodo",
                table: "ManagerNotes");

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "ManagerNotes",
                type: "TEXT",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }
    }
}
