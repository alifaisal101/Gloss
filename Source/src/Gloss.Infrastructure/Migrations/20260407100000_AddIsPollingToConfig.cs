using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gloss.Infrastructure.Migrations
{
    public partial class AddIsPollingToConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPolling",
                table: "configs",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IsPolling", table: "configs");
        }
    }
}
