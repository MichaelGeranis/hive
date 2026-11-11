using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveNoteTodos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ManagerNotes_DueDate",
                table: "ManagerNotes");

            migrationBuilder.DropIndex(
                name: "IX_ManagerNotes_IsCompleted",
                table: "ManagerNotes");

            migrationBuilder.DropIndex(
                name: "IX_ManagerNotes_Priority",
                table: "ManagerNotes");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "ManagerNotes");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "ManagerNotes");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "ManagerNotes");

            migrationBuilder.DropColumn(
                name: "IsTodo",
                table: "ManagerNotes");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "ManagerNotes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "ManagerNotes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "ManagerNotes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
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

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "ManagerNotes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ManagerNotes_DueDate",
                table: "ManagerNotes",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerNotes_IsCompleted",
                table: "ManagerNotes",
                column: "IsCompleted");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerNotes_Priority",
                table: "ManagerNotes",
                column: "Priority");
        }
    }
}
