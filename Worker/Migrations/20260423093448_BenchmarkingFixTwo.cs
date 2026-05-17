using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace COMP702_WindTurbine.Migrations
{
    /// <inheritdoc />
    public partial class BenchmarkingFixTwo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PowerBinExpected_TurbineModel_TurbineModelId",
                table: "PowerBinExpected");

            migrationBuilder.DropForeignKey(
                name: "FK_PowerBinMeasured_BenchmarkResult_BenchmarkId",
                table: "PowerBinMeasured");

            migrationBuilder.RenameColumn(
                name: "BenchmarkId",
                table: "PowerBinMeasured",
                newName: "BenchmarkResultId");

            migrationBuilder.RenameIndex(
                name: "IX_PowerBinMeasured_BenchmarkId",
                table: "PowerBinMeasured",
                newName: "IX_PowerBinMeasured_BenchmarkResultId");

            migrationBuilder.AlterColumn<long>(
                name: "TurbineModelId",
                table: "PowerBinExpected",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<float>(
                name: "DeviationScore",
                table: "BenchmarkResult",
                type: "real",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AddForeignKey(
                name: "FK_PowerBinExpected_TurbineModel_TurbineModelId",
                table: "PowerBinExpected",
                column: "TurbineModelId",
                principalTable: "TurbineModel",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PowerBinMeasured_BenchmarkResult_BenchmarkResultId",
                table: "PowerBinMeasured",
                column: "BenchmarkResultId",
                principalTable: "BenchmarkResult",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PowerBinExpected_TurbineModel_TurbineModelId",
                table: "PowerBinExpected");

            migrationBuilder.DropForeignKey(
                name: "FK_PowerBinMeasured_BenchmarkResult_BenchmarkResultId",
                table: "PowerBinMeasured");

            migrationBuilder.RenameColumn(
                name: "BenchmarkResultId",
                table: "PowerBinMeasured",
                newName: "BenchmarkId");

            migrationBuilder.RenameIndex(
                name: "IX_PowerBinMeasured_BenchmarkResultId",
                table: "PowerBinMeasured",
                newName: "IX_PowerBinMeasured_BenchmarkId");

            migrationBuilder.AlterColumn<long>(
                name: "TurbineModelId",
                table: "PowerBinExpected",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<float>(
                name: "DeviationScore",
                table: "BenchmarkResult",
                type: "real",
                nullable: false,
                defaultValue: 0f,
                oldClrType: typeof(float),
                oldType: "real",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PowerBinExpected_TurbineModel_TurbineModelId",
                table: "PowerBinExpected",
                column: "TurbineModelId",
                principalTable: "TurbineModel",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PowerBinMeasured_BenchmarkResult_BenchmarkId",
                table: "PowerBinMeasured",
                column: "BenchmarkId",
                principalTable: "BenchmarkResult",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
