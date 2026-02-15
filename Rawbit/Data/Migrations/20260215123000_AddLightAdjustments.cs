using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rawbit.Migrations
{
    /// <inheritdoc />
    public partial class AddLightAdjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "Highlights",
                table: "Adjustments",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Temperature",
                table: "Adjustments",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Tint",
                table: "Adjustments",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Highlights",
                table: "Adjustments");

            migrationBuilder.DropColumn(
                name: "Temperature",
                table: "Adjustments");

            migrationBuilder.DropColumn(
                name: "Tint",
                table: "Adjustments");
        }
    }
}
