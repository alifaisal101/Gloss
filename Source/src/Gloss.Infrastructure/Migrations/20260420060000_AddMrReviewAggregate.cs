using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gloss.Infrastructure.Migrations;

public partial class AddMrReviewAggregate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "mr_reviews",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MergeRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Status = table.Column<string>(type: "jsonb", nullable: false,
                    defaultValue: "{\"type\":\"Pending\",\"detectedAt\":\"2000-01-01T00:00:00+00:00\"}"),
                ReviewJobId = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_mr_reviews", x => x.Id);
                table.ForeignKey(
                    name: "FK_mr_reviews_merge_requests_MergeRequestId",
                    column: x => x.MergeRequestId,
                    principalTable: "merge_requests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_mr_reviews_MergeRequestId_UserId",
            table: "mr_reviews",
            columns: new[] { "MergeRequestId", "UserId" },
            unique: true);

        migrationBuilder.Sql("""
            INSERT INTO mr_reviews ("Id", "MergeRequestId", "UserId", "Status", "ReviewJobId")
            SELECT gen_random_uuid(), "Id", '00000000-0000-0000-0000-000000000000'::uuid, "Status", "ReviewJobId"
            FROM merge_requests;
            """);

        migrationBuilder.AddColumn<Guid>(
            name: "MrReviewId",
            table: "draft_comments",
            type: "uuid",
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE draft_comments dc
            SET "MrReviewId" = mr."Id"
            FROM mr_reviews mr
            WHERE mr."MergeRequestId" = dc."MergeRequestId";
            """);

        migrationBuilder.AlterColumn<Guid>(
            name: "MrReviewId",
            table: "draft_comments",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.DropForeignKey(
            name: "FK_draft_comments_merge_requests_MergeRequestId",
            table: "draft_comments");

        migrationBuilder.DropColumn(name: "MergeRequestId", table: "draft_comments");

        migrationBuilder.AddForeignKey(
            name: "FK_draft_comments_mr_reviews_MrReviewId",
            table: "draft_comments",
            column: "MrReviewId",
            principalTable: "mr_reviews",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.CreateIndex(
            name: "IX_draft_comments_MrReviewId",
            table: "draft_comments",
            column: "MrReviewId");

        migrationBuilder.DropColumn(name: "Status", table: "merge_requests");
        migrationBuilder.DropColumn(name: "ReviewJobId", table: "merge_requests");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Status",
            table: "merge_requests",
            type: "jsonb",
            nullable: false,
            defaultValue: "{\"type\":\"Pending\",\"detectedAt\":\"2000-01-01T00:00:00+00:00\"}");

        migrationBuilder.AddColumn<string>(
            name: "ReviewJobId",
            table: "merge_requests",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "MergeRequestId",
            table: "draft_comments",
            type: "uuid",
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE draft_comments dc
            SET "MergeRequestId" = mr."MergeRequestId"
            FROM mr_reviews mr
            WHERE mr."Id" = dc."MrReviewId";
            """);

        migrationBuilder.AlterColumn<Guid>(
            name: "MergeRequestId",
            table: "draft_comments",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.DropForeignKey(name: "FK_draft_comments_mr_reviews_MrReviewId", table: "draft_comments");
        migrationBuilder.DropIndex(name: "IX_draft_comments_MrReviewId", table: "draft_comments");
        migrationBuilder.DropColumn(name: "MrReviewId", table: "draft_comments");
        migrationBuilder.DropTable(name: "mr_reviews");
    }
}
