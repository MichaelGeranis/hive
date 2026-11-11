using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ClearSizesBeforeReady : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // An option is sized once it is ready for commitment: those still in discovery (0) or
            // in solution design (1), and the standing lanes (Kind 2), lose the size they were given
            // before the rule. Its own migration, because the column only became nullable when the
            // previous one rebuilt the table
            migrationBuilder.Sql("UPDATE Initiatives SET TshirtSize = NULL WHERE Status IN (0, 1) OR Kind = 2;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The sizes cleared are not known any more
        }
    }
}
