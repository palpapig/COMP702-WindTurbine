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
            migrationBuilder.Sql("""
                ALTER TABLE "TurbineData"
                DROP CONSTRAINT IF EXISTS "FK_TurbineData_FailureDetectionResult_FailureDetectionResultId";
            """);

            migrationBuilder.Sql("""
                ALTER TABLE "TurbineData"
                DROP CONSTRAINT IF EXISTS "FK_TurbineData_FailureDetectionResults_FailureDetectionResultId";
            """);

            migrationBuilder.Sql("""
                ALTER TABLE IF EXISTS "FailureDetectionResult"
                DROP CONSTRAINT IF EXISTS "PK_FailureDetectionResult";
            """);

            migrationBuilder.Sql("""
                ALTER TABLE "TurbineData"
                DROP COLUMN IF EXISTS "Vibration";
            """);

            migrationBuilder.Sql("""
                ALTER TABLE IF EXISTS "FailureDetectionResult"
                RENAME TO "FailureDetectionResults";
            """);

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

            migrationBuilder.Sql("""
                ALTER TABLE "FailureDetectionResults"
                ADD COLUMN IF NOT EXISTS "IsAcknowledged" boolean NOT NULL DEFAULT false;
            """);

            migrationBuilder.Sql("""
                DO $$
                DECLARE pk_name text;
                BEGIN
                    SELECT c.conname INTO pk_name
                    FROM pg_constraint c
                    JOIN pg_class t ON t.oid = c.conrelid
                    WHERE t.relname = 'FailureDetectionResults' AND c.contype = 'p'
                    LIMIT 1;

                    IF pk_name IS NOT NULL THEN
                        EXECUTE format('ALTER TABLE "FailureDetectionResults" DROP CONSTRAINT %I', pk_name);
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint c
                        JOIN pg_class t ON t.oid = c.conrelid
                        WHERE t.relname = 'FailureDetectionResults' AND c.contype = 'p'
                    ) THEN
                        ALTER TABLE "FailureDetectionResults"
                        ADD CONSTRAINT "PK_FailureDetectionResults" PRIMARY KEY ("Id");
                    END IF;
                END $$;
            """);

            migrationBuilder.Sql("""
                UPDATE "TurbineData" td
                SET "FailureDetectionResultId" = NULL
                WHERE "FailureDetectionResultId" IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1
                      FROM "FailureDetectionResults" fdr
                      WHERE fdr."Id" = td."FailureDetectionResultId"
                  );
            """);

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

            migrationBuilder.Sql("""
                ALTER TABLE "TurbineData"
                ADD COLUMN IF NOT EXISTS "Vibration" double precision NULL;
            """);

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
