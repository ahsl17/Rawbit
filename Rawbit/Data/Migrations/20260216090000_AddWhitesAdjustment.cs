using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rawbit.Migrations
{
    /// <inheritdoc />
    public partial class AddWhitesAdjustment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "Whites",
                table: "Adjustments",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Whites",
                table: "Adjustments");
        }
    }
}
