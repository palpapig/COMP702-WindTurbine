using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace COMP702_WindTurbine.Migrations
{
    /// <inheritdoc />
    public partial class addFaultDetectionColoumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "WindSpeed",
                table: "TurbineData",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AlterColumn<double>(
                name: "Vibration",
                table: "TurbineData",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AlterColumn<double>(
                name: "Temperature",
                table: "TurbineData",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AlterColumn<double>(
                name: "RotorSpeed",
                table: "TurbineData",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AlterColumn<double>(
                name: "PowerOutput",
                table: "TurbineData",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AlterColumn<double>(
                name: "PitchAngle",
                table: "TurbineData",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AlterColumn<double>(
                name: "GearboxOilTemp",
                table: "TurbineData",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AddColumn<double>(
                name: "GearOilInletPressure",
                table: "TurbineData",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GearOilInletTemp",
                table: "TurbineData",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GearOilPumpPressure",
                table: "TurbineData",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GeneratorBearingFrontTemp",
                table: "TurbineData",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "NacelleTemp",
                table: "TurbineData",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RearBearingTemp",
                table: "TurbineData",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GearOilInletPressure",
                table: "TurbineData");

            migrationBuilder.DropColumn(
                name: "GearOilInletTemp",
                table: "TurbineData");

            migrationBuilder.DropColumn(
                name: "GearOilPumpPressure",
                table: "TurbineData");

            migrationBuilder.DropColumn(
                name: "GeneratorBearingFrontTemp",
                table: "TurbineData");

            migrationBuilder.DropColumn(
                name: "NacelleTemp",
                table: "TurbineData");

            migrationBuilder.DropColumn(
                name: "RearBearingTemp",
                table: "TurbineData");

            migrationBuilder.AlterColumn<double>(
                name: "WindSpeed",
                table: "TurbineData",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Vibration",
                table: "TurbineData",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Temperature",
                table: "TurbineData",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "RotorSpeed",
                table: "TurbineData",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "PowerOutput",
                table: "TurbineData",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "PitchAngle",
                table: "TurbineData",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "GearboxOilTemp",
                table: "TurbineData",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);
        }
    }
}
