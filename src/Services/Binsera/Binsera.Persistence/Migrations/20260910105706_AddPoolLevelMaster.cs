using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Binsera.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPoolLevelMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PoolLevelMaster",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LevelNo = table.Column<int>(type: "int", nullable: false),
                    RequiredMembers = table.Column<int>(type: "int", nullable: false),
                    ContributionPerMember = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TransferAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StepsUp = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoolLevelMaster", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "PoolLevelMaster",
                columns: new[] { "Id", "ContributionPerMember", "CreatedBy", "CreatedDate", "IsActive", "IsDeleted", "LevelNo", "ModifiedBy", "ModifiedDate", "RequiredMembers", "StepsUp", "TransferAmount" },
                values: new object[,]
                {
                    { 1L, 15m, 0L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, 0, null, null, 1, 1, 15m },
                    { 2L, 15m, 0L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, 1, null, null, 2, 2, 30m },
                    { 3L, 30m, 0L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, 2, null, null, 4, 3, 110m },
                    { 4L, 110m, 0L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, 3, null, null, 8, 4, 860m },
                    { 5L, 860m, 0L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, 4, null, null, 16, 5, 12760m },
                    { 6L, 12760m, 0L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, 5, null, null, 32, 6, 108320m },
                    { 7L, 108320m, 0L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, 6, null, null, 64, 7, 100000m },
                    { 8L, 100000m, 0L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, 7, null, null, 128, null, 0m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PoolLevelMaster_LevelNo",
                schema: "dbo",
                table: "PoolLevelMaster",
                column: "LevelNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PoolLevelMaster",
                schema: "dbo");
        }
    }
}
