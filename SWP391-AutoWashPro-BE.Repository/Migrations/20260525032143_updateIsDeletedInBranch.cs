using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP391_AutoWashPro_BE.Repository.Migrations
{
    /// <inheritdoc />
    public partial class updateIsDeletedInBranch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "branch",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RedemAmount",
                table: "booking",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_branch_is_deleted",
                table: "branch",
                column: "is_deleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_branch_is_deleted",
                table: "branch");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "branch");

            migrationBuilder.DropColumn(
                name: "RedemAmount",
                table: "booking");
        }
    }
}
