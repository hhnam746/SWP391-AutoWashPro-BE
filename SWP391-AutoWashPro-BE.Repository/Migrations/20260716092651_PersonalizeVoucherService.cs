using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP391_AutoWashPro_BE.Repository.Migrations
{
    /// <inheritdoc />
    public partial class PersonalizeVoucherService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "verified_at",
                table: "user",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "date_of_birth",
                table: "customer_profile",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "date_of_birth_set_at",
                table: "customer_profile",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "customer_date_of_birth_correction",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admin_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    new_date_of_birth = table.Column<DateOnly>(type: "date", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_date_of_birth_correction", x => x.id);
                    table.ForeignKey(
                        name: "FK_customer_date_of_birth_correction_customer_profile_customer~",
                        column: x => x.customer_id,
                        principalTable: "customer_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_date_of_birth_correction_user_admin_user_id",
                        column: x => x.admin_user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "personalized_promotion_rule",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    promotion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trigger_type = table.Column<string>(type: "text", nullable: false),
                    threshold_days = table.Column<int>(type: "integer", nullable: true),
                    voucher_validity_days = table.Column<int>(type: "integer", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    send_in_app_notification = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    send_email = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    notification_title_template = table.Column<string>(type: "text", nullable: true),
                    notification_content_template = table.Column<string>(type: "text", nullable: true),
                    email_subject_template = table.Column<string>(type: "text", nullable: true),
                    email_body_template = table.Column<string>(type: "text", nullable: true),
                    call_to_action_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personalized_promotion_rule", x => x.id);
                    table.ForeignKey(
                        name: "FK_personalized_promotion_rule_promotion_promotion_id",
                        column: x => x.promotion_id,
                        principalTable: "promotion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "personalized_voucher_issuance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    promotion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    promotion_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    voucher_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trigger_type = table.Column<string>(type: "text", nullable: false),
                    cycle_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    trigger_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    notification_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notification_status = table.Column<string>(type: "text", nullable: false),
                    notification_attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    notification_last_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    notification_sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    notification_last_error = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    email_status = table.Column<string>(type: "text", nullable: false),
                    email_attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    email_last_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    email_sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    email_last_error = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personalized_voucher_issuance", x => x.id);
                    table.ForeignKey(
                        name: "FK_personalized_voucher_issuance_customer_profile_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customer_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_personalized_voucher_issuance_personalized_promotion_rule_p~",
                        column: x => x.promotion_rule_id,
                        principalTable: "personalized_promotion_rule",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_personalized_voucher_issuance_promotion_promotion_id",
                        column: x => x.promotion_id,
                        principalTable: "promotion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_personalized_voucher_issuance_voucher_voucher_id",
                        column: x => x.voucher_id,
                        principalTable: "voucher",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customer_date_of_birth_correction_admin_user_id",
                table: "customer_date_of_birth_correction",
                column: "admin_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_date_of_birth_correction_created_at",
                table: "customer_date_of_birth_correction",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_customer_date_of_birth_correction_customer_id",
                table: "customer_date_of_birth_correction",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_personalized_promotion_rule_is_active_trigger_type",
                table: "personalized_promotion_rule",
                columns: new[] { "is_active", "trigger_type" });

            migrationBuilder.CreateIndex(
                name: "IX_personalized_promotion_rule_promotion_id",
                table: "personalized_promotion_rule",
                column: "promotion_id");

            migrationBuilder.CreateIndex(
                name: "IX_personalized_promotion_rule_threshold_days",
                table: "personalized_promotion_rule",
                column: "threshold_days");

            migrationBuilder.CreateIndex(
                name: "IX_personalized_promotion_rule_trigger_type",
                table: "personalized_promotion_rule",
                column: "trigger_type");

            migrationBuilder.CreateIndex(
                name: "IX_personalized_voucher_issuance_customer_id",
                table: "personalized_voucher_issuance",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_personalized_voucher_issuance_email_status_email_attempt_co~",
                table: "personalized_voucher_issuance",
                columns: new[] { "email_status", "email_attempt_count" });

            migrationBuilder.CreateIndex(
                name: "IX_personalized_voucher_issuance_notification_id",
                table: "personalized_voucher_issuance",
                column: "notification_id",
                unique: true,
                filter: "notification_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_personalized_voucher_issuance_notification_status_notificat~",
                table: "personalized_voucher_issuance",
                columns: new[] { "notification_status", "notification_attempt_count" });

            migrationBuilder.CreateIndex(
                name: "IX_personalized_voucher_issuance_promotion_id",
                table: "personalized_voucher_issuance",
                column: "promotion_id");

            migrationBuilder.CreateIndex(
                name: "IX_personalized_voucher_issuance_promotion_rule_id",
                table: "personalized_voucher_issuance",
                column: "promotion_rule_id");

            migrationBuilder.CreateIndex(
                name: "IX_personalized_voucher_issuance_voucher_id",
                table: "personalized_voucher_issuance",
                column: "voucher_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_personalized_voucher_issuance_customer_trigger_cycle",
                table: "personalized_voucher_issuance",
                columns: new[] { "customer_id", "trigger_type", "cycle_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_date_of_birth_correction");

            migrationBuilder.DropTable(
                name: "personalized_voucher_issuance");

            migrationBuilder.DropTable(
                name: "personalized_promotion_rule");

            migrationBuilder.DropColumn(
                name: "verified_at",
                table: "user");

            migrationBuilder.DropColumn(
                name: "date_of_birth",
                table: "customer_profile");

            migrationBuilder.DropColumn(
                name: "date_of_birth_set_at",
                table: "customer_profile");
        }
    }
}
