using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BUnited.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizContentModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "quiz_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_questions = table.Column<int>(type: "integer", nullable: false),
                    correct_count = table.Column<int>(type: "integer", nullable: false),
                    submitted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quiz_attempts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "quiz_questions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    content_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quiz_questions", x => x.id);
                    table.ForeignKey(
                        name: "fk_quiz_questions_content_items_content_item_id",
                        column: x => x.content_item_id,
                        principalTable: "content_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quiz_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    quiz_question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_correct = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quiz_options", x => x.id);
                    table.ForeignKey(
                        name: "fk_quiz_options__quiz_questions_quiz_question_id",
                        column: x => x.quiz_question_id,
                        principalTable: "quiz_questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quiz_question_translations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    quiz_question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quiz_question_translations", x => x.id);
                    table.ForeignKey(
                        name: "fk_quiz_question_translations_quiz_questions_quiz_question_id",
                        column: x => x.quiz_question_id,
                        principalTable: "quiz_questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quiz_option_translations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    quiz_option_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    label = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quiz_option_translations", x => x.id);
                    table.ForeignKey(
                        name: "fk_quiz_option_translations_quiz_options_quiz_option_id",
                        column: x => x.quiz_option_id,
                        principalTable: "quiz_options",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_quiz_attempts_user_id_content_item_id",
                table: "quiz_attempts",
                columns: new[] { "user_id", "content_item_id" });

            migrationBuilder.CreateIndex(
                name: "ix_quiz_option_translations_quiz_option_id_language",
                table: "quiz_option_translations",
                columns: new[] { "quiz_option_id", "language" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_quiz_options_quiz_question_id",
                table: "quiz_options",
                column: "quiz_question_id");

            migrationBuilder.CreateIndex(
                name: "ix_quiz_question_translations_quiz_question_id_language",
                table: "quiz_question_translations",
                columns: new[] { "quiz_question_id", "language" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_quiz_questions_content_item_id",
                table: "quiz_questions",
                column: "content_item_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quiz_attempts");

            migrationBuilder.DropTable(
                name: "quiz_option_translations");

            migrationBuilder.DropTable(
                name: "quiz_question_translations");

            migrationBuilder.DropTable(
                name: "quiz_options");

            migrationBuilder.DropTable(
                name: "quiz_questions");
        }
    }
}
