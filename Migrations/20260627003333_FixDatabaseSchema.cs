using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPS_PROJECT.Migrations
{
    /// <inheritdoc />
    public partial class FixDatabaseSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrafficType",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Protocol",
                table: "AlertNotifications");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TrafficType",
                table: "Events",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Protocol",
                table: "AlertNotifications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
