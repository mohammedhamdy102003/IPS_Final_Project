using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPS_PROJECT.Migrations
{
    /// <inheritdoc />
    public partial class AddAttackTypeRow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttackType",
                table: "Events",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttackType",
                table: "Events");
        }
    }
}
