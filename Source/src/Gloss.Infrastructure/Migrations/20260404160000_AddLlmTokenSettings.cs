using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gloss.Infrastructure.Migrations
{
    public partial class AddLlmTokenSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LlmMaxTokens",
                table: "configs",
                type: "integer",
                nullable: false,
                defaultValue: 16000);

            migrationBuilder.AddColumn<int>(
                name: "LlmThinkingBudget",
                table: "configs",
                type: "integer",
                nullable: false,
                defaultValue: 10000);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "LlmMaxTokens", table: "configs");
            migrationBuilder.DropColumn(name: "LlmThinkingBudget", table: "configs");
        }
    }
}
