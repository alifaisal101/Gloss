using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Gloss.Infrastructure.Migrations;

public partial class AddEventStoreTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "event_store",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                StreamId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                EventType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Position = table.Column<long>(type: "bigint", nullable: false),
                GlobalPosition = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                Payload = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_event_store", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_event_store_EventType",
            table: "event_store",
            column: "EventType");

        migrationBuilder.CreateIndex(
            name: "IX_event_store_OccurredAt",
            table: "event_store",
            column: "OccurredAt");

        migrationBuilder.CreateIndex(
            name: "IX_event_store_Payload",
            table: "event_store",
            column: "Payload")
            .Annotation("Npgsql:IndexMethod", "gin");

        migrationBuilder.CreateIndex(
            name: "IX_event_store_StreamId_Position",
            table: "event_store",
            columns: new[] { "StreamId", "Position" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "event_store");
    }
}
