using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gloss.Infrastructure.Migrations;

public partial class AddRichMrStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "State", table: "merge_requests");

        migrationBuilder.AddColumn<string>(
            name: "Status",
            table: "merge_requests",
            type: "jsonb",
            nullable: false,
            defaultValue: "{\"type\":\"Pending\",\"detectedAt\":\"2000-01-01T00:00:00+00:00\"}");

        migrationBuilder.AddColumn<string>(
            name: "PlatformStatus",
            table: "merge_requests",
            type: "jsonb",
            nullable: false,
            defaultValue: "{\"type\":\"Open\"}");

        migrationBuilder.AddColumn<bool>(
            name: "IsApproved",
            table: "merge_requests",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Status", table: "merge_requests");
        migrationBuilder.DropColumn(name: "PlatformStatus", table: "merge_requests");
        migrationBuilder.DropColumn(name: "IsApproved", table: "merge_requests");

        migrationBuilder.AddColumn<int>(
            name: "State",
            table: "merge_requests",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }
}
