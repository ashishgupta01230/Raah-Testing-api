using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Binsera.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MemberAdditionalInfoMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MemberAdditionalInfo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: false),
                    MemberInfo = table.Column<string>(type: "NVARCHAR(MAX)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberAdditionalInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberAdditionalInfo_member_Master_Id",
                        column: x => x.Id,
                        principalTable: "member_Master",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberAdditionalInfo");
        }
    }
}
