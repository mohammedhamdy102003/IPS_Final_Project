using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPS_PROJECT.Migrations
{
    /// <inheritdoc />
    public partial class RequestedRoleAndAdminCreated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FirstAdminCreated",
                table: "SystemStatus",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RequestedRole",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstAdminCreated",
                table: "SystemStatus");

            migrationBuilder.DropColumn(
                name: "RequestedRole",
                table: "AspNetUsers");
        }
    }
}
