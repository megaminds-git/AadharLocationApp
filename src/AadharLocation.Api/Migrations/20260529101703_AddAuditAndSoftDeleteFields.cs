using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AadharLocation.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditAndSoftDeleteFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Operators",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                table: "Operators",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Operators",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Machines",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MachineAuthCode",
                table: "Machines",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Machines",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Operators");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "Operators");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Operators");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "MachineAuthCode",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Machines");
        }
    }
}
