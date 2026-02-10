using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rawbit.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Adjustments",
                columns: table => new
                {
                    AdjustmentsId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Exposure = table.Column<float>(type: "REAL", nullable: false),
                    Shadows = table.Column<float>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Adjustments", x => x.AdjustmentsId);
                });

            migrationBuilder.CreateTable(
                name: "Images",
                columns: table => new
                {
                    ImageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    AdjustmentsId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.ImageId);
                    table.ForeignKey(
                        name: "FK_Images_Adjustments_AdjustmentsId",
                        column: x => x.AdjustmentsId,
                        principalTable: "Adjustments",
                        principalColumn: "AdjustmentsId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Images_AdjustmentsId",
                table: "Images",
                column: "AdjustmentsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Images");

            migrationBuilder.DropTable(
                name: "Adjustments");
        }
    }
}
