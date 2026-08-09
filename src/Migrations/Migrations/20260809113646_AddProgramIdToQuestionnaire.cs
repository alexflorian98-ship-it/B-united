using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BUnited.Migrations.Migrations
{
    /// <summary>
    /// P3.42 — every <c>Questionnaire</c> now belongs to exactly one Content-owned program
    /// (<c>ProgramId</c>, a plain <c>Guid</c> with no FK constraint, matching
    /// <c>ProgramOffer.ProgramId</c>/<c>Purchase.ProgramId</c>'s cross-module boundary
    /// convention). Added directly as <c>NOT NULL</c> with no backfill: verified against the real
    /// local Postgres dev database before this migration was written that the "questionnaires"
    /// table (and every dependent table — questions, submissions, answers, guidance) contained
    /// zero rows, so there is no existing data to map to a program.
    /// </summary>
    public partial class AddProgramIdToQuestionnaire : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "program_id",
                table: "questionnaires",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "program_id",
                table: "questionnaires");
        }
    }
}
