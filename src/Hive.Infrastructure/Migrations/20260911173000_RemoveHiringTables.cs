using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations;

public partial class RemoveHiringTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ChecklistInstanceItems");
        migrationBuilder.DropTable(name: "ChecklistInstances");
        migrationBuilder.DropTable(name: "ChecklistTemplateItems");
        migrationBuilder.DropTable(name: "ChecklistTemplates");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ChecklistTemplates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                Type = table.Column<int>(type: "INTEGER", nullable: false),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_ChecklistTemplates", x => x.Id));

        migrationBuilder.CreateTable(
            name: "ChecklistInstances",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                TemplateId = table.Column<Guid>(type: "TEXT", nullable: false),
                Type = table.Column<int>(type: "INTEGER", nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                Status = table.Column<int>(type: "INTEGER", nullable: false),
                CandidateName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                Position = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                InterviewDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                NewHireName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                StartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                TargetCompletionDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                Notes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_ChecklistInstances", x => x.Id));

        migrationBuilder.CreateTable(
            name: "ChecklistTemplateItems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                TemplateId = table.Column<Guid>(type: "TEXT", nullable: false),
                SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                Content = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                ItemType = table.Column<int>(type: "INTEGER", nullable: false),
                IsRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                HelpText = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                EstimatedMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_ChecklistTemplateItems", x => x.Id));

        migrationBuilder.CreateTable(
            name: "ChecklistInstanceItems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                InstanceId = table.Column<Guid>(type: "TEXT", nullable: false),
                TemplateItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                Content = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                ItemType = table.Column<int>(type: "INTEGER", nullable: false),
                IsRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                Status = table.Column<int>(type: "INTEGER", nullable: false),
                Notes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                Score = table.Column<int>(type: "INTEGER", nullable: true),
                Assignee = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                DueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_ChecklistInstanceItems", x => x.Id));
    }
}
