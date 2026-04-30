using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPS_PROJECT.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDatabaseFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstAdminCreated",
                table: "SystemStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FirstAdminCreated",
                table: "SystemStatus",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
