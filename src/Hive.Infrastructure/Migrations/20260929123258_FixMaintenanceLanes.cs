using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixMaintenanceLanes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A standing lane (Kind 2) is Maintenance (0) or Tech roadmap (2), never Product roadmap
            // (1). The Maintenance lane was stored as Product roadmap while WorkType had a column
            // default and no sentinel: EF took 0 for "not set" and let the default fill it in.
            migrationBuilder.Sql("UPDATE Initiatives SET WorkType = 0 WHERE Kind = 2 AND WorkType = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The lanes were wrong before; nothing to put back
        }
    }
}
