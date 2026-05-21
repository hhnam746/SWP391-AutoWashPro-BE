using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP391_AutoWashPro_BE.Repository.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "branch",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "text", nullable: false),
                    address = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branch", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "promotion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    discount_type = table.Column<string>(type: "text", nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    start_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_global = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reward",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "text", nullable: false),
                    reward_type = table.Column<string>(type: "text", nullable: false),
                    points_required = table.Column<int>(type: "integer", nullable: false),
                    quantity_available = table.Column<int>(type: "integer", nullable: false, defaultValue: -1),
                    valid_days = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reward", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "text", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    required_washes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    priority_booking_days = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tier", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    email = table.Column<string>(type: "text", nullable: true),
                    phone = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false, defaultValue: "customer"),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "active"),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "promotion_tier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    promotion_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_tier", x => x.id);
                    table.ForeignKey(
                        name: "FK_promotion_tier_promotion_promotion_id",
                        column: x => x.promotion_id,
                        principalTable: "promotion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_promotion_tier_tier_tier_id",
                        column: x => x.tier_id,
                        principalTable: "tier",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reward_tier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    reward_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reward_tier", x => x.id);
                    table.ForeignKey(
                        name: "FK_reward_tier_reward_reward_id",
                        column: x => x.reward_id,
                        principalTable: "reward",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reward_tier_tier_tier_id",
                        column: x => x.tier_id,
                        principalTable: "tier",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customer_profile",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    last_name = table.Column<string>(type: "text", nullable: false),
                    cccd = table.Column<string>(type: "text", nullable: true),
                    total_points = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_washes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_point_activity_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_profile", x => x.id);
                    table.ForeignKey(
                        name: "FK_customer_profile_tier_tier_id",
                        column: x => x.tier_id,
                        principalTable: "tier",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_profile_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    is_read = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification", x => x.id);
                    table.ForeignKey(
                        name: "FK_notification_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "system_config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    config_key = table.Column<string>(type: "text", nullable: false),
                    config_value = table.Column<string>(type: "jsonb", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_config", x => x.id);
                    table.ForeignKey(
                        name: "FK_system_config_user_updated_by",
                        column: x => x.updated_by,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_face_image",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    image_url = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_face_image", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_face_image_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vehicle",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    license_plate = table.Column<string>(type: "text", nullable: false),
                    brand = table.Column<string>(type: "text", nullable: true),
                    model = table.Column<string>(type: "text", nullable: true),
                    color = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle", x => x.id);
                    table.ForeignKey(
                        name: "FK_vehicle_customer_profile_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customer_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "voucher",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reward_id = table.Column<Guid>(type: "uuid", nullable: true),
                    promotion_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "active"),
                    discount_type = table.Column<string>(type: "text", nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_voucher", x => x.id);
                    table.ForeignKey(
                        name: "FK_voucher_customer_profile_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customer_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_voucher_promotion_promotion_id",
                        column: x => x.promotion_id,
                        principalTable: "promotion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_voucher_reward_reward_id",
                        column: x => x.reward_id,
                        principalTable: "reward",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "wallet",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    balance = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallet", x => x.id);
                    table.ForeignKey(
                        name: "FK_wallet_customer_profile_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customer_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "booking",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    voucher_id = table.Column<Guid>(type: "uuid", nullable: true),
                    booking_date = table.Column<DateOnly>(type: "date", nullable: false),
                    start_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    end_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "pending"),
                    base_price = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m),
                    final_price = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking", x => x.id);
                    table.ForeignKey(
                        name: "FK_booking_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_booking_customer_profile_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customer_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_booking_vehicle_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "vehicle",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_booking_voucher_voucher_id",
                        column: x => x.voucher_id,
                        principalTable: "voucher",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "point_transaction",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reward_id = table.Column<Guid>(type: "uuid", nullable: true),
                    points = table.Column<int>(type: "integer", nullable: false),
                    transaction_type = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_point_transaction", x => x.id);
                    table.ForeignKey(
                        name: "FK_point_transaction_booking_booking_id",
                        column: x => x.booking_id,
                        principalTable: "booking",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_point_transaction_customer_profile_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customer_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_point_transaction_reward_reward_id",
                        column: x => x.reward_id,
                        principalTable: "reward",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_booking_booking_date",
                table: "booking",
                column: "booking_date");

            migrationBuilder.CreateIndex(
                name: "IX_booking_branch_id",
                table: "booking",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_customer_id",
                table: "booking",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_start_time",
                table: "booking",
                column: "start_time");

            migrationBuilder.CreateIndex(
                name: "IX_booking_status",
                table: "booking",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_booking_vehicle_id",
                table: "booking",
                column: "vehicle_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_voucher_id",
                table: "booking",
                column: "voucher_id");

            migrationBuilder.CreateIndex(
                name: "IX_branch_is_active",
                table: "branch",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_customer_profile_cccd",
                table: "customer_profile",
                column: "cccd",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_profile_tier_id",
                table: "customer_profile",
                column: "tier_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_profile_user_id",
                table: "customer_profile",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_created_at",
                table: "notification",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_notification_is_read",
                table: "notification",
                column: "is_read");

            migrationBuilder.CreateIndex(
                name: "IX_notification_user_id",
                table: "notification",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_point_transaction_booking_id",
                table: "point_transaction",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_point_transaction_customer_id",
                table: "point_transaction",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_point_transaction_reward_id",
                table: "point_transaction",
                column: "reward_id");

            migrationBuilder.CreateIndex(
                name: "IX_point_transaction_transaction_type",
                table: "point_transaction",
                column: "transaction_type");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_end_date",
                table: "promotion",
                column: "end_date");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_is_active",
                table: "promotion",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_start_date",
                table: "promotion",
                column: "start_date");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_tier_promotion_id_tier_id",
                table: "promotion_tier",
                columns: new[] { "promotion_id", "tier_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_promotion_tier_tier_id",
                table: "promotion_tier",
                column: "tier_id");

            migrationBuilder.CreateIndex(
                name: "IX_reward_is_active",
                table: "reward",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_reward_reward_type",
                table: "reward",
                column: "reward_type");

            migrationBuilder.CreateIndex(
                name: "IX_reward_tier_reward_id_tier_id",
                table: "reward_tier",
                columns: new[] { "reward_id", "tier_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reward_tier_tier_id",
                table: "reward_tier",
                column: "tier_id");

            migrationBuilder.CreateIndex(
                name: "IX_system_config_config_key",
                table: "system_config",
                column: "config_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_system_config_updated_by",
                table: "system_config",
                column: "updated_by");

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

            migrationBuilder.CreateIndex(
                name: "IX_user_email",
                table: "user",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_phone",
                table: "user",
                column: "phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_role",
                table: "user",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "IX_user_status",
                table: "user",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_user_face_image_user_id",
                table: "user_face_image",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_customer_id",
                table: "vehicle",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_is_active",
                table: "vehicle",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_license_plate",
                table: "vehicle",
                column: "license_plate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_voucher_code",
                table: "voucher",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_voucher_customer_id",
                table: "voucher",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_voucher_expires_at",
                table: "voucher",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_voucher_promotion_id",
                table: "voucher",
                column: "promotion_id");

            migrationBuilder.CreateIndex(
                name: "IX_voucher_reward_id",
                table: "voucher",
                column: "reward_id");

            migrationBuilder.CreateIndex(
                name: "IX_voucher_status",
                table: "voucher",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_customer_id",
                table: "wallet",
                column: "customer_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification");

            migrationBuilder.DropTable(
                name: "point_transaction");

            migrationBuilder.DropTable(
                name: "promotion_tier");

            migrationBuilder.DropTable(
                name: "reward_tier");

            migrationBuilder.DropTable(
                name: "system_config");

            migrationBuilder.DropTable(
                name: "user_face_image");

            migrationBuilder.DropTable(
                name: "wallet");

            migrationBuilder.DropTable(
                name: "booking");

            migrationBuilder.DropTable(
                name: "branch");

            migrationBuilder.DropTable(
                name: "vehicle");

            migrationBuilder.DropTable(
                name: "voucher");

            migrationBuilder.DropTable(
                name: "customer_profile");

            migrationBuilder.DropTable(
                name: "promotion");

            migrationBuilder.DropTable(
                name: "reward");

            migrationBuilder.DropTable(
                name: "tier");

            migrationBuilder.DropTable(
                name: "user");
        }
    }
}
