using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace COMP702_WindTurbine.Migrations
{
    /// <inheritdoc />
    public partial class AddPitchAngleAndGearboxOilTemp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "GearboxOilTemp",
                table: "TurbineData",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PitchAngle",
                table: "TurbineData",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GearboxOilTemp",
                table: "TurbineData");

            migrationBuilder.DropColumn(
                name: "PitchAngle",
                table: "TurbineData");
        }
    }
}
