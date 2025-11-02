using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetakesAllocator.Migrations
{
    /// <inheritdoc />
    public partial class AddEnemyStuffTeamPreferenceToUserSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EnemyStuffTeamPreference",
                table: "UserSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
UPDATE "UserSettings"
SET "EnemyStuffTeamPreference" = 3
WHERE "EnemyStuffEnabled" = 1
""");

            migrationBuilder.DropColumn(
                name: "EnemyStuffEnabled",
                table: "UserSettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnemyStuffEnabled",
                table: "UserSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
UPDATE "UserSettings"
SET "EnemyStuffEnabled" = CASE
    WHEN ("EnemyStuffTeamPreference" & 3) <> 0 THEN 1
    ELSE 0
END
""");

            migrationBuilder.DropColumn(
                name: "EnemyStuffTeamPreference",
                table: "UserSettings");
        }
    }
}
