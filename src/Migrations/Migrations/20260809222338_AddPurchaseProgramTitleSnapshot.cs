using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BUnited.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseProgramTitleSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "program_title_snapshot",
                table: "purchases",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            // Backfill: docs/IMPLEMENTATION_PLAN.md Slice A5 — every purchase created before this
            // column existed gets its title filled in from the program's current default-language
            // translation. This is a one-time, best-effort backfill (a schema-level data fix, not
            // application business logic — modules still never read each other's Domain/
            // Infrastructure layers at runtime): a purchase whose program/translation no longer
            // exists is left NULL, same as a purchase created for a program with no resolvable
            // title would be going forward.
            migrationBuilder.Sql(
                """
                UPDATE purchases
                SET program_title_snapshot = pt.title
                FROM programs p
                JOIN program_translations pt ON pt.program_id = p.id AND pt.language = p.default_language
                WHERE purchases.program_id = p.id
                  AND purchases.program_title_snapshot IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "program_title_snapshot",
                table: "purchases");
        }
    }
}
