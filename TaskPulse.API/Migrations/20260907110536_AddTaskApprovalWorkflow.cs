using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPulse.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaskApprovalRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ResolvedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskApprovalRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskApprovalRequests_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskApprovalRequests_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskApprovalResponses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskApprovalRequestId = table.Column<int>(type: "int", nullable: false),
                    ApproverUserId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RespondedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskApprovalResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskApprovalResponses_TaskApprovalRequests_TaskApprovalRequestId",
                        column: x => x.TaskApprovalRequestId,
                        principalTable: "TaskApprovalRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskApprovalResponses_Users_ApproverUserId",
                        column: x => x.ApproverUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskApprovalRequests_RequestedByUserId",
                table: "TaskApprovalRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskApprovalRequests_TaskId_Status",
                table: "TaskApprovalRequests",
                columns: new[] { "TaskId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskApprovalResponses_ApproverUserId",
                table: "TaskApprovalResponses",
                column: "ApproverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskApprovalResponses_TaskApprovalRequestId_ApproverUserId",
                table: "TaskApprovalResponses",
                columns: new[] { "TaskApprovalRequestId", "ApproverUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskApprovalResponses");

            migrationBuilder.DropTable(
                name: "TaskApprovalRequests");
        }
    }
}
