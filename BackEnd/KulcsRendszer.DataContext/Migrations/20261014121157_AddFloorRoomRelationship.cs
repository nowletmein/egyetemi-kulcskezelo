using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KulcsRendszer.DataContext.Migrations
{
    /// <inheritdoc />
    public partial class AddFloorRoomRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Floor",
                table: "ClassRooms",
                newName: "FloorId");

            migrationBuilder.CreateTable(
                name: "Floors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Number = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Floors", x => x.Id);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO Floors (Number)
                SELECT DISTINCT FloorId
                FROM ClassRooms
                WHERE FloorId IS NOT NULL;

                UPDATE rooms
                SET FloorId = floors.Id
                FROM ClassRooms AS rooms
                INNER JOIN Floors AS floors ON floors.Number = rooms.FloorId;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ClassRooms_FloorId",
                table: "ClassRooms",
                column: "FloorId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassRooms_Floors_FloorId",
                table: "ClassRooms",
                column: "FloorId",
                principalTable: "Floors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassRooms_Floors_FloorId",
                table: "ClassRooms");

            migrationBuilder.DropTable(
                name: "Floors");

            migrationBuilder.DropIndex(
                name: "IX_ClassRooms_FloorId",
                table: "ClassRooms");

            migrationBuilder.RenameColumn(
                name: "FloorId",
                table: "ClassRooms",
                newName: "Floor");
        }
    }
}
