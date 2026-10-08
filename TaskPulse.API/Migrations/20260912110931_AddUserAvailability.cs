using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPulse.API.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The entity's C# property initializer (= true) only applies to
            // rows created through EF from now on — it says nothing about
            // backfilling rows that already exist. Every existing user
            // should start out "available", not silently flipped to "busy".
            migrationBuilder.AddColumn<bool>(
                name: "IsAvailableForAssignment",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAvailableForAssignment",
                table: "Users");
        }
    }
}
