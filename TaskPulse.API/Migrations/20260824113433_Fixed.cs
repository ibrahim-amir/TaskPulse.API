using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPulse.API.Migrations
{
    /// <inheritdoc />
    public partial class Fixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActivityLogs_Users_UserId",
                table: "ActivityLogs");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "ActivityLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TaskId",
                table: "ActivityLogs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_ProjectId",
                table: "ActivityLogs",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_TaskId",
                table: "ActivityLogs",
                column: "TaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityLogs_Projects_ProjectId",
                table: "ActivityLogs",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityLogs_Tasks_TaskId",
                table: "ActivityLogs",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityLogs_Users_UserId",
                table: "ActivityLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActivityLogs_Projects_ProjectId",
                table: "ActivityLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_ActivityLogs_Tasks_TaskId",
                table: "ActivityLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_ActivityLogs_Users_UserId",
                table: "ActivityLogs");

            migrationBuilder.DropIndex(
                name: "IX_ActivityLogs_ProjectId",
                table: "ActivityLogs");

            migrationBuilder.DropIndex(
                name: "IX_ActivityLogs_TaskId",
                table: "ActivityLogs");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "ActivityLogs");

            migrationBuilder.DropColumn(
                name: "TaskId",
                table: "ActivityLogs");

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityLogs_Users_UserId",
                table: "ActivityLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
