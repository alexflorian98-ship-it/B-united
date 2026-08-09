using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BUnited.Migrations.Migrations
{
    /// <summary>
    /// docs/TASKS.md P3.43 — two independent schema changes landed together because they were
    /// scaffolded in the same session; there is no ordering dependency between them.
    ///
    /// P3.43.b (Events): adds the <c>event_programs</c> many-to-many join table. No data
    /// migration needed — it is a brand new table with no prior column to backfill from.
    ///
    /// P3.43.a (Chat): converts the fixed 6-member <c>ChatRoom</c> enum into a database-backed,
    /// program-scoped entity. Unlike the Questionnaires precedent (P3.42, which added
    /// <c>ProgramId</c> as a plain <c>NOT NULL</c> column with no backfill because that table was
    /// verified empty first), this migration runs against a database that already has real rows —
    /// 1 message and 6 <c>chat_read_states</c> rows — so it explicitly preserves them instead of
    /// dropping the column outright:
    /// <list type="number">
    /// <item>Creates <c>chat_rooms</c> and seeds the 6 legacy room identities (same names as the
    /// old enum members) as fixed, well-known GUIDs, <c>is_active = false</c>,
    /// <c>program_id = NULL</c> — the confirmed product decision to deactivate rather than invent
    /// a fake program association for them (see docs/HANDOVER.md and the plan this migration
    /// implements). A deactivated room is excluded from every client-facing discovery/read/post
    /// path, so a null <see cref="System.Guid"/>? <c>ProgramId</c> on these rows is inert.</item>
    /// <item>Adds nullable <c>room_id</c> columns to <c>messages</c>/<c>chat_read_states</c>,
    /// backfills them from the old <c>room</c> string column via the fixed legacy-room GUIDs
    /// above, then tightens to <c>NOT NULL</c> before dropping the old column — so no row's room
    /// association is ever lost.</item>
    /// </list>
    /// </summary>
    public partial class ConvertChatRoomToProgramOwnedEntity : Migration
    {
        private const string GeneralRoomId = "11111111-1111-1111-1111-111111111101";
        private const string PsychologyRoomId = "11111111-1111-1111-1111-111111111102";
        private const string SportRoomId = "11111111-1111-1111-1111-111111111103";
        private const string NutritionRoomId = "11111111-1111-1111-1111-111111111104";
        private const string BusinessRoomId = "11111111-1111-1111-1111-111111111105";
        private const string FinancialEducationRoomId = "11111111-1111-1111-1111-111111111106";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --- P3.43.b: Events <-> Program association (no data migration needed) -----------

            migrationBuilder.CreateTable(
                name: "event_programs",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    program_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_programs", x => new { x.event_id, x.program_id });
                    table.ForeignKey(
                        name: "fk_event_programs_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_event_programs_program_id",
                table: "event_programs",
                column: "program_id");

            // --- P3.43.a: ChatRoom enum -> program-owned entity (data-preserving) -------------

            migrationBuilder.CreateTable(
                name: "chat_rooms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    program_id = table.Column<Guid>(type: "uuid", nullable: true),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chat_rooms", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_chat_rooms_is_active",
                table: "chat_rooms",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_chat_rooms_key",
                table: "chat_rooms",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_chat_rooms_program_id",
                table: "chat_rooms",
                column: "program_id");

            // Seed the 6 legacy room identities as deactivated, unassociated rows — history that
            // referenced them (below) still resolves to a real chat_rooms.id, it is just never
            // reachable through client discovery/read/post again.
            migrationBuilder.InsertData(
                table: "chat_rooms",
                columns: new[] { "id", "program_id", "key", "name", "is_active", "created_at", "created_by", "updated_at", "updated_by" },
                values: new object[,]
                {
                    { new Guid(GeneralRoomId), null, "general", "General", false, DateTime.UtcNow, null, DateTime.UtcNow, null },
                    { new Guid(PsychologyRoomId), null, "psychology", "Psychology", false, DateTime.UtcNow, null, DateTime.UtcNow, null },
                    { new Guid(SportRoomId), null, "sport", "Sport", false, DateTime.UtcNow, null, DateTime.UtcNow, null },
                    { new Guid(NutritionRoomId), null, "nutrition", "Nutrition", false, DateTime.UtcNow, null, DateTime.UtcNow, null },
                    { new Guid(BusinessRoomId), null, "business", "Business", false, DateTime.UtcNow, null, DateTime.UtcNow, null },
                    { new Guid(FinancialEducationRoomId), null, "financial-education", "Financial Education", false, DateTime.UtcNow, null, DateTime.UtcNow, null },
                });

            // messages.room -> messages.room_id
            migrationBuilder.AddColumn<Guid>(
                name: "room_id",
                table: "messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql($"""
                UPDATE messages SET room_id = CASE room
                    WHEN 'General' THEN '{GeneralRoomId}'::uuid
                    WHEN 'Psychology' THEN '{PsychologyRoomId}'::uuid
                    WHEN 'Sport' THEN '{SportRoomId}'::uuid
                    WHEN 'Nutrition' THEN '{NutritionRoomId}'::uuid
                    WHEN 'Business' THEN '{BusinessRoomId}'::uuid
                    WHEN 'FinancialEducation' THEN '{FinancialEducationRoomId}'::uuid
                END;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "room_id",
                table: "messages",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "ix_messages_room_created_at",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "room",
                table: "messages");

            migrationBuilder.CreateIndex(
                name: "ix_messages_room_id_created_at",
                table: "messages",
                columns: new[] { "room_id", "created_at" });

            migrationBuilder.AddForeignKey(
                name: "fk_messages_chat_rooms_room_id",
                table: "messages",
                column: "room_id",
                principalTable: "chat_rooms",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            // chat_read_states.(user_id, room) -> chat_read_states.(user_id, room_id)
            migrationBuilder.AddColumn<Guid>(
                name: "room_id",
                table: "chat_read_states",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql($"""
                UPDATE chat_read_states SET room_id = CASE room
                    WHEN 'General' THEN '{GeneralRoomId}'::uuid
                    WHEN 'Psychology' THEN '{PsychologyRoomId}'::uuid
                    WHEN 'Sport' THEN '{SportRoomId}'::uuid
                    WHEN 'Nutrition' THEN '{NutritionRoomId}'::uuid
                    WHEN 'Business' THEN '{BusinessRoomId}'::uuid
                    WHEN 'FinancialEducation' THEN '{FinancialEducationRoomId}'::uuid
                END;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "room_id",
                table: "chat_read_states",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.DropPrimaryKey(
                name: "pk_chat_read_states",
                table: "chat_read_states");

            migrationBuilder.DropColumn(
                name: "room",
                table: "chat_read_states");

            migrationBuilder.AddPrimaryKey(
                name: "pk_chat_read_states",
                table: "chat_read_states",
                columns: new[] { "user_id", "room_id" });

            migrationBuilder.CreateIndex(
                name: "ix_chat_read_states_room_id",
                table: "chat_read_states",
                column: "room_id");

            migrationBuilder.AddForeignKey(
                name: "fk_chat_read_states__chat_rooms_room_id",
                table: "chat_read_states",
                column: "room_id",
                principalTable: "chat_rooms",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_chat_read_states__chat_rooms_room_id",
                table: "chat_read_states");

            migrationBuilder.DropForeignKey(
                name: "fk_messages_chat_rooms_room_id",
                table: "messages");

            migrationBuilder.DropTable(
                name: "event_programs");

            migrationBuilder.DropIndex(
                name: "ix_messages_room_id_created_at",
                table: "messages");

            migrationBuilder.AddColumn<string>(
                name: "room",
                table: "messages",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.Sql($"""
                UPDATE messages SET room = CASE room_id
                    WHEN '{GeneralRoomId}'::uuid THEN 'General'
                    WHEN '{PsychologyRoomId}'::uuid THEN 'Psychology'
                    WHEN '{SportRoomId}'::uuid THEN 'Sport'
                    WHEN '{NutritionRoomId}'::uuid THEN 'Nutrition'
                    WHEN '{BusinessRoomId}'::uuid THEN 'Business'
                    WHEN '{FinancialEducationRoomId}'::uuid THEN 'FinancialEducation'
                    ELSE 'General'
                END;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "room",
                table: "messages",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "room_id",
                table: "messages");

            migrationBuilder.DropPrimaryKey(
                name: "pk_chat_read_states",
                table: "chat_read_states");

            migrationBuilder.DropIndex(
                name: "ix_chat_read_states_room_id",
                table: "chat_read_states");

            migrationBuilder.AddColumn<string>(
                name: "room",
                table: "chat_read_states",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.Sql($"""
                UPDATE chat_read_states SET room = CASE room_id
                    WHEN '{GeneralRoomId}'::uuid THEN 'General'
                    WHEN '{PsychologyRoomId}'::uuid THEN 'Psychology'
                    WHEN '{SportRoomId}'::uuid THEN 'Sport'
                    WHEN '{NutritionRoomId}'::uuid THEN 'Nutrition'
                    WHEN '{BusinessRoomId}'::uuid THEN 'Business'
                    WHEN '{FinancialEducationRoomId}'::uuid THEN 'FinancialEducation'
                    ELSE 'General'
                END;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "room",
                table: "chat_read_states",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "room_id",
                table: "chat_read_states");

            migrationBuilder.AddPrimaryKey(
                name: "pk_chat_read_states",
                table: "chat_read_states",
                columns: new[] { "user_id", "room" });

            migrationBuilder.CreateIndex(
                name: "ix_messages_room_created_at",
                table: "messages",
                columns: new[] { "room", "created_at" });

            migrationBuilder.DropTable(
                name: "chat_rooms");
        }
    }
}
