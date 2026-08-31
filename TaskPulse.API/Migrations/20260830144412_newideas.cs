using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPulse.API.Migrations
{
    /// <inheritdoc />
    public partial class newideas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Users_ManagerId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Users_AssignedUserId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_AssignedUserId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Projects_ManagerId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AssignedUserId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "ManagerId",
                table: "Projects");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "TaskAttachments",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "JoinedAt",
                table: "ProjectMembers",
                newName: "CreatedAt");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "ProjectMembers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "TaskAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AssignedByRole = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskAssignments_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskAssignments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskAssignments_TaskId_UserId",
                table: "TaskAssignments",
                columns: new[] { "TaskId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskAssignments_UserId",
                table: "TaskAssignments",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskAssignments");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "ProjectMembers");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "TaskAttachments",
                newName: "UploadedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "ProjectMembers",
                newName: "JoinedAt");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "AssignedUserId",
                table: "Tasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ManagerId",
                table: "Projects",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_AssignedUserId",
                table: "Tasks",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ManagerId",
                table: "Projects",
                column: "ManagerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Users_ManagerId",
                table: "Projects",
                column: "ManagerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Users_AssignedUserId",
                table: "Tasks",
                column: "AssignedUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
