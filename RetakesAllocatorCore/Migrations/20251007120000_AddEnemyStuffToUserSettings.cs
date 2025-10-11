using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetakesAllocator.Migrations
{
    /// <inheritdoc />
    public partial class AddEnemyStuffToUserSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnemyStuffEnabled",
                table: "UserSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnemyStuffEnabled",
                table: "UserSettings");
        }
    }
}
