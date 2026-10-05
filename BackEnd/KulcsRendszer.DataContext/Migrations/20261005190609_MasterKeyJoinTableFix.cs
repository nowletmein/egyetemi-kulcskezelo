using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KulcsRendszer.DataContext.Migrations
{
    /// <inheritdoc />
    public partial class MasterKeyJoinTableFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MasterKeyRooms_ClassRooms_AccessibleRoomsId",
                table: "MasterKeyRooms");

            migrationBuilder.DropForeignKey(
                name: "FK_MasterKeyRooms_Keys_MasterKeyId",
                table: "MasterKeyRooms");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MasterKeyRooms",
                table: "MasterKeyRooms");

            migrationBuilder.RenameTable(
                name: "MasterKeyRooms",
                newName: "MasterKeyAccess");

            migrationBuilder.RenameIndex(
                name: "IX_MasterKeyRooms_MasterKeyId",
                table: "MasterKeyAccess",
                newName: "IX_MasterKeyAccess_MasterKeyId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MasterKeyAccess",
                table: "MasterKeyAccess",
                columns: new[] { "AccessibleRoomsId", "MasterKeyId" });

            migrationBuilder.AddForeignKey(
                name: "FK_MasterKeyAccess_ClassRooms_AccessibleRoomsId",
                table: "MasterKeyAccess",
                column: "AccessibleRoomsId",
                principalTable: "ClassRooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MasterKeyAccess_Keys_MasterKeyId",
                table: "MasterKeyAccess",
                column: "MasterKeyId",
                principalTable: "Keys",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MasterKeyAccess_ClassRooms_AccessibleRoomsId",
                table: "MasterKeyAccess");

            migrationBuilder.DropForeignKey(
                name: "FK_MasterKeyAccess_Keys_MasterKeyId",
                table: "MasterKeyAccess");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MasterKeyAccess",
                table: "MasterKeyAccess");

            migrationBuilder.RenameTable(
                name: "MasterKeyAccess",
                newName: "MasterKeyRooms");

            migrationBuilder.RenameIndex(
                name: "IX_MasterKeyAccess_MasterKeyId",
                table: "MasterKeyRooms",
                newName: "IX_MasterKeyRooms_MasterKeyId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MasterKeyRooms",
                table: "MasterKeyRooms",
                columns: new[] { "AccessibleRoomsId", "MasterKeyId" });

            migrationBuilder.AddForeignKey(
                name: "FK_MasterKeyRooms_ClassRooms_AccessibleRoomsId",
                table: "MasterKeyRooms",
                column: "AccessibleRoomsId",
                principalTable: "ClassRooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MasterKeyRooms_Keys_MasterKeyId",
                table: "MasterKeyRooms",
                column: "MasterKeyId",
                principalTable: "Keys",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
