using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace COMP702_WindTurbine.Migrations
{
    /// <inheritdoc />
    public partial class AddDegDetailRegions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Offset",
                table: "DegradationModelDetails",
                newName: "Region2p5Offset");

            migrationBuilder.RenameColumn(
                name: "Filepath",
                table: "DegradationModelDetails",
                newName: "Region2p5Filename");

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
                name: "GearboxOilTemp",
                table: "TurbineData",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AddColumn<string>(
                name: "Region2Filename",
                table: "DegradationModelDetails",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<float>(
                name: "Region2Offset",
                table: "DegradationModelDetails",
                type: "real",
                nullable: false,
                defaultValue: 0f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Region2Filename",
                table: "DegradationModelDetails");

            migrationBuilder.DropColumn(
                name: "Region2Offset",
                table: "DegradationModelDetails");

            migrationBuilder.RenameColumn(
                name: "Region2p5Offset",
                table: "DegradationModelDetails",
                newName: "Offset");

            migrationBuilder.RenameColumn(
                name: "Region2p5Filename",
                table: "DegradationModelDetails",
                newName: "Filepath");

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
