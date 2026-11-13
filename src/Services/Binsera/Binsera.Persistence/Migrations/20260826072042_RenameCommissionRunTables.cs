using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Binsera.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameCommissionRunTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonthlyPayoutLineDetail",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "MonthlyPayoutLine",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "MonthlyPayoutRun",
                schema: "dbo");

            migrationBuilder.CreateTable(
                name: "CommissionRun",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CalculatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalBusiness = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCommission = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ApprovedBy = table.Column<long>(type: "bigint", nullable: true),
                    ApprovedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionRun", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommissionLine",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommissionRunId = table.Column<long>(type: "bigint", nullable: false),
                    RegNo = table.Column<long>(type: "bigint", nullable: false),
                    IntroRegNoAtRun = table.Column<long>(type: "bigint", nullable: true),
                    SelfBusiness = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TeamBusiness = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Level = table.Column<int>(type: "int", nullable: true),
                    LevelPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 2, nullable: false),
                    OwnBusinessCommission = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DifferentialCommission = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCommission = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreditStatus = table.Column<int>(type: "int", nullable: false),
                    CreditedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommissionLine_CommissionRun_CommissionRunId",
                        column: x => x.CommissionRunId,
                        principalSchema: "dbo",
                        principalTable: "CommissionRun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommissionLineDetail",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommissionLineId = table.Column<long>(type: "bigint", nullable: false),
                    ReferralRegNo = table.Column<long>(type: "bigint", nullable: false),
                    ReferralSelfBusiness = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReferralLevelPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 2, nullable: false),
                    SponsorLevelPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 2, nullable: false),
                    DifferentialAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionLineDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommissionLineDetail_CommissionLine_CommissionLineId",
                        column: x => x.CommissionLineId,
                        principalSchema: "dbo",
                        principalTable: "CommissionLine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommissionLine_CommissionRunId_RegNo",
                schema: "dbo",
                table: "CommissionLine",
                columns: new[] { "CommissionRunId", "RegNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommissionLine_CreditStatus",
                schema: "dbo",
                table: "CommissionLine",
                column: "CreditStatus");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionLine_RegNo",
                schema: "dbo",
                table: "CommissionLine",
                column: "RegNo");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionLineDetail_CommissionLineId",
                schema: "dbo",
                table: "CommissionLineDetail",
                column: "CommissionLineId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRun_Status",
                schema: "dbo",
                table: "CommissionRun",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRun_Year_Month",
                schema: "dbo",
                table: "CommissionRun",
                columns: new[] { "Year", "Month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommissionLineDetail",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "CommissionLine",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "CommissionRun",
                schema: "dbo");

            migrationBuilder.CreateTable(
                name: "MonthlyPayoutRun",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApprovedBy = table.Column<long>(type: "bigint", nullable: true),
                    ApprovedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CalculatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TotalBusiness = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCommission = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyPayoutRun", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyPayoutLine",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MonthlyPayoutRunId = table.Column<long>(type: "bigint", nullable: false),
                    CreditStatus = table.Column<int>(type: "int", nullable: false),
                    CreditedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DifferentialCommission = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IntroRegNoAtRun = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: true),
                    LevelPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 2, nullable: false),
                    OwnBusinessCommission = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RegNo = table.Column<long>(type: "bigint", nullable: false),
                    SelfBusiness = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TeamBusiness = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCommission = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyPayoutLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyPayoutLine_MonthlyPayoutRun_MonthlyPayoutRunId",
                        column: x => x.MonthlyPayoutRunId,
                        principalSchema: "dbo",
                        principalTable: "MonthlyPayoutRun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyPayoutLineDetail",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MonthlyPayoutLineId = table.Column<long>(type: "bigint", nullable: false),
                    DifferentialAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ReferralLevelPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 2, nullable: false),
                    ReferralRegNo = table.Column<long>(type: "bigint", nullable: false),
                    ReferralSelfBusiness = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SponsorLevelPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyPayoutLineDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyPayoutLineDetail_MonthlyPayoutLine_MonthlyPayoutLineId",
                        column: x => x.MonthlyPayoutLineId,
                        principalSchema: "dbo",
                        principalTable: "MonthlyPayoutLine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyPayoutLine_CreditStatus",
                schema: "dbo",
                table: "MonthlyPayoutLine",
                column: "CreditStatus");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyPayoutLine_MonthlyPayoutRunId_RegNo",
                schema: "dbo",
                table: "MonthlyPayoutLine",
                columns: new[] { "MonthlyPayoutRunId", "RegNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyPayoutLine_RegNo",
                schema: "dbo",
                table: "MonthlyPayoutLine",
                column: "RegNo");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyPayoutLineDetail_MonthlyPayoutLineId",
                schema: "dbo",
                table: "MonthlyPayoutLineDetail",
                column: "MonthlyPayoutLineId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyPayoutRun_Status",
                schema: "dbo",
                table: "MonthlyPayoutRun",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyPayoutRun_Year_Month",
                schema: "dbo",
                table: "MonthlyPayoutRun",
                columns: new[] { "Year", "Month" },
                unique: true);
        }
    }
}
