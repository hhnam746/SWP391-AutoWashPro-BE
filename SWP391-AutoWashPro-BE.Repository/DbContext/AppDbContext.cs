using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SWP391_AutoWashPro_BE.Repository.Entities;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Repository;

public class AppDbContext : DbContext
{
    private static readonly ValueConverter<UserRole, string> UserRoleConverter = new(v => ToDbUserRole(v), v => FromDbUserRole(v));
    private static readonly ValueConverter<AccountStatus, string> AccountStatusConverter = new(v => ToDbAccountStatus(v), v => FromDbAccountStatus(v));
    private static readonly ValueConverter<BookingStatus, string> BookingStatusConverter = new(v => ToDbBookingStatus(v), v => FromDbBookingStatus(v));
    private static readonly ValueConverter<DiscountType, string> DiscountTypeConverter = new(v => ToDbDiscountType(v), v => FromDbDiscountType(v));
    private static readonly ValueConverter<RewardType, string> RewardTypeConverter = new(v => ToDbRewardType(v), v => FromDbRewardType(v));
    private static readonly ValueConverter<VoucherStatus, string> VoucherStatusConverter = new(v => ToDbVoucherStatus(v), v => FromDbVoucherStatus(v));
    private static readonly ValueConverter<NotificationType, string> NotificationTypeConverter = new(v => ToDbNotificationType(v), v => FromDbNotificationType(v));
    private static readonly ValueConverter<PointTransactionType, string> PointTransactionTypeConverter = new(v => ToDbPointTransactionType(v), v => FromDbPointTransactionType(v));
    
    
    private static string ToDbUserRole(UserRole value) => value switch
    {
        UserRole.Admin => "admin",
        UserRole.Customer => "customer",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static UserRole FromDbUserRole(string value) => value switch
    {
        "admin" => UserRole.Admin,
        "customer" => UserRole.Customer,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static string ToDbAccountStatus(AccountStatus value) => value switch
    {
        AccountStatus.Active => "active",
        AccountStatus.Locked => "locked",
        AccountStatus.Inactive => "inactive",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static AccountStatus FromDbAccountStatus(string value) => value switch
    {
        "active" => AccountStatus.Active,
        "locked" => AccountStatus.Locked,
        "inactive" => AccountStatus.Inactive,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static string ToDbBookingStatus(BookingStatus value) => value switch
    {
        BookingStatus.Pending => "pending",
        BookingStatus.Confirmed => "confirmed",
        BookingStatus.CheckIn => "check_in",
        BookingStatus.InProgress => "in_progress",
        BookingStatus.Completed => "completed",
        BookingStatus.Cancelled => "cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static BookingStatus FromDbBookingStatus(string value) => value switch
    {
        "pending" => BookingStatus.Pending,
        "confirmed" => BookingStatus.Confirmed,
        "check_in" => BookingStatus.CheckIn,
        "in_progress" => BookingStatus.InProgress,
        "completed" => BookingStatus.Completed,
        "cancelled" => BookingStatus.Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static string ToDbDiscountType(DiscountType value) => value switch
    {
        DiscountType.Percentage => "percentage",
        DiscountType.FixedAmount => "fixed_amount",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static DiscountType FromDbDiscountType(string value) => value switch
    {
        "percentage" => DiscountType.Percentage,
        "fixed_amount" => DiscountType.FixedAmount,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static string ToDbRewardType(RewardType value) => value switch
    {
        RewardType.FreeWash => "free_wash",
        RewardType.Voucher => "voucher",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static RewardType FromDbRewardType(string value) => value switch
    {
        "free_wash" => RewardType.FreeWash,
        "voucher" => RewardType.Voucher,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static string ToDbVoucherStatus(VoucherStatus value) => value switch
    {
        VoucherStatus.Active => "active",
        VoucherStatus.Used => "used",
        VoucherStatus.Expired => "expired",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static VoucherStatus FromDbVoucherStatus(string value) => value switch
    {
        "active" => VoucherStatus.Active,
        "used" => VoucherStatus.Used,
        "expired" => VoucherStatus.Expired,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static string ToDbNotificationType(NotificationType value) => value switch
    {
        NotificationType.BookingCreated => "booking_created",
        NotificationType.BookingReminder => "booking_reminder",
        NotificationType.BookingCancelled => "booking_cancelled",
        NotificationType.BookingCompleted => "booking_completed",
        NotificationType.TierUpgraded => "tier_upgraded",
        NotificationType.RewardRedeemed => "reward_redeemed",
        NotificationType.SystemAlert => "system_alert",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static NotificationType FromDbNotificationType(string value) => value switch
    {
        "booking_created" => NotificationType.BookingCreated,
        "booking_reminder" => NotificationType.BookingReminder,
        "booking_cancelled" => NotificationType.BookingCancelled,
        "booking_completed" => NotificationType.BookingCompleted,
        "tier_upgraded" => NotificationType.TierUpgraded,
        "reward_redeemed" => NotificationType.RewardRedeemed,
        "system_alert" => NotificationType.SystemAlert,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static string ToDbPointTransactionType(PointTransactionType value) => value switch
    {
        PointTransactionType.Earn => "earn",
        PointTransactionType.Redeem => "redeem",
        PointTransactionType.Reset => "reset",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static PointTransactionType FromDbPointTransactionType(string value) => value switch
    {
        "earn" => PointTransactionType.Earn,
        "redeem" => PointTransactionType.Redeem,
        "reset" => PointTransactionType.Reset,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Wallet> Wallets { get; set; }
    public DbSet<CustomerProfile> CustomerProfiles { get; set; }
    public DbSet<UserFaceImage> UserFaceImages { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<Tier> Tiers { get; set; }
    public DbSet<Branch> Branches { get; set; }
    public DbSet<SystemConfig> SystemConfigs { get; set; }
    public DbSet<Promotion> Promotions { get; set; }
    public DbSet<PromotionTier> PromotionTiers { get; set; }
    public DbSet<Reward> Rewards { get; set; }
    public DbSet<RewardTier> RewardTiers { get; set; }
    public DbSet<Voucher> Vouchers { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<PointTransaction> PointTransactions { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("user");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            builder.Property(x => x.Email).HasColumnName("email");
            builder.Property(x => x.Phone).HasColumnName("phone").IsRequired();
            builder.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired();
            builder.Property(x => x.Role).HasColumnName("role").HasConversion(UserRoleConverter).HasDefaultValue(UserRole.Customer).IsRequired();
            builder.Property(x => x.Status).HasColumnName("status").HasConversion(AccountStatusConverter).HasDefaultValue(AccountStatus.Active).IsRequired();
            builder.Property(x => x.isVerify).HasColumnName("is_verify").HasDefaultValue(false).IsRequired();
            builder.Property(x => x.LastLoginAt).HasColumnName("last_login_at");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => x.Email).IsUnique();
            builder.HasIndex(x => x.Phone).IsUnique();
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.Role);
        });

        modelBuilder.Entity<Wallet>(builder =>
        {
            builder.ToTable("wallet");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            builder.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
            builder.Property(x => x.Balance).HasColumnName("balance").HasColumnType("numeric(12,2)").HasDefaultValue(0m).IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => x.CustomerId).IsUnique();

            builder.HasOne(x => x.Customer)
                .WithOne(x => x.Wallet)
                .HasForeignKey<Wallet>(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CustomerProfile>(builder =>
        {
            builder.ToTable("customer_profile");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            builder.Property(x => x.TierId).HasColumnName("tier_id").IsRequired();
            builder.Property(x => x.FirstName).HasColumnName("first_name").IsRequired();
            builder.Property(x => x.LastName).HasColumnName("last_name").IsRequired();
            builder.Property(x => x.Cccd).HasColumnName("cccd");
            builder.Property(x => x.TotalPoints).HasColumnName("total_points").HasDefaultValue(0).IsRequired();
            builder.Property(x => x.TotalWashes).HasColumnName("total_washes").HasDefaultValue(0).IsRequired();
            builder.Property(x => x.LastPointActivityAt).HasColumnName("last_point_activity_at");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => x.UserId).IsUnique();
            builder.HasIndex(x => x.TierId);
            builder.HasIndex(x => x.Cccd).IsUnique();

            builder.HasOne(x => x.User)
                .WithOne(x => x.CustomerProfile)
                .HasForeignKey<CustomerProfile>(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Tier)
                .WithMany(x => x.CustomerProfiles)
                .HasForeignKey(x => x.TierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserFaceImage>(builder =>
        {
            builder.ToTable("user_face_image");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            builder.Property(x => x.ImageUrl).HasColumnName("image_url").IsRequired();
            builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(false).IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => x.UserId);

            builder.HasOne(x => x.User)
                .WithMany(x => x.UserFaceImages)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Vehicle>(builder =>
        {
            builder.ToTable("vehicle");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            builder.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
            builder.Property(x => x.LicensePlate).HasColumnName("license_plate").IsRequired();
            builder.Property(x => x.Brand).HasColumnName("brand");
            builder.Property(x => x.Model).HasColumnName("model");
            builder.Property(x => x.Color).HasColumnName("color");
            builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");

            builder.HasIndex(x => x.CustomerId);
            builder.HasIndex(x => x.LicensePlate).IsUnique();
            builder.HasIndex(x => x.IsActive);

            builder.HasOne(x => x.Customer)
                .WithMany(x => x.Vehicles)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Tier>(builder =>
        {
            builder.ToTable("tier");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            builder.Property(x => x.Name).HasColumnName("name").IsRequired();
            builder.Property(x => x.Level).HasColumnName("level").IsRequired();
            builder.Property(x => x.RequiredWashes).HasColumnName("required_washes").HasDefaultValue(0).IsRequired();
            builder.Property(x => x.PriorityBookingDays).HasColumnName("priority_booking_days").HasDefaultValue(0).IsRequired();
            builder.Property(x => x.Description).HasColumnName("description").HasColumnType("text");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => x.Name).IsUnique();
            builder.HasIndex(x => x.Level).IsUnique();
        });

        modelBuilder.Entity<Branch>(builder =>
        {
            builder.ToTable("branch");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            builder.Property(x => x.Name).HasColumnName("name").IsRequired();
            builder.Property(x => x.Address).HasColumnName("address").HasColumnType("text").IsRequired();
            builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => x.IsActive);
        });

        modelBuilder.Entity<SystemConfig>(builder =>
        {
            builder.ToTable("system_config");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            builder.Property(x => x.ConfigKey).HasColumnName("config_key").IsRequired();
            builder.Property(x => x.ConfigValue).HasColumnName("config_value").HasColumnType("jsonb").IsRequired();
            builder.Property(x => x.Description).HasColumnName("description").HasColumnType("text");
            builder.Property(x => x.UpdatedById).HasColumnName("updated_by");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => x.ConfigKey).IsUnique();

            builder.HasOne(x => x.UpdatedBy)
                .WithMany(x => x.UpdatedSystemConfigs)
                .HasForeignKey(x => x.UpdatedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Promotion>(builder =>
        {
            builder.ToTable("promotion");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            builder.Property(x => x.Name).HasColumnName("name").IsRequired();
            builder.Property(x => x.Description).HasColumnName("description").HasColumnType("text");
            builder.Property(x => x.DiscountType).HasColumnName("discount_type").HasConversion(DiscountTypeConverter).IsRequired();
            builder.Property(x => x.DiscountValue).HasColumnName("discount_value").HasColumnType("numeric(12,2)").IsRequired();
            builder.Property(x => x.StartDate).HasColumnName("start_date").IsRequired();
            builder.Property(x => x.EndDate).HasColumnName("end_date").IsRequired();
            builder.Property(x => x.IsGlobal).HasColumnName("is_global").HasDefaultValue(false);
            builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => x.IsActive);
            builder.HasIndex(x => x.StartDate);
            builder.HasIndex(x => x.EndDate);
        });

        modelBuilder.Entity<PromotionTier>(builder =>
        {
            builder.ToTable("promotion_tier");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            builder.Property(x => x.PromotionId).HasColumnName("promotion_id").IsRequired();
            builder.Property(x => x.TierId).HasColumnName("tier_id").IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => new { x.PromotionId, x.TierId }).IsUnique();

            builder.HasOne(x => x.Promotion)
                .WithMany(x => x.PromotionTiers)
                .HasForeignKey(x => x.PromotionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Tier)
                .WithMany(x => x.PromotionTiers)
                .HasForeignKey(x => x.TierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Reward>(builder =>
        {
            builder.ToTable("reward");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            builder.Property(x => x.Name).HasColumnName("name").IsRequired();
            builder.Property(x => x.RewardType).HasColumnName("reward_type").HasConversion(RewardTypeConverter).IsRequired();
            builder.Property(x => x.PointsRequired).HasColumnName("points_required").IsRequired();
            builder.Property(x => x.QuantityAvailable).HasColumnName("quantity_available").HasDefaultValue(-1).IsRequired();
            builder.Property(x => x.ValidDays).HasColumnName("valid_days").HasDefaultValue(30).IsRequired();
            builder.Property(x => x.Description).HasColumnName("description").HasColumnType("text");
            builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => x.RewardType);
            builder.HasIndex(x => x.IsActive);
        });

        modelBuilder.Entity<RewardTier>(builder =>
        {
            builder.ToTable("reward_tier");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            builder.Property(x => x.RewardId).HasColumnName("reward_id").IsRequired();
            builder.Property(x => x.TierId).HasColumnName("tier_id").IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => new { x.RewardId, x.TierId }).IsUnique();

            builder.HasOne(x => x.Reward)
                .WithMany(x => x.RewardTiers)
                .HasForeignKey(x => x.RewardId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Tier)
                .WithMany(x => x.RewardTiers)
                .HasForeignKey(x => x.TierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Voucher>(builder =>
        {
            builder.ToTable("voucher");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            builder.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
            builder.Property(x => x.RewardId).HasColumnName("reward_id");
            builder.Property(x => x.PromotionId).HasColumnName("promotion_id");
            builder.Property(x => x.Code).HasColumnName("code").IsRequired();
            builder.Property(x => x.Status).HasColumnName("status").HasConversion(VoucherStatusConverter).HasDefaultValue(VoucherStatus.Active).IsRequired();
            builder.Property(x => x.DiscountType).HasColumnName("discount_type").HasConversion(DiscountTypeConverter).IsRequired();
            builder.Property(x => x.DiscountValue).HasColumnName("discount_value").HasColumnType("numeric(12,2)").IsRequired();
            builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").IsRequired();
            builder.Property(x => x.UsedAt).HasColumnName("used_at");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => x.CustomerId);
            builder.HasIndex(x => x.Code).IsUnique();
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.ExpiresAt);

            builder.HasOne(x => x.Customer)
                .WithMany(x => x.Vouchers)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Reward)
                .WithMany(x => x.Vouchers)
                .HasForeignKey(x => x.RewardId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Promotion)
                .WithMany(x => x.Vouchers)
                .HasForeignKey(x => x.PromotionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Booking>(builder =>
        {
            builder.ToTable("booking");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            builder.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
            builder.Property(x => x.VehicleId).HasColumnName("vehicle_id").IsRequired();
            builder.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
            builder.Property(x => x.VoucherId).HasColumnName("voucher_id");
            builder.Property(x => x.BookingDate).HasColumnName("booking_date").HasColumnType("date").IsRequired();
            builder.Property(x => x.StartTime).HasColumnName("start_time").IsRequired();
            builder.Property(x => x.EndTime).HasColumnName("end_time").IsRequired();
            builder.Property(x => x.Status).HasColumnName("status").HasConversion(BookingStatusConverter).HasDefaultValue(BookingStatus.Pending).IsRequired();
            builder.Property(x => x.BasePrice).HasColumnName("base_price").HasColumnType("numeric(12,2)").IsRequired();
            builder.Property(x => x.DiscountAmount).HasColumnName("discount_amount").HasColumnType("numeric(12,2)").HasDefaultValue(0m).IsRequired();
            builder.Property(x => x.FinalPrice).HasColumnName("final_price").HasColumnType("numeric(12,2)").IsRequired();
            builder.Property(x => x.CancelledAt).HasColumnName("cancelled_at");
            builder.Property(x => x.CompletedAt).HasColumnName("completed_at");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => x.CustomerId);
            builder.HasIndex(x => x.VehicleId);
            builder.HasIndex(x => x.BranchId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.BookingDate);
            builder.HasIndex(x => x.StartTime);

            builder.HasOne(x => x.Customer)
                .WithMany(x => x.Bookings)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Vehicle)
                .WithMany(x => x.Bookings)
                .HasForeignKey(x => x.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Branch)
                .WithMany(x => x.Bookings)
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Voucher)
                .WithMany(x => x.Bookings)
                .HasForeignKey(x => x.VoucherId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PointTransaction>(builder =>
        {
            builder.ToTable("point_transaction");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            builder.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
            builder.Property(x => x.BookingId).HasColumnName("booking_id");
            builder.Property(x => x.RewardId).HasColumnName("reward_id");
            builder.Property(x => x.Points).HasColumnName("points").IsRequired();
            builder.Property(x => x.TransactionType).HasColumnName("transaction_type").HasConversion(PointTransactionTypeConverter).IsRequired();
            builder.Property(x => x.Description).HasColumnName("description").HasColumnType("text");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => x.CustomerId);
            builder.HasIndex(x => x.TransactionType);

            builder.HasOne(x => x.Customer)
                .WithMany(x => x.PointTransactions)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Booking)
                .WithMany(x => x.PointTransactions)
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Reward)
                .WithMany(x => x.PointTransactions)
                .HasForeignKey(x => x.RewardId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Notification>(builder =>
        {
            builder.ToTable("notification");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            builder.Property(x => x.Type).HasColumnName("type").HasConversion(NotificationTypeConverter).IsRequired();
            builder.Property(x => x.Title).HasColumnName("title").IsRequired();
            builder.Property(x => x.Content).HasColumnName("content").HasColumnType("text").IsRequired();
            builder.Property(x => x.IsRead).HasColumnName("is_read").HasDefaultValue(false).IsRequired();
            builder.Property(x => x.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.IsRead);
            builder.HasIndex(x => x.CreatedAt);

            builder.HasOne(x => x.User)
                .WithMany(x => x.Notifications)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
