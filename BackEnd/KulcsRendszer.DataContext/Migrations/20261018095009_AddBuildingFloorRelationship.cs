using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KulcsRendszer.DataContext.Migrations
{
    /// <inheritdoc />
    public partial class AddBuildingFloorRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClassRooms_FloorId",
                table: "ClassRooms");

            migrationBuilder.CreateTable(
                name: "Buildings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BuildingCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BuildingName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Buildings", x => x.Id);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO Buildings (BuildingCode, BuildingName)
                SELECT DISTINCT LEFT(Building, 50), Building
                FROM ClassRooms;
                """);

            migrationBuilder.AddColumn<int>(
                name: "BuildingId",
                table: "Floors",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                WITH FloorBuildings AS (
                    SELECT rooms.FloorId, MIN(buildings.Id) AS BuildingId
                    FROM ClassRooms AS rooms
                    INNER JOIN Buildings AS buildings ON buildings.BuildingName = rooms.Building
                    GROUP BY rooms.FloorId
                )
                UPDATE floors
                SET BuildingId = assignments.BuildingId
                FROM Floors AS floors
                INNER JOIN FloorBuildings AS assignments ON assignments.FloorId = floors.Id;

                INSERT INTO Floors (Number, BuildingId)
                SELECT DISTINCT oldFloor.Number, buildings.Id
                FROM Floors AS oldFloor
                INNER JOIN ClassRooms AS rooms ON rooms.FloorId = oldFloor.Id
                INNER JOIN Buildings AS buildings ON buildings.BuildingName = rooms.Building
                WHERE buildings.Id <> oldFloor.BuildingId;

                UPDATE rooms
                SET FloorId = destination.Id
                FROM ClassRooms AS rooms
                INNER JOIN Floors AS oldFloor ON oldFloor.Id = rooms.FloorId
                INNER JOIN Buildings AS buildings ON buildings.BuildingName = rooms.Building
                INNER JOIN Floors AS destination
                    ON destination.Number = oldFloor.Number
                    AND destination.BuildingId = buildings.Id
                WHERE oldFloor.BuildingId <> buildings.Id;

                DELETE FROM Floors WHERE BuildingId IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "BuildingId",
                table: "Floors",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ClassRooms",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.DropColumn(
                name: "Building",
                table: "ClassRooms");

            migrationBuilder.CreateIndex(
                name: "IX_Floors_BuildingId_Number",
                table: "Floors",
                columns: new[] { "BuildingId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassRooms_FloorId_Name",
                table: "ClassRooms",
                columns: new[] { "FloorId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_BuildingCode",
                table: "Buildings",
                column: "BuildingCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Floors_Buildings_BuildingId",
                table: "Floors",
                column: "BuildingId",
                principalTable: "Buildings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Floors_BuildingId_Number",
                table: "Floors");

            migrationBuilder.DropIndex(
                name: "IX_ClassRooms_FloorId_Name",
                table: "ClassRooms");

            migrationBuilder.DropForeignKey(
                name: "FK_Floors_Buildings_BuildingId",
                table: "Floors");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassRooms_Floors_FloorId",
                table: "ClassRooms");

            migrationBuilder.AddColumn<string>(
                name: "Building",
                table: "ClassRooms",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE rooms
                SET Building = buildings.BuildingName
                FROM ClassRooms AS rooms
                INNER JOIN Floors AS floors ON floors.Id = rooms.FloorId
                INNER JOIN Buildings AS buildings ON buildings.Id = floors.BuildingId;

                WITH FloorNumbers AS (
                    SELECT Number, MIN(Id) AS FloorId
                    FROM Floors
                    GROUP BY Number
                )
                UPDATE rooms
                SET FloorId = floorNumbers.FloorId
                FROM ClassRooms AS rooms
                INNER JOIN Floors AS currentFloor ON currentFloor.Id = rooms.FloorId
                INNER JOIN FloorNumbers AS floorNumbers ON floorNumbers.Number = currentFloor.Number;

                DELETE FROM Floors
                WHERE Id NOT IN (SELECT MIN(Id) FROM Floors GROUP BY Number);
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

            migrationBuilder.DropTable(
                name: "Buildings");

            migrationBuilder.DropColumn(
                name: "BuildingId",
                table: "Floors");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ClassRooms",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

        }
    }
}
