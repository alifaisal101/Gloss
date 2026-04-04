using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gloss.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCascadeForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_draft_comments_MergeRequestId",
                table: "draft_comments");

            migrationBuilder.AddForeignKey(
                name: "FK_merge_requests_repositories_RepositoryId",
                table: "merge_requests",
                column: "RepositoryId",
                principalTable: "repositories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_draft_comments_merge_requests_MergeRequestId",
                table: "draft_comments",
                column: "MergeRequestId",
                principalTable: "merge_requests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_merge_requests_repositories_RepositoryId",
                table: "merge_requests");

            migrationBuilder.DropForeignKey(
                name: "FK_draft_comments_merge_requests_MergeRequestId",
                table: "draft_comments");

            migrationBuilder.CreateIndex(
                name: "IX_draft_comments_MergeRequestId",
                table: "draft_comments",
                column: "MergeRequestId");
        }
    }
}
