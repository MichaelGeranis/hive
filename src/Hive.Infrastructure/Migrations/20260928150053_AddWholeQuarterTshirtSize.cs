using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWholeQuarterTshirtSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // XXL, an option that takes up the whole quarter, joins the sizes a manager already
            // saved. Sizes they have are left as they are; a database with no saved sizes uses
            // the defaults, which include it.
            migrationBuilder.Sql("""
                UPDATE AppSettings
                SET TshirtSizeMappings = json_insert(
                    TshirtSizeMappings, '$[#]',
                    json('{"Size":"XXL","Sprints":12,"Label":""}'))
                WHERE TshirtSizeMappings <> ''
                  AND json_valid(TshirtSizeMappings)
                  AND json_type(TshirtSizeMappings) = 'array'
                  AND json_array_length(TshirtSizeMappings) > 0
                  AND NOT EXISTS (
                      SELECT 1 FROM json_each(TshirtSizeMappings)
                      WHERE upper(json_extract(value, '$.Size')) = 'XXL');
                """);

            // The only labels saved so far are the old defaults Hive wrote itself (Settings has no
            // way to type one), and they describe the old sprint counts, so they are cleared
            migrationBuilder.Sql("""
                UPDATE AppSettings
                SET TshirtSizeMappings =
                    replace(replace(replace(replace(replace(TshirtSizeMappings,
                        '"Label":"S = 1/2 sprint"', '"Label":""'),
                        '"Label":"M = 1 sprint"', '"Label":""'),
                        '"Label":"L = 2 sprints"', '"Label":""'),
                        '"Label":"XL = 4\u002B sprints"', '"Label":""'),
                        '"Label":"XL = 4+ sprints"', '"Label":""');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Sizes are the manager's to edit, so an XXL may be theirs: it is left in place
        }
    }
}
