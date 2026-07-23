using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP391_AutoWashPro_BE.Repository.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCCCD : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_customer_profile_cccd",
                table: "customer_profile");

            migrationBuilder.DropColumn(
                name: "cccd",
                table: "customer_profile");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cccd",
                table: "customer_profile",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_profile_cccd",
                table: "customer_profile",
                column: "cccd",
                unique: true);
        }
    }
}
