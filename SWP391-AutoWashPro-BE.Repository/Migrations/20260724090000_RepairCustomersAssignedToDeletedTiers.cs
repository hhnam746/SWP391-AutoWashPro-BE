using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP391_AutoWashPro_BE.Repository.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260724090000_RepairCustomersAssignedToDeletedTiers")]
    public partial class RepairCustomersAssignedToDeletedTiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM customer_profile AS customer
                        INNER JOIN tier AS assigned_tier
                            ON assigned_tier.id = customer.tier_id
                        WHERE assigned_tier."IsDeleted" = TRUE
                          AND NOT EXISTS (
                              SELECT 1
                              FROM tier AS active_tier
                              WHERE active_tier."IsDeleted" = FALSE
                                AND active_tier.required_washes <= customer.total_washes
                          )
                    ) THEN
                        RAISE EXCEPTION
                            'Cannot repair customers assigned to deleted tiers because no eligible active tier exists.';
                    END IF;
                END
                $$;

                UPDATE customer_profile AS customer
                SET tier_id = (
                        SELECT active_tier.id
                        FROM tier AS active_tier
                        WHERE active_tier."IsDeleted" = FALSE
                          AND active_tier.required_washes <= customer.total_washes
                        ORDER BY
                            active_tier.level DESC,
                            active_tier.required_washes DESC,
                            active_tier.id
                        LIMIT 1
                    ),
                    updated_at = NOW()
                WHERE EXISTS (
                    SELECT 1
                    FROM tier AS assigned_tier
                    WHERE assigned_tier.id = customer.tier_id
                      AND assigned_tier."IsDeleted" = TRUE
                );

                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM customer_profile AS customer
                        INNER JOIN tier AS assigned_tier
                            ON assigned_tier.id = customer.tier_id
                        WHERE assigned_tier."IsDeleted" = TRUE
                    ) THEN
                        RAISE EXCEPTION
                            'Tier repair completed with customers still assigned to deleted tiers.';
                    END IF;
                END
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException(
                "RepairCustomersAssignedToDeletedTiers is forward-only because the invalid deleted-tier assignments cannot be restored safely.");
        }
    }
}
