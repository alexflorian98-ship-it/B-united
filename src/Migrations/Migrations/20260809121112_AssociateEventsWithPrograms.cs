using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BUnited.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AssociateEventsWithPrograms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Some development databases received this table while Slice 5 was being built,
            // before a migration-history row existed. Keep the forward migration safe for that
            // upgrade state while still creating the table on a clean database.
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS event_programs (
                    event_id uuid NOT NULL,
                    program_id uuid NOT NULL,
                    CONSTRAINT pk_event_programs PRIMARY KEY (event_id, program_id),
                    CONSTRAINT fk_event_programs_events_event_id
                        FOREIGN KEY (event_id) REFERENCES events (id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS ix_event_programs_program_id
                    ON event_programs (program_id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "event_programs");
        }
    }
}
