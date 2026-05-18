using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace COMP702_WindTurbine.Migrations
{
    /// <inheritdoc />
    public partial class AddBenchmarkingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddColumn<double>(
                name: "CorrectedWindSpeed",
                table: "TurbineData",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GeneratorSpeed",
                table: "TurbineData",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "MinimumPowerOutput",
                table: "TurbineData",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "TurbineModelId",
                table: "Turbine",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BenchmarkResult",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeviationScore = table.Column<float>(type: "real", nullable: false),
                    TimeRangeStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeRangeEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TurbineId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenchmarkResult", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BenchmarkResult_Turbine_TurbineId",
                        column: x => x.TurbineId,
                        principalTable: "Turbine",
                        principalColumn: "TurbineId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DegradationModelDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Offset = table.Column<float>(type: "real", nullable: false),
                    Filepath = table.Column<string>(type: "text", nullable: false),
                    TurbineId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DegradationModelDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DegradationModelDetails_Turbine_TurbineId",
                        column: x => x.TurbineId,
                        principalTable: "Turbine",
                        principalColumn: "TurbineId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DegradationResult",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Region2Score = table.Column<float>(type: "real", nullable: false),
                    Region2Point5Score = table.Column<float>(type: "real", nullable: false),
                    TimeRangeStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeRangeEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TurbineId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DegradationResult", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DegradationResult_Turbine_TurbineId",
                        column: x => x.TurbineId,
                        principalTable: "Turbine",
                        principalColumn: "TurbineId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TurbineModel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CutInWindSpeed = table.Column<float>(type: "real", nullable: false),
                    SaturationWindSpeed = table.Column<float>(type: "real", nullable: false),
                    RatedWindSpeed = table.Column<float>(type: "real", nullable: false),
                    CutOutWindSpeed = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TurbineModel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PowerBinDeviation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BenchmarkResultId = table.Column<int>(type: "integer", nullable: false),
                    WindSpeed = table.Column<float>(type: "real", nullable: false),
                    PowerDifference = table.Column<float>(type: "real", nullable: false),
                    PowerRatio = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PowerBinDeviation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PowerBinDeviation_BenchmarkResult_BenchmarkResultId",
                        column: x => x.BenchmarkResultId,
                        principalTable: "BenchmarkResult",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PowerBinMeasured",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BenchmarkId = table.Column<int>(type: "integer", nullable: false),
                    WindSpeed = table.Column<float>(type: "real", nullable: false),
                    Power = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PowerBinMeasured", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PowerBinMeasured_BenchmarkResult_BenchmarkId",
                        column: x => x.BenchmarkId,
                        principalTable: "BenchmarkResult",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PowerBinExpected",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BenchmarkId = table.Column<int>(type: "integer", nullable: false),
                    WindSpeed = table.Column<float>(type: "real", nullable: false),
                    Power = table.Column<float>(type: "real", nullable: false),
                    TurbineModelId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PowerBinExpected", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PowerBinExpected_BenchmarkResult_BenchmarkId",
                        column: x => x.BenchmarkId,
                        principalTable: "BenchmarkResult",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PowerBinExpected_TurbineModel_TurbineModelId",
                        column: x => x.TurbineModelId,
                        principalTable: "TurbineModel",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Turbine_TurbineModelId",
                table: "Turbine",
                column: "TurbineModelId");

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkResult_TurbineId",
                table: "BenchmarkResult",
                column: "TurbineId");

            migrationBuilder.CreateIndex(
                name: "IX_DegradationModelDetails_TurbineId",
                table: "DegradationModelDetails",
                column: "TurbineId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DegradationResult_TurbineId",
                table: "DegradationResult",
                column: "TurbineId");

            migrationBuilder.CreateIndex(
                name: "IX_PowerBinDeviation_BenchmarkResultId",
                table: "PowerBinDeviation",
                column: "BenchmarkResultId");

            migrationBuilder.CreateIndex(
                name: "IX_PowerBinExpected_BenchmarkId",
                table: "PowerBinExpected",
                column: "BenchmarkId");

            migrationBuilder.CreateIndex(
                name: "IX_PowerBinExpected_TurbineModelId",
                table: "PowerBinExpected",
                column: "TurbineModelId");

            migrationBuilder.CreateIndex(
                name: "IX_PowerBinMeasured_BenchmarkId",
                table: "PowerBinMeasured",
                column: "BenchmarkId");

            migrationBuilder.AddForeignKey(
                name: "FK_Turbine_TurbineModel_TurbineModelId",
                table: "Turbine",
                column: "TurbineModelId",
                principalTable: "TurbineModel",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Turbine_TurbineModel_TurbineModelId",
                table: "Turbine");

            migrationBuilder.DropTable(
                name: "DegradationModelDetails");

            migrationBuilder.DropTable(
                name: "DegradationResult");

            migrationBuilder.DropTable(
                name: "PowerBinDeviation");

            migrationBuilder.DropTable(
                name: "PowerBinExpected");

            migrationBuilder.DropTable(
                name: "PowerBinMeasured");

            migrationBuilder.DropTable(
                name: "TurbineModel");

            migrationBuilder.DropTable(
                name: "BenchmarkResult");

            migrationBuilder.DropIndex(
                name: "IX_Turbine_TurbineModelId",
                table: "Turbine");

            migrationBuilder.DropColumn(
                name: "CorrectedWindSpeed",
                table: "TurbineData");

            migrationBuilder.DropColumn(
                name: "GeneratorSpeed",
                table: "TurbineData");

            migrationBuilder.DropColumn(
                name: "MinimumPowerOutput",
                table: "TurbineData");

            migrationBuilder.DropColumn(
                name: "TurbineModelId",
                table: "Turbine");

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
        }
    }
}
