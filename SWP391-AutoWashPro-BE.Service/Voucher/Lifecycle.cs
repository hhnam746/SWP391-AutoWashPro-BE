using Microsoft.EntityFrameworkCore;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Enums;
using SWP391_AutoWashPro_BE.Service.Models;

namespace SWP391_AutoWashPro_BE.Service.Voucher;

public enum VoucherReleaseResult
{
    NoChange,
    Active,
    Expired
}

public static class Lifecycle
{
    public static Task<int> ReserveAsync(
        AppDbContext dbContext,
        Guid voucherId,
        Guid customerId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Vouchers
            .Where(voucher =>
                voucher.Id == voucherId &&
                voucher.CustomerId == customerId &&
                voucher.Status == VoucherStatus.Active &&
                voucher.UsedAt == null &&
                voucher.ExpiresAt > nowUtc)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(voucher => voucher.Status, VoucherStatus.Reserved)
                    .SetProperty(voucher => voucher.UpdatedAt, nowUtc),
                cancellationToken);
    }

    public static async Task ThrowReservationFailureAsync(
        AppDbContext dbContext,
        Guid voucherId,
        Guid customerId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        var voucher = await dbContext.Vouchers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == voucherId, cancellationToken);

        if (voucher == null)
        {
            throw new KeyNotFoundException("Voucher not found.");
        }

        if (voucher.CustomerId != customerId)
        {
            throw new ForbiddenAccessException("Voucher does not belong to the current customer.");
        }

        if (voucher.Status == VoucherStatus.Used || voucher.UsedAt.HasValue)
        {
            throw new InvalidOperationException("Voucher already used.");
        }

        if (voucher.Status == VoucherStatus.Reserved)
        {
            throw new InvalidOperationException("Voucher is reserved by another booking.");
        }

        if (voucher.Status == VoucherStatus.Expired || voucher.ExpiresAt <= nowUtc)
        {
            throw new InvalidOperationException("Voucher expired.");
        }

        throw new InvalidOperationException("Voucher is no longer available.");
    }

    public static Task<int> MarkReservedAsUsedAsync(
        AppDbContext dbContext,
        Guid voucherId,
        Guid bookingId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Vouchers
            .Where(voucher =>
                voucher.Id == voucherId &&
                voucher.Status == VoucherStatus.Reserved &&
                voucher.UsedAt == null &&
                voucher.Bookings.Any(booking =>
                    booking.Id == bookingId &&
                    booking.VoucherId == voucher.Id &&
                    booking.Status == BookingStatus.Confirmed))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(voucher => voucher.Status, VoucherStatus.Used)
                    .SetProperty(voucher => voucher.UsedAt, nowUtc)
                    .SetProperty(voucher => voucher.UpdatedAt, nowUtc),
                cancellationToken);
    }

    public static async Task<VoucherReleaseResult> ReleaseReservedAsync(
        AppDbContext dbContext,
        Guid voucherId,
        Guid bookingId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        var linkedReservedVouchers = dbContext.Vouchers
            .Where(voucher =>
                voucher.Id == voucherId &&
                voucher.Status == VoucherStatus.Reserved &&
                voucher.UsedAt == null &&
                voucher.Bookings.Any(booking =>
                    booking.Id == bookingId &&
                    booking.VoucherId == voucher.Id));

        var expiredCount = await linkedReservedVouchers
            .Where(voucher => voucher.ExpiresAt <= nowUtc)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(voucher => voucher.Status, VoucherStatus.Expired)
                    .SetProperty(voucher => voucher.UpdatedAt, nowUtc),
                cancellationToken);
        if (expiredCount == 1)
        {
            return VoucherReleaseResult.Expired;
        }

        var activeCount = await linkedReservedVouchers
            .Where(voucher => voucher.ExpiresAt > nowUtc)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(voucher => voucher.Status, VoucherStatus.Active)
                    .SetProperty(voucher => voucher.UpdatedAt, nowUtc),
                cancellationToken);

        return activeCount == 1
            ? VoucherReleaseResult.Active
            : VoucherReleaseResult.NoChange;
    }
}
