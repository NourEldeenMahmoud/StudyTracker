using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudyTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddBadgeIsNotified : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsNotified",
                table: "UserBadges",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsNotified",
                table: "UserBadges");
        }
    }
}
