using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rawbit.Migrations
{
    /// <inheritdoc />
    public partial class AddBlacksAdjustment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "Blacks",
                table: "Adjustments",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Blacks",
                table: "Adjustments");
        }
    }
}
