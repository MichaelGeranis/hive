using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SizeOptionsWhenReady : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TshirtSize",
                table: "Initiatives",
                type: "TEXT",
                maxLength: 8,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 8);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TshirtSize",
                table: "Initiatives",
                type: "TEXT",
                maxLength: 8,
                nullable: false,
                defaultValue: "M",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 8,
                oldNullable: true);
        }
    }
}
