using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillCategoryEntity : Migration
    {
        // These are the deterministic GUIDs matching what's seeded in HiveDbContext
        private static readonly Guid TechnicalCategoryId = new Guid("10000000-0000-0000-0000-000000000001");
        private static readonly Guid SoftSkillsCategoryId = new Guid("10000000-0000-0000-0000-000000000002");
        private static readonly Guid LeadershipCategoryId = new Guid("10000000-0000-0000-0000-000000000003");
        private static readonly Guid DomainKnowledgeCategoryId = new Guid("10000000-0000-0000-0000-000000000004");
        private static readonly Guid ToolsCategoryId = new Guid("10000000-0000-0000-0000-000000000005");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create the SkillCategories table first
            migrationBuilder.CreateTable(
                name: "SkillCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillCategories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SkillCategories_Name",
                table: "SkillCategories",
                column: "Name",
                unique: true);

            // 2. Seed the default categories (matching the enum values: Technical=0, SoftSkills=1, Leadership=2, DomainKnowledge=3, Tools=4)
            var now = DateTime.UtcNow.ToString("o");
            migrationBuilder.Sql($@"
                INSERT INTO SkillCategories (Id, Name, Description, SortOrder, IsActive, CreatedAt, UpdatedAt)
                VALUES
                    ('{TechnicalCategoryId}', 'Technical', 'Technical skills and programming knowledge', 0, 1, '{now}', NULL),
                    ('{SoftSkillsCategoryId}', 'Soft Skills', 'Communication and interpersonal skills', 1, 1, '{now}', NULL),
                    ('{LeadershipCategoryId}', 'Leadership', 'Leadership and management skills', 2, 1, '{now}', NULL),
                    ('{DomainKnowledgeCategoryId}', 'Domain Knowledge', 'Industry and domain-specific knowledge', 3, 1, '{now}', NULL),
                    ('{ToolsCategoryId}', 'Tools', 'Tools and technologies proficiency', 4, 1, '{now}', NULL);
            ");

            // 3. Add the new SkillCategoryId column with a default (Technical category)
            migrationBuilder.AddColumn<Guid>(
                name: "SkillCategoryId",
                table: "Skills",
                type: "TEXT",
                nullable: false,
                defaultValue: TechnicalCategoryId);

            // 4. Migrate existing skills from old Category (int enum) to new SkillCategoryId (Guid)
            // Map: Technical=0, SoftSkills=1, Leadership=2, DomainKnowledge=3, Tools=4
            migrationBuilder.Sql($@"
                UPDATE Skills SET SkillCategoryId = '{TechnicalCategoryId}' WHERE Category = 0;
                UPDATE Skills SET SkillCategoryId = '{SoftSkillsCategoryId}' WHERE Category = 1;
                UPDATE Skills SET SkillCategoryId = '{LeadershipCategoryId}' WHERE Category = 2;
                UPDATE Skills SET SkillCategoryId = '{DomainKnowledgeCategoryId}' WHERE Category = 3;
                UPDATE Skills SET SkillCategoryId = '{ToolsCategoryId}' WHERE Category = 4;
            ");

            // 5. Drop the old Category column
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Skills");

            // 6. Create index on SkillCategoryId
            migrationBuilder.CreateIndex(
                name: "IX_Skills_SkillCategoryId",
                table: "Skills",
                column: "SkillCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Add back the Category column
            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "Skills",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // 2. Migrate data back from SkillCategoryId to Category
            migrationBuilder.Sql($@"
                UPDATE Skills SET Category = 0 WHERE SkillCategoryId = '{TechnicalCategoryId}';
                UPDATE Skills SET Category = 1 WHERE SkillCategoryId = '{SoftSkillsCategoryId}';
                UPDATE Skills SET Category = 2 WHERE SkillCategoryId = '{LeadershipCategoryId}';
                UPDATE Skills SET Category = 3 WHERE SkillCategoryId = '{DomainKnowledgeCategoryId}';
                UPDATE Skills SET Category = 4 WHERE SkillCategoryId = '{ToolsCategoryId}';
            ");

            // 3. Drop the index and column
            migrationBuilder.DropIndex(
                name: "IX_Skills_SkillCategoryId",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "SkillCategoryId",
                table: "Skills");

            // 4. Drop the SkillCategories table
            migrationBuilder.DropTable(
                name: "SkillCategories");
        }
    }
}
