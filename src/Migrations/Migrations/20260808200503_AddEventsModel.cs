using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BUnited.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddEventsModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    default_language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    starts_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    display_timezone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    location_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    location = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    meeting_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    capacity = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event_registrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    canceled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_registrations", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_registrations_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_translations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_translations", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_translations_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "event_reminders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_registration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    scheduled_for_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sent_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_reminders", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_reminders_event_registrations_event_registration_id",
                        column: x => x.event_registration_id,
                        principalTable: "event_registrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_reminders_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_event_registrations_event_id_status_created_at",
                table: "event_registrations",
                columns: new[] { "event_id", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_event_registrations_event_id_user_id",
                table: "event_registrations",
                columns: new[] { "event_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_registrations_user_id",
                table: "event_registrations",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_reminders_event_id",
                table: "event_reminders",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_reminders_event_registration_id_type",
                table: "event_reminders",
                columns: new[] { "event_registration_id", "type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_reminders_scheduled_for_utc_sent_at_utc",
                table: "event_reminders",
                columns: new[] { "scheduled_for_utc", "sent_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_event_translations_event_id_language",
                table: "event_translations",
                columns: new[] { "event_id", "language" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_events_starts_at_utc",
                table: "events",
                column: "starts_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_events_status",
                table: "events",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_reminders");

            migrationBuilder.DropTable(
                name: "event_translations");

            migrationBuilder.DropTable(
                name: "event_registrations");

            migrationBuilder.DropTable(
                name: "events");
        }
    }
}
