using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rawbit.Migrations
{
    /// <inheritdoc />
    public partial class AddHslAndCurveAdjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurvePointsJson",
                table: "Adjustments",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<int>(
                name: "CurvePointCount",
                table: "Adjustments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "HslAdjustmentsJson",
                table: "Adjustments",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurvePointsJson",
                table: "Adjustments");

            migrationBuilder.DropColumn(
                name: "CurvePointCount",
                table: "Adjustments");

            migrationBuilder.DropColumn(
                name: "HslAdjustmentsJson",
                table: "Adjustments");
        }
    }
}
