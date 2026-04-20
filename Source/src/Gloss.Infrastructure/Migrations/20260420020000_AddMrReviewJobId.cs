using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gloss.Infrastructure.Migrations;

public partial class AddMrReviewJobId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ReviewJobId",
            table: "merge_requests",
            type: "text",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ReviewJobId",
            table: "merge_requests");
    }
}
