using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPulse.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthTokenUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EmailConfirmationTokenExpiresAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailConfirmationTokenHash",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEmailConfirmed",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpiresAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetTokenHash",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefreshTokenExpiresAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefreshTokenHash",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RefreshTokenRememberMe",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailConfirmationTokenExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailConfirmationTokenHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsEmailConfirmed",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RefreshTokenExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RefreshTokenHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RefreshTokenRememberMe",
                table: "Users");
        }
    }
}
