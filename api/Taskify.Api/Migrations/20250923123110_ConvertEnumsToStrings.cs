using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taskify.Api.Migrations
{
    /// <inheritdoc />
    public partial class ConvertEnumsToStrings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, add new string columns
            migrationBuilder.AddColumn<string>(
                name: "StatusString",
                table: "Tasks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Todo");

            migrationBuilder.AddColumn<string>(
                name: "PriorityString",
                table: "Tasks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Normal");

            // Convert existing integer values to string values
            migrationBuilder.Sql(@"
                UPDATE Tasks SET StatusString = 
                    CASE Status
                        WHEN 0 THEN 'Todo'
                        WHEN 1 THEN 'InProgress'
                        WHEN 2 THEN 'Done'
                        ELSE 'Todo'
                    END");

            migrationBuilder.Sql(@"
                UPDATE Tasks SET PriorityString = 
                    CASE Priority
                        WHEN 0 THEN 'Low'
                        WHEN 1 THEN 'Normal'
                        WHEN 2 THEN 'High'
                        ELSE 'Normal'
                    END");

            // Drop old integer columns
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Tasks");

            // Rename new columns to original names
            migrationBuilder.RenameColumn(
                name: "StatusString",
                table: "Tasks",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "PriorityString",
                table: "Tasks",
                newName: "Priority");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add back integer columns
            migrationBuilder.AddColumn<int>(
                name: "StatusInt",
                table: "Tasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PriorityInt",
                table: "Tasks",
                type: "int",
                nullable: false,
                defaultValue: 1);

            // Convert string values back to integers
            migrationBuilder.Sql(@"
                UPDATE Tasks SET StatusInt = 
                    CASE Status
                        WHEN 'Todo' THEN 0
                        WHEN 'InProgress' THEN 1
                        WHEN 'Done' THEN 2
                        ELSE 0
                    END");

            migrationBuilder.Sql(@"
                UPDATE Tasks SET PriorityInt = 
                    CASE Priority
                        WHEN 'Low' THEN 0
                        WHEN 'Normal' THEN 1
                        WHEN 'High' THEN 2
                        ELSE 1
                    END");

            // Drop string columns
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Tasks");

            // Rename integer columns back to original names
            migrationBuilder.RenameColumn(
                name: "StatusInt",
                table: "Tasks",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "PriorityInt",
                table: "Tasks",
                newName: "Priority");
        }
    }
}
