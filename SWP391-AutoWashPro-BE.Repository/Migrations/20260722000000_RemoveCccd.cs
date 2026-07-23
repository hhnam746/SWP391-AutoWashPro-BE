using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SWP391_AutoWashPro_BE.Repository;

#nullable disable

namespace SWP391_AutoWashPro_BE.Repository.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260722000000_RemoveCccd")]
    public partial class RemoveCccd : Migration
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
