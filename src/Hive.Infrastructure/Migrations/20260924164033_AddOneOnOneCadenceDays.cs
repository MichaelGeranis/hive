using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOneOnOneCadenceDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OneOnOneCadenceDays",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 14);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OneOnOneCadenceDays",
                table: "AppSettings");
        }
    }
}
