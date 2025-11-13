using System;
using Microsoft.EntityFrameworkCore.Migrations;
using RetakesAllocatorCore.Db;

#nullable disable

namespace RetakesAllocator.Migrations
{
    /// <inheritdoc />
    public partial class AddEnemyStuffTeamPreferenceToUserSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var isMySql = ActiveProvider.Contains("MySql", StringComparison.OrdinalIgnoreCase);
            
            if (isMySql)
            {
                migrationBuilder.Sql(
                    MySqlColumnSqlHelper.BuildAddColumnIfMissingSql(
                        "UserSettings",
                        "EnemyStuffTeamPreference",
                        "INT NOT NULL DEFAULT 0"));
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
                isMySql
                    ? "UPDATE `UserSettings`\nSET `EnemyStuffTeamPreference` = 3\nWHERE `EnemyStuffEnabled` = 1;"
                    : "UPDATE \"UserSettings\"\nSET \"EnemyStuffTeamPreference\" = 3\nWHERE \"EnemyStuffEnabled\" = 1;");

            if (isMySql)
            {
                migrationBuilder.Sql(
                    MySqlColumnSqlHelper.BuildDropColumnIfExistsSql("UserSettings", "EnemyStuffEnabled"));
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
            var isMySql = ActiveProvider.Contains("MySql", StringComparison.OrdinalIgnoreCase);
            
            if (isMySql)
            {
                migrationBuilder.Sql(
                    MySqlColumnSqlHelper.BuildAddColumnIfMissingSql(
                        "UserSettings",
                        "EnemyStuffEnabled",
                        "TINYINT(1) NOT NULL DEFAULT 0"));
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
                isMySql
                    ? "UPDATE `UserSettings`\nSET `EnemyStuffEnabled` = CASE\n    WHEN (`EnemyStuffTeamPreference` & 3) <> 0 THEN 1\n    ELSE 0\nEND;"
                    : "UPDATE \"UserSettings\"\nSET \"EnemyStuffEnabled\" = CASE\n    WHEN (\"EnemyStuffTeamPreference\" & 3) <> 0 THEN 1\n    ELSE 0\nEND;");

            if (isMySql)
            {
                migrationBuilder.Sql(
                    MySqlColumnSqlHelper.BuildDropColumnIfExistsSql("UserSettings", "EnemyStuffTeamPreference"));
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
