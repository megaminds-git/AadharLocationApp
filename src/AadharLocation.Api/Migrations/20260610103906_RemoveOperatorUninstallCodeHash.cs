using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AadharLocation.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOperatorUninstallCodeHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UninstallCodeHash",
                table: "Operators");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UninstallCodeHash",
                table: "Operators",
                type: "text",
                nullable: true);
        }
    }
}
