using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP391_AutoWashPro_BE.Repository.Migrations
{
    /// <inheritdoc />
    public partial class updatetier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tier_level",
                table: "tier");

            migrationBuilder.DropIndex(
                name: "IX_tier_name",
                table: "tier");

            migrationBuilder.CreateIndex(
                name: "IX_tier_level",
                table: "tier",
                column: "level",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_tier_name",
                table: "tier",
                column: "name",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tier_level",
                table: "tier");

            migrationBuilder.DropIndex(
                name: "IX_tier_name",
                table: "tier");

            migrationBuilder.CreateIndex(
                name: "IX_tier_level",
                table: "tier",
                column: "level",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tier_name",
                table: "tier",
                column: "name",
                unique: true);
        }
    }
}
