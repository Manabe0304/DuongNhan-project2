using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DuongNhan.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class SecurityHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiagnosisConditions_diagnoses_DiagnosisId",
                table: "DiagnosisConditions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DiagnosisConditions",
                table: "DiagnosisConditions");

            migrationBuilder.DropIndex(
                name: "IX_DiagnosisConditions_DiagnosisId",
                table: "DiagnosisConditions");

            migrationBuilder.RenameTable(
                name: "DiagnosisConditions",
                newName: "diagnosis_conditions");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TokensInvalidatedAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Confidence",
                table: "diagnosis_conditions",
                type: "numeric(4,3)",
                precision: 4,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "ConditionCode",
                table: "diagnosis_conditions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddPrimaryKey(
                name: "PK_diagnosis_conditions",
                table: "diagnosis_conditions",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "login_attempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FailedCount = table.Column<int>(type: "integer", nullable: false),
                    LastFailedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockedUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_login_attempts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_diagnosis_conditions_DiagnosisId_Rank",
                table: "diagnosis_conditions",
                columns: new[] { "DiagnosisId", "Rank" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_EntityType_EntityId",
                table: "audit_logs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_UserId_CreatedAt",
                table: "audit_logs",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_login_attempts_EmailHash",
                table: "login_attempts",
                column: "EmailHash",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_diagnosis_conditions_diagnoses_DiagnosisId",
                table: "diagnosis_conditions",
                column: "DiagnosisId",
                principalTable: "diagnoses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_diagnosis_conditions_diagnoses_DiagnosisId",
                table: "diagnosis_conditions");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "login_attempts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_diagnosis_conditions",
                table: "diagnosis_conditions");

            migrationBuilder.DropIndex(
                name: "IX_diagnosis_conditions_DiagnosisId_Rank",
                table: "diagnosis_conditions");

            migrationBuilder.DropColumn(
                name: "TokensInvalidatedAt",
                table: "users");

            migrationBuilder.RenameTable(
                name: "diagnosis_conditions",
                newName: "DiagnosisConditions");

            migrationBuilder.AlterColumn<decimal>(
                name: "Confidence",
                table: "DiagnosisConditions",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(4,3)",
                oldPrecision: 4,
                oldScale: 3);

            migrationBuilder.AlterColumn<string>(
                name: "ConditionCode",
                table: "DiagnosisConditions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddPrimaryKey(
                name: "PK_DiagnosisConditions",
                table: "DiagnosisConditions",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosisConditions_DiagnosisId",
                table: "DiagnosisConditions",
                column: "DiagnosisId");

            migrationBuilder.AddForeignKey(
                name: "FK_DiagnosisConditions_diagnoses_DiagnosisId",
                table: "DiagnosisConditions",
                column: "DiagnosisId",
                principalTable: "diagnoses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
