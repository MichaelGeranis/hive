using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSentimentProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OllamaBaseUrl",
                table: "AppSettings",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "http://localhost:11434");

            migrationBuilder.AddColumn<string>(
                name: "OllamaModel",
                table: "AppSettings",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "llama3.1:8b");

            migrationBuilder.AddColumn<string>(
                name: "SentimentProvider",
                table: "AppSettings",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "Ollama");

            // A manager who already set up Claude keeps it; everyone else starts on Ollama
            migrationBuilder.Sql(
                "UPDATE AppSettings SET SentimentProvider = 'Claude' " +
                "WHERE ClaudeApiKey IS NOT NULL AND TRIM(ClaudeApiKey) <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OllamaBaseUrl",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "OllamaModel",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "SentimentProvider",
                table: "AppSettings");
        }
    }
}
