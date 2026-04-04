using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gloss.Infrastructure.Migrations
{
    public partial class AddMrCommits : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mr_commits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MergeRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sha = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    AuthorName = table.Column<string>(type: "text", nullable: false),
                    Diff = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mr_commits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mr_commits_merge_requests_MergeRequestId",
                        column: x => x.MergeRequestId,
                        principalTable: "merge_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_mr_commits_MergeRequestId",
                table: "mr_commits",
                column: "MergeRequestId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "mr_commits");
        }
    }
}
