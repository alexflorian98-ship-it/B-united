using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BUnited.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPreferenceLanguageAndBucharestDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill existing rows with the platform's default language rather than an empty
            // string — an empty PreferredLanguage would fail UpdateProfileValidator's
            // "supported language" rule the next time an existing account's profile is saved.
            migrationBuilder.AddColumn<string>(
                name: "preferred_language",
                table: "user_preferences",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "ro");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "preferred_language",
                table: "user_preferences");
        }
    }
}
