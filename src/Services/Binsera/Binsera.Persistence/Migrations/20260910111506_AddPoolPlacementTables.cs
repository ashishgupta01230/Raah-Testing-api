using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Binsera.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPoolPlacementTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PoolSeedAccount",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SeedIndex = table.Column<int>(type: "int", nullable: false),
                    MemberRegNo = table.Column<long>(type: "bigint", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoolSeedAccount", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PoolWalletTransaction",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberRegNo = table.Column<long>(type: "bigint", nullable: true),
                    WalletType = table.Column<int>(type: "int", nullable: false),
                    Credit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Debit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LevelNo = table.Column<int>(type: "int", nullable: false),
                    SourceRegNo = table.Column<long>(type: "bigint", nullable: true),
                    SeedIndex = table.Column<int>(type: "int", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    OccurredOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoolWalletTransaction", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UplinePath",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberRegNo = table.Column<long>(type: "bigint", nullable: false),
                    AncestorRegNo = table.Column<long>(type: "bigint", nullable: false),
                    Depth = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UplinePath", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "PoolSeedAccount",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "IsDeleted", "MemberRegNo", "ModifiedBy", "ModifiedDate", "SeedIndex" },
                values: new object[,]
                {
                    { 1L, 0L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, 1L, null, null, 1 },
                    { 2L, 0L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, 2L, null, null, 2 },
                    { 3L, 0L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, 3L, null, null, 3 },
                    { 4L, 0L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, 4L, null, null, 4 },
                    { 5L, 0L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, 5L, null, null, 5 },
                    { 6L, 0L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, 6L, null, null, 6 },
                    { 7L, 0L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, 7L, null, null, 7 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PoolSeedAccount_MemberRegNo",
                schema: "dbo",
                table: "PoolSeedAccount",
                column: "MemberRegNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PoolSeedAccount_SeedIndex",
                schema: "dbo",
                table: "PoolSeedAccount",
                column: "SeedIndex",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PoolWalletTransaction_IdempotencyKey_WalletType",
                schema: "dbo",
                table: "PoolWalletTransaction",
                columns: new[] { "IdempotencyKey", "WalletType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PoolWalletTransaction_MemberRegNo_WalletType",
                schema: "dbo",
                table: "PoolWalletTransaction",
                columns: new[] { "MemberRegNo", "WalletType" });

            migrationBuilder.CreateIndex(
                name: "IX_PoolWalletTransaction_OccurredOn",
                schema: "dbo",
                table: "PoolWalletTransaction",
                column: "OccurredOn");

            migrationBuilder.CreateIndex(
                name: "IX_UplinePath_AncestorRegNo",
                schema: "dbo",
                table: "UplinePath",
                column: "AncestorRegNo");

            migrationBuilder.CreateIndex(
                name: "IX_UplinePath_MemberRegNo_Depth",
                schema: "dbo",
                table: "UplinePath",
                columns: new[] { "MemberRegNo", "Depth" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PoolSeedAccount",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "PoolWalletTransaction",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "UplinePath",
                schema: "dbo");
        }
    }
}
