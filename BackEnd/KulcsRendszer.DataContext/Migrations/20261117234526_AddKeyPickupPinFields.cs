using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KulcsRendszer.DataContext.Migrations
{
    /// <inheritdoc />
    public partial class AddKeyPickupPinFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PickupPIN",
                table: "Keys",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PinGeneratedAt",
                table: "Keys",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "KeyMovements",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PickupPIN",
                table: "Keys");

            migrationBuilder.DropColumn(
                name: "PinGeneratedAt",
                table: "Keys");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "KeyMovements");
        }
    }
}
