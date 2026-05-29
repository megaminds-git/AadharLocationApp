using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AadharLocation.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDistrictDropEmployeeId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Operators_EmployeeId",
                table: "Operators");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "Operators");

            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "Operators",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "District",
                table: "Operators");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeId",
                table: "Operators",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Operators_EmployeeId",
                table: "Operators",
                column: "EmployeeId",
                unique: true);
        }
    }
}
