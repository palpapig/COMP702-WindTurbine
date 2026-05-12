using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace COMP702_WindTurbine.Migrations
{
    /// <inheritdoc />
    public partial class AddFailureDetectionResultsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "FailureDetectionResultId",
                table: "TurbineData",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FailureDetectionResult",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TurbineId = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Residual = table.Column<double>(type: "double precision", nullable: true),
                    IsAbnormal = table.Column<bool>(type: "boolean", nullable: true),
                    AlarmLvl = table.Column<int>(type: "integer", nullable: true),
                    PredictedValue = table.Column<double>(type: "double precision", nullable: true),
                    ActualValue = table.Column<double>(type: "double precision", nullable: true),
                    LCL = table.Column<double>(type: "double precision", nullable: true),
                    UCL = table.Column<double>(type: "double precision", nullable: true),
                    EWMA = table.Column<double>(type: "double precision", nullable: true),
                    A1Triggered = table.Column<bool>(type: "boolean", nullable: true),
                    A2Triggered = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FailureDetectionResult", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TurbineData_FailureDetectionResultId",
                table: "TurbineData",
                column: "FailureDetectionResultId");

            migrationBuilder.AddForeignKey(
                name: "FK_TurbineData_FailureDetectionResult_FailureDetectionResultId",
                table: "TurbineData",
                column: "FailureDetectionResultId",
                principalTable: "FailureDetectionResult",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TurbineData_FailureDetectionResult_FailureDetectionResultId",
                table: "TurbineData");

            migrationBuilder.DropTable(
                name: "FailureDetectionResult");

            migrationBuilder.DropIndex(
                name: "IX_TurbineData_FailureDetectionResultId",
                table: "TurbineData");

            migrationBuilder.DropColumn(
                name: "FailureDetectionResultId",
                table: "TurbineData");
        }
    }
}
