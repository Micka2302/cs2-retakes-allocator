using System;
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
            if (ActiveProvider.Contains("MySql", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql(
                    """
ALTER TABLE `UserSettings`
ADD COLUMN IF NOT EXISTS `EnemyStuffTeamPreference` INT NOT NULL DEFAULT 0
""");
            }
            else
            {
                migrationBuilder.AddColumn<int>(
                    name: "EnemyStuffTeamPreference",
                    table: "UserSettings",
                    type: "INTEGER",
                    nullable: false,
                    defaultValue: 0);
            }

            migrationBuilder.Sql(
                ActiveProvider.Contains("MySql", StringComparison.OrdinalIgnoreCase)
                    ? """
UPDATE `UserSettings`
SET `EnemyStuffTeamPreference` = 3
WHERE `EnemyStuffEnabled` = 1
"""
                    : """
UPDATE "UserSettings"
SET "EnemyStuffTeamPreference" = 3
WHERE "EnemyStuffEnabled" = 1
""");

            if (ActiveProvider.Contains("MySql", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql(
                    """
ALTER TABLE `UserSettings`
DROP COLUMN IF EXISTS `EnemyStuffEnabled`
""");
            }
            else
            {
                migrationBuilder.DropColumn(
                    name: "EnemyStuffEnabled",
                    table: "UserSettings");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider.Contains("MySql", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql(
                    """
ALTER TABLE `UserSettings`
ADD COLUMN IF NOT EXISTS `EnemyStuffEnabled` TINYINT(1) NOT NULL DEFAULT 0
""");
            }
            else
            {
                migrationBuilder.AddColumn<bool>(
                    name: "EnemyStuffEnabled",
                    table: "UserSettings",
                    type: "INTEGER",
                    nullable: false,
                    defaultValue: false);
            }

            migrationBuilder.Sql(
                ActiveProvider.Contains("MySql", StringComparison.OrdinalIgnoreCase)
                    ? """
UPDATE `UserSettings`
SET `EnemyStuffEnabled` = CASE
    WHEN (`EnemyStuffTeamPreference` & 3) <> 0 THEN 1
    ELSE 0
END
"""
                    : """
UPDATE "UserSettings"
SET "EnemyStuffEnabled" = CASE
    WHEN ("EnemyStuffTeamPreference" & 3) <> 0 THEN 1
    ELSE 0
END
""");

            if (ActiveProvider.Contains("MySql", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql(
                    """
ALTER TABLE `UserSettings`
DROP COLUMN IF EXISTS `EnemyStuffTeamPreference`
""");
            }
            else
            {
                migrationBuilder.DropColumn(
                    name: "EnemyStuffTeamPreference",
                    table: "UserSettings");
            }
        }
    }
}
