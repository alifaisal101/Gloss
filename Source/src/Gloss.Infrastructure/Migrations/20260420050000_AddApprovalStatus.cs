using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gloss.Infrastructure.Migrations;

public partial class AddApprovalStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "IsApproved", table: "merge_requests");

        migrationBuilder.AddColumn<string>(
            name: "Approval",
            table: "merge_requests",
            type: "jsonb",
            nullable: false,
            defaultValue: "{\"type\":\"NotApproved\"}");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Approval", table: "merge_requests");

        migrationBuilder.AddColumn<bool>(
            name: "IsApproved",
            table: "merge_requests",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }
}
