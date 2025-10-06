using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taskify.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskTagsManyToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskTags_Tasks_TaskItemId",
                table: "TaskTags");

            migrationBuilder.RenameColumn(
                name: "TaskItemId",
                table: "TaskTags",
                newName: "TaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskTags_Tasks_TaskId",
                table: "TaskTags",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskTags_Tasks_TaskId",
                table: "TaskTags");

            migrationBuilder.RenameColumn(
                name: "TaskId",
                table: "TaskTags",
                newName: "TaskItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskTags_Tasks_TaskItemId",
                table: "TaskTags",
                column: "TaskItemId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
