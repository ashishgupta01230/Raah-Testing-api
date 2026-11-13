using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Binsera.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropLeadsAndPropertiesTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.DropTable(name: "TaskItem");
            migrationBuilder.DropTable(name: "TaskStatus");
            migrationBuilder.DropTable(name: "Note");
            migrationBuilder.DropTable(name: "LeadTag");
            migrationBuilder.DropTable(name: "Comment");
            migrationBuilder.DropTable(name: "Activity");
            migrationBuilder.DropTable(name: "ActivityType");
            migrationBuilder.DropTable(name: "Lead");
            migrationBuilder.DropTable(name: "LeadStatus");
            migrationBuilder.DropTable(name: "Contact");
            migrationBuilder.DropTable(name: "LeadSource");
            migrationBuilder.DropTable(name: "Tag");
            migrationBuilder.DropTable(name: "PropertyFieldValue");
            migrationBuilder.DropTable(name: "PropertyFieldOptions");
            migrationBuilder.DropTable(name: "PropertyFields");
            migrationBuilder.DropTable(name: "PropertyFile");
            migrationBuilder.DropTable(name: "Property");
            migrationBuilder.DropTable(name: "PropertyType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
