using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP391_AutoWashPro_BE.Repository.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260731120000_ReserveBookingVouchers")]
    public partial class ReserveBookingVouchers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT booking.voucher_id
                        FROM booking
                        WHERE booking.voucher_id IS NOT NULL
                          AND booking.status = 'confirmed'
                        GROUP BY booking.voucher_id
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION
                            'Cannot reserve existing vouchers because at least one voucher is linked to multiple confirmed bookings.';
                    END IF;
                END
                $$;
                """);

            migrationBuilder.Sql(
                """
                UPDATE voucher
                SET status = 'reserved',
                    updated_at = NOW()
                WHERE status = 'active'
                  AND used_at IS NULL
                  AND EXISTS (
                      SELECT 1
                      FROM booking
                      WHERE booking.voucher_id = voucher.id
                        AND booking.status = 'confirmed'
                  );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE voucher
                SET status = CASE
                        WHEN expires_at > NOW() THEN 'active'
                        ELSE 'expired'
                    END,
                    updated_at = NOW()
                WHERE status = 'reserved';
                """);
        }
    }
}
