using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KulcsRendszer.DataContext.Migrations
{
    /// <inheritdoc />
    public partial class RenameMasterKeyJoinTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassRoomMasterKey_ClassRooms_AccessibleRoomsId",
                table: "ClassRoomMasterKey");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassRoomMasterKey_Keys_MasterKeyId",
                table: "ClassRoomMasterKey");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ClassRoomMasterKey",
                table: "ClassRoomMasterKey");

            migrationBuilder.RenameTable(
                name: "ClassRoomMasterKey",
                newName: "MasterKeyRooms");

            migrationBuilder.RenameIndex(
                name: "IX_ClassRoomMasterKey_MasterKeyId",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
                newName: "ClassRoomMasterKey");

            migrationBuilder.RenameIndex(
                name: "IX_MasterKeyRooms_MasterKeyId",
                table: "ClassRoomMasterKey",
                newName: "IX_ClassRoomMasterKey_MasterKeyId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ClassRoomMasterKey",
                table: "ClassRoomMasterKey",
                columns: new[] { "AccessibleRoomsId", "MasterKeyId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ClassRoomMasterKey_ClassRooms_AccessibleRoomsId",
                table: "ClassRoomMasterKey",
                column: "AccessibleRoomsId",
                principalTable: "ClassRooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassRoomMasterKey_Keys_MasterKeyId",
                table: "ClassRoomMasterKey",
                column: "MasterKeyId",
                principalTable: "Keys",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
