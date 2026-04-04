using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gloss.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "configs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GitProvider = table.Column<string>(type: "text", nullable: false),
                    GitBaseUrl = table.Column<string>(type: "text", nullable: false),
                    GitToken = table.Column<string>(type: "text", nullable: false),
                    GitProjects = table.Column<string[]>(type: "text[]", nullable: false),
                    LlmProvider = table.Column<string>(type: "text", nullable: false),
                    LlmApiKey = table.Column<string>(type: "text", nullable: false),
                    LlmModel = table.Column<string>(type: "text", nullable: false),
                    LlmReasoningEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultPollCron = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "configs");
        }
    }
}
