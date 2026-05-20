using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace COMP702_WindTurbine.Migrations
{
    /// <inheritdoc />
    public partial class FailureDetectionMerge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TurbineData_FailureDetectionResult_FailureDetectionResultId",
                table: "TurbineData");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FailureDetectionResult",
                table: "FailureDetectionResult");

            migrationBuilder.DropColumn(
                name: "Vibration",
                table: "TurbineData");

            migrationBuilder.RenameTable(
                name: "FailureDetectionResult",
                newName: "FailureDetectionResults");

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

            migrationBuilder.AddColumn<bool>(
                name: "IsAcknowledged",
                table: "FailureDetectionResults",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_FailureDetectionResults",
                table: "FailureDetectionResults",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TurbineData_FailureDetectionResults_FailureDetectionResultId",
                table: "TurbineData",
                column: "FailureDetectionResultId",
                principalTable: "FailureDetectionResults",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TurbineData_FailureDetectionResults_FailureDetectionResultId",
                table: "TurbineData");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FailureDetectionResults",
                table: "FailureDetectionResults");

            migrationBuilder.DropColumn(
                name: "IsAcknowledged",
                table: "FailureDetectionResults");

            migrationBuilder.RenameTable(
                name: "FailureDetectionResults",
                newName: "FailureDetectionResult");

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

            migrationBuilder.AddColumn<double>(
                name: "Vibration",
                table: "TurbineData",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_FailureDetectionResult",
                table: "FailureDetectionResult",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TurbineData_FailureDetectionResult_FailureDetectionResultId",
                table: "TurbineData",
                column: "FailureDetectionResultId",
                principalTable: "FailureDetectionResult",
                principalColumn: "Id");
        }
    }
}
