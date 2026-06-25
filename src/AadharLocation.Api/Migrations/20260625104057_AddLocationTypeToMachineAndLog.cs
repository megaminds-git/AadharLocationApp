using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AadharLocation.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationTypeToMachineAndLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentLocationType",
                table: "Machines",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationType",
                table: "LocationLogs",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentLocationType",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "LocationType",
                table: "LocationLogs");
        }
    }
}
