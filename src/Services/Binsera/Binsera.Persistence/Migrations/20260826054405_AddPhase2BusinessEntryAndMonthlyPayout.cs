using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Binsera.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase2BusinessEntryAndMonthlyPayout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessEntry",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RegNo = table.Column<long>(type: "bigint", nullable: false),
                    EntryDate = table.Column<DateTime>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RejectedReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessEntry", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyPayoutRun",
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
                    table.PrimaryKey("PK_MonthlyPayoutRun", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceLevelSlab",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Level = table.Column<int>(type: "int", nullable: false),
                    MinTeamBusiness = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceLevelSlab", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessEntryAmendment",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessEntryId = table.Column<long>(type: "bigint", nullable: false),
                    PreviousAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AddedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NewAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AmendedBy = table.Column<long>(type: "bigint", nullable: false),
                    AmendedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessEntryAmendment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessEntryAmendment_BusinessEntry_BusinessEntryId",
                        column: x => x.BusinessEntryId,
                        principalSchema: "dbo",
                        principalTable: "BusinessEntry",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessEntryStatusHistory",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessEntryId = table.Column<long>(type: "bigint", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: false),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChangedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangedBy = table.Column<long>(type: "bigint", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessEntryStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessEntryStatusHistory_BusinessEntry_BusinessEntryId",
                        column: x => x.BusinessEntryId,
                        principalSchema: "dbo",
                        principalTable: "BusinessEntry",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyPayoutLine",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MonthlyPayoutRunId = table.Column<long>(type: "bigint", nullable: false),
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
                    ReferralRegNo = table.Column<long>(type: "bigint", nullable: false),
                    ReferralSelfBusiness = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReferralLevelPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 2, nullable: false),
                    SponsorLevelPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 2, nullable: false),
                    DifferentialAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
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

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "PerformanceLevelSlab",
                columns: new[] { "Id", "IsActive", "IsDeleted", "Level", "MinTeamBusiness", "Percentage" },
                values: new object[,]
                {
                    { 1L, true, false, 1, 100000m, 1m },
                    { 2L, true, false, 2, 500000m, 2m },
                    { 3L, true, false, 3, 1000000m, 3m },
                    { 4L, true, false, 4, 2500000m, 4m },
                    { 5L, true, false, 5, 10000000m, 5m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEntry_EntryDate",
                schema: "dbo",
                table: "BusinessEntry",
                column: "EntryDate");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEntry_RegNo_EntryDate",
                schema: "dbo",
                table: "BusinessEntry",
                columns: new[] { "RegNo", "EntryDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEntry_Status",
                schema: "dbo",
                table: "BusinessEntry",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEntryAmendment_BusinessEntryId",
                schema: "dbo",
                table: "BusinessEntryAmendment",
                column: "BusinessEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEntryStatusHistory_BusinessEntryId",
                schema: "dbo",
                table: "BusinessEntryStatusHistory",
                column: "BusinessEntryId");

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

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceLevelSlab_Level",
                schema: "dbo",
                table: "PerformanceLevelSlab",
                column: "Level",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessEntryAmendment",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "BusinessEntryStatusHistory",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "MonthlyPayoutLineDetail",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "PerformanceLevelSlab",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "BusinessEntry",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "MonthlyPayoutLine",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "MonthlyPayoutRun",
                schema: "dbo");
        }
    }
}
