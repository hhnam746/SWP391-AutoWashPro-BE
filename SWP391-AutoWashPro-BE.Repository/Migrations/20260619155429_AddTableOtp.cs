using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP391_AutoWashPro_BE.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddTableOtp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "otp",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    otp_hash = table.Column<string>(type: "text", nullable: false),
                    failed_attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_used = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_otp", x => x.id);
                    table.ForeignKey(
                        name: "FK_otp_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_otp_expires_at",
                table: "otp",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_otp_is_used",
                table: "otp",
                column: "is_used");

            migrationBuilder.CreateIndex(
                name: "IX_otp_user_id",
                table: "otp",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "otp");
        }
    }
}
