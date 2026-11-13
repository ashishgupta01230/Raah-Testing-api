using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Binsera.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PaymentTypeId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaymentTypeId",
                table: "COMPANYBANKACCOUNT",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentTypeId",
                table: "COMPANYBANKACCOUNT");
        }
    }
}
