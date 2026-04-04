using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gloss.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMergeRequestsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "merge_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RepositoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderIid = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SourceBranch = table.Column<string>(type: "text", nullable: false),
                    TargetBranch = table.Column<string>(type: "text", nullable: false),
                    AuthorUsername = table.Column<string>(type: "text", nullable: false),
                    Diff = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_merge_requests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_merge_requests_RepositoryId_ProviderIid",
                table: "merge_requests",
                columns: ["RepositoryId", "ProviderIid"],
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "merge_requests");
        }
    }
}
