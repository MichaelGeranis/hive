using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveInitiativeStatusPriorityOkr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Initiatives_Priority",
                table: "Initiatives");

            migrationBuilder.DropIndex(
                name: "IX_Initiatives_Status",
                table: "Initiatives");

            migrationBuilder.DropColumn(
                name: "OkrObjective",
                table: "Initiatives");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Initiatives");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Initiatives");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OkrObjective",
                table: "Initiatives",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "Initiatives",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Initiatives",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Initiatives_Priority",
                table: "Initiatives",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_Initiatives_Status",
                table: "Initiatives",
                column: "Status");
        }
    }
}
