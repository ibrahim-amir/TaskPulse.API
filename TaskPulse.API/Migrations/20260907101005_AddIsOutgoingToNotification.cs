using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPulse.API.Migrations
{
    /// <inheritdoc />
    public partial class AddIsOutgoingToNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOutgoing",
                table: "Notifications",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOutgoing",
                table: "Notifications");
        }
    }
}
