using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gloss.Infrastructure.Migrations
{
    public partial class AddMrDiffShas : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BaseSha",
                table: "merge_requests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeadSha",
                table: "merge_requests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StartSha",
                table: "merge_requests",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "BaseSha", table: "merge_requests");
            migrationBuilder.DropColumn(name: "HeadSha", table: "merge_requests");
            migrationBuilder.DropColumn(name: "StartSha", table: "merge_requests");
        }
    }
}
