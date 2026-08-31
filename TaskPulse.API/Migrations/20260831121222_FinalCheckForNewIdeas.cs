using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPulse.API.Migrations
{
    /// <inheritdoc />
    public partial class FinalCheckForNewIdeas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "EmailConfirmationTokenExpiresAt", "EmailConfirmationTokenHash", "IsDisabled", "IsEmailConfirmed", "Name", "PasswordHash", "PasswordResetTokenExpiresAt", "PasswordResetTokenHash", "RefreshTokenExpiresAt", "RefreshTokenHash", "RefreshTokenRememberMe", "Role", "profileImageUrl" },
                values: new object[] { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@taskpulse.local", null, null, false, true, "Admin", "$2a$11$Jq4mIC9S3KdDM17V8AZvKOxW0Ljd4eicGvHgRRe0KzxMbqUjZXn.m", null, null, null, null, false, "Admin", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");
        }
    }
}
