using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetakesAllocator.Migrations
{
    /// <inheritdoc />
    public partial class AddZeusPreferenceToUserSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ZeusEnabled",
                table: "UserSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ZeusEnabled",
                table: "UserSettings");
        }
    }
}
