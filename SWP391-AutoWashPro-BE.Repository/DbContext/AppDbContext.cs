using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SWP391_AutoWashPro_BE.Repository.Entities;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Repository;

public class AppDbContext : DbContext
{
    private static readonly ValueConverter<UserRole, string> UserRoleConverter =
        new(v => ToDbUserRole(v), v => FromDbUserRole(v));

    private static readonly ValueConverter<AccountStatus, string> AccountStatusConverter =
        new(v => ToDbAccountStatus(v), v => FromDbAccountStatus(v));

    private static readonly ValueConverter<BookingStatus, string> BookingStatusConverter =
        new(v => ToDbBookingStatus(v), v => FromDbBookingStatus(v));

    private static readonly ValueConverter<DiscountType, string> DiscountTypeConverter =
        new(v => ToDbDiscountType(v), v => FromDbDiscountType(v));

    private static readonly ValueConverter<RewardType, string> RewardTypeConverter =
        new(v => ToDbRewardType(v), v => FromDbRewardType(v));

    private static readonly ValueConverter<VoucherStatus, string> VoucherStatusConverter =
        new(v => ToDbVoucherStatus(v), v => FromDbVoucherStatus(v));

    private static readonly ValueConverter<NotificationType, string> NotificationTypeConverter =
        new(v => ToDbNotificationType(v), v => FromDbNotificationType(v));

    private static readonly ValueConverter<PersonalizedVoucherTriggerType, string> PersonalizedVoucherTriggerTypeConverter =
        new(v => ToDbPersonalizedVoucherTriggerType(v), v => FromDbPersonalizedVoucherTriggerType(v));

    private static readonly ValueConverter<PersonalizedVoucherDeliveryStatus, string> PersonalizedVoucherDeliveryStatusConverter =
        new(v => ToDbPersonalizedVoucherDeliveryStatus(v), v => FromDbPersonalizedVoucherDeliveryStatus(v));

    private static readonly ValueConverter<PointTransactionType, string> PointTransactionTypeConverter =
        new(v => ToDbPointTransactionType(v), v => FromDbPointTransactionType(v));

    private static readonly ValueConverter<TransactionType, string> TransactionTypeConverter =
        new(v => ToDbTransactionType(v), v => FromDbTransactionType(v));

    private static readonly ValueConverter<ChatIntent, string> ChatIntentConverter =
        new(v => ToDbChatIntent(v), v => FromDbChatIntent(v));

    private static readonly ValueConverter<ChatMessageRole, string> ChatMessageRoleConverter =
        new(v => ToDbChatMessageRole(v), v => FromDbChatMessageRole(v));


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
        AccountStatus.Pending => "pending",
        AccountStatus.Active => "active",
        AccountStatus.Rejected => "rejected",
        AccountStatus.Locked => "locked",
        AccountStatus.Inactive => "inactive",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static AccountStatus FromDbAccountStatus(string value) => value switch
    {
        "pending" => AccountStatus.Pending,
        "active" => AccountStatus.Active,
        "rejected" => AccountStatus.Rejected,
        "locked" => AccountStatus.Locked,
        "inactive" => AccountStatus.Inactive,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static string ToDbBookingStatus(BookingStatus value) => value switch
    {
        BookingStatus.Available => "available",
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
        "available" => BookingStatus.Available,
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
        NotificationType.IdentityApproved => "identity_approved",
        NotificationType.IdentityRejected => "identity_rejected",
        NotificationType.PersonalizedVoucher => "personalized_voucher",
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
        "identity_approved" => NotificationType.IdentityApproved,
        "identity_rejected" => NotificationType.IdentityRejected,
        "personalized_voucher" => NotificationType.PersonalizedVoucher,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static string ToDbPersonalizedVoucherTriggerType(PersonalizedVoucherTriggerType value) => value switch
    {
        PersonalizedVoucherTriggerType.Birthday => "birthday",
        PersonalizedVoucherTriggerType.InactiveCustomer => "inactive_customer",
        PersonalizedVoucherTriggerType.Welcome => "welcome",
        PersonalizedVoucherTriggerType.NoFirstBooking => "no_first_booking",
        PersonalizedVoucherTriggerType.TierUpgrade => "tier_upgrade",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static PersonalizedVoucherTriggerType FromDbPersonalizedVoucherTriggerType(string value) => value switch
    {
        "birthday" => PersonalizedVoucherTriggerType.Birthday,
        "inactive_customer" => PersonalizedVoucherTriggerType.InactiveCustomer,
        "welcome" => PersonalizedVoucherTriggerType.Welcome,
        "no_first_booking" => PersonalizedVoucherTriggerType.NoFirstBooking,
        "tier_upgrade" => PersonalizedVoucherTriggerType.TierUpgrade,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static string ToDbPersonalizedVoucherDeliveryStatus(PersonalizedVoucherDeliveryStatus value) => value switch
    {
        PersonalizedVoucherDeliveryStatus.NotRequired => "not_required",
        PersonalizedVoucherDeliveryStatus.Pending => "pending",
        PersonalizedVoucherDeliveryStatus.Sent => "sent",
        PersonalizedVoucherDeliveryStatus.Failed => "failed",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static PersonalizedVoucherDeliveryStatus FromDbPersonalizedVoucherDeliveryStatus(string value) => value switch
    {
        "not_required" => PersonalizedVoucherDeliveryStatus.NotRequired,
        "pending" => PersonalizedVoucherDeliveryStatus.Pending,
        "sent" => PersonalizedVoucherDeliveryStatus.Sent,
        "failed" => PersonalizedVoucherDeliveryStatus.Failed,
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

    private static string ToDbTransactionType(TransactionType value) => value switch
    {
        TransactionType.Deposit => "deposit",
        TransactionType.FullPayment => "full_payment",
        TransactionType.WalletTopup => "wallet_topup",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static TransactionType FromDbTransactionType(string value) => value switch
    {
        "deposit" => TransactionType.Deposit,
        "full_payment" => TransactionType.FullPayment,
        "wallet_topup" => TransactionType.WalletTopup,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static string ToDbChatIntent(ChatIntent value) => value switch
    {
        ChatIntent.UserProfile => "user_profile",
        ChatIntent.Loyalty => "loyalty",
        ChatIntent.Booking => "booking",
        ChatIntent.BookingDetail => "booking_detail",
        ChatIntent.Voucher => "voucher",
        ChatIntent.Promotion => "promotion",
        ChatIntent.Branch => "branch",
        ChatIntent.NearestBranch => "nearest_branch",
        ChatIntent.TopBranch => "top_branch",
        ChatIntent.Faq => "faq",
        ChatIntent.Unknown => "unknown",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static ChatIntent FromDbChatIntent(string value) => value switch
    {
        "user_profile" => ChatIntent.UserProfile,
        "loyalty" => ChatIntent.Loyalty,
        "booking" => ChatIntent.Booking,
        "booking_detail" => ChatIntent.BookingDetail,
        "voucher" => ChatIntent.Voucher,
        "promotion" => ChatIntent.Promotion,
        "branch" => ChatIntent.Branch,
        "nearest_branch" => ChatIntent.NearestBranch,
        "top_branch" => ChatIntent.TopBranch,
        "faq" => ChatIntent.Faq,
        "unknown" => ChatIntent.Unknown,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static string ToDbChatMessageRole(ChatMessageRole value) => value switch
    {
        ChatMessageRole.User => "user",
        ChatMessageRole.Assistant => "assistant",
        ChatMessageRole.System => "system",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static ChatMessageRole FromDbChatMessageRole(string value) => value switch
    {
        "user" => ChatMessageRole.User,
        "assistant" => ChatMessageRole.Assistant,
        "system" => ChatMessageRole.System,
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
    public DbSet<VehicleImage> VehicleImages { get; set; }
    public DbSet<VehicleType> VehicleTypes { get; set; }
    public DbSet<Tier> Tiers { get; set; }
    public DbSet<Branch> Branches { get; set; }
    public DbSet<SystemConfig> SystemConfigs { get; set; }
    public DbSet<Promotion> Promotions { get; set; }
    public DbSet<PromotionTier> PromotionTiers { get; set; }
    public DbSet<Reward> Rewards { get; set; }
    public DbSet<RewardTier> RewardTiers { get; set; }
    public DbSet<Voucher> Vouchers { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<PointTransaction> PointTransactions { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<PersonalizedPromotionRule> PersonalizedPromotionRules { get; set; }
    public DbSet<PersonalizedVoucherIssuance> PersonalizedVoucherIssuances { get; set; }
    public DbSet<CustomerDateOfBirthCorrection> CustomerDateOfBirthCorrections { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }

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
            builder.Property(x => x.Role).HasColumnName("role").HasConversion(UserRoleConverter)
                .HasSentinel(UserRole.Customer)
                .HasDefaultValue(UserRole.Customer).IsRequired();
            builder.Property(x => x.Status).HasColumnName("status").HasConversion(AccountStatusConverter)
                .HasDefaultValue(AccountStatus.Pending).IsRequired();
            builder.Property(x => x.isVerify).HasColumnName("is_verify").HasDefaultValue(false).IsRequired();
            builder.Property(x => x.LastLoginAt).HasColumnName("last_login_at");
            builder.Property(x => x.VerifiedAt).HasColumnName("verified_at");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            builder.Property(x => x.Reason).HasColumnName("reason");

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
            builder.Property(x => x.Balance).HasColumnName("balance").HasColumnType("numeric(12,2)").HasDefaultValue(0m)
                .IsRequired();
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
            builder.Property(x => x.DateOfBirth).HasColumnName("date_of_birth").HasColumnType("date");
            builder.Property(x => x.DateOfBirthSetAt).HasColumnName("date_of_birth_set_at");
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

        modelBuilder.Entity<CustomerDateOfBirthCorrection>(builder =>
        {
            builder.ToTable("customer_date_of_birth_correction");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            builder.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
            builder.Property(x => x.AdminUserId).HasColumnName("admin_user_id").IsRequired();
            builder.Property(x => x.PreviousDateOfBirth).HasColumnName("previous_date_of_birth").HasColumnType("date");
            builder.Property(x => x.NewDateOfBirth).HasColumnName("new_date_of_birth").HasColumnType("date").IsRequired();
            builder.Property(x => x.Reason).HasColumnName("reason").HasColumnType("text").IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => x.CustomerId);
            builder.HasIndex(x => x.AdminUserId);
            builder.HasIndex(x => x.CreatedAt);

            builder.HasOne(x => x.Customer)
                .WithMany(x => x.DateOfBirthCorrections)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.AdminUser)
                .WithMany(x => x.DateOfBirthCorrections)
                .HasForeignKey(x => x.AdminUserId)
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
            builder.Property(x => x.VehicleTypeId).HasColumnName("vehicle_type_id").IsRequired();

            builder.HasIndex(x => x.CustomerId);
            builder.HasIndex(x => x.LicensePlate).IsUnique();
            builder.HasIndex(x => x.IsActive);
            builder.HasIndex(x => x.VehicleTypeId);

            builder.HasOne(x => x.Customer)
                .WithMany(x => x.Vehicles)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.VehicleType)
                .WithMany(x => x.Vehicles)
                .HasForeignKey(x => x.VehicleTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VehicleImage>(builder =>
        {
            builder.ToTable("vehicle_image");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            builder.Property(x => x.VehicleId).HasColumnName("vehicle_id").IsRequired();
            builder.Property(x => x.ImageUrl).HasColumnName("image_url").IsRequired();
            builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => x.VehicleId);

            builder.HasOne(x => x.Vehicle)
                .WithMany(x => x.VehicleImages)
                .HasForeignKey(x => x.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VehicleType>(builder =>
        {
            builder.ToTable("vehicle_type");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            builder.Property(x => x.TypeName).HasColumnName("type_name").HasConversion<string>().IsRequired();
            builder.Property(x => x.VehicleSlot).HasColumnName("vehicle_slot").IsRequired();
            builder.Property(x => x.SizeLevel).HasColumnName("size_level").IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => x.TypeName).IsUnique();
            builder.HasIndex(x => x.SizeLevel);

            builder.HasData(
                new VehicleType
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    TypeName = SWP391_AutoWashPro_BE.Repository.Enums.VehicleTypes.SUV,
                    VehicleSlot = 12,
                    SizeLevel = 2,
                    CreatedAt = new DateTimeOffset(2026, 6, 9, 21, 0, 0, TimeSpan.FromHours(7))
                },
                new VehicleType
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    TypeName = SWP391_AutoWashPro_BE.Repository.Enums.VehicleTypes.Sedan,
                    VehicleSlot = 5,
                    SizeLevel = 1,
                    CreatedAt = new DateTimeOffset(2026, 6, 9, 21, 0, 0, TimeSpan.FromHours(7))
                }
            );
        });

        modelBuilder.Entity<Tier>(builder =>
        {
            builder.ToTable("tier");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            builder.Property(x => x.Name).HasColumnName("name").IsRequired();
            builder.Property(x => x.Level).HasColumnName("level").IsRequired();
            builder.Property(x => x.RequiredWashes).HasColumnName("required_washes").HasDefaultValue(0).IsRequired();
            builder.Property(x => x.PriorityBookingDays).HasColumnName("priority_booking_days").HasDefaultValue(0)
                .IsRequired();
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
            builder.Property(x => x.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => x.IsActive);
            builder.HasIndex(x => x.IsDeleted);
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

            var defaultConfigCreatedAt =
                new DateTimeOffset(2026, 5, 28, 20, 13, 39, TimeSpan.FromHours(7)).AddMilliseconds(590);
            List<SystemConfig> systemConfigs = new List<SystemConfig>()
            {
                // Default business configs (UTC+7)
                //WorkingStartHour = 8 am
                new()
                {
                    Id = Guid.Parse("6e830ac7-1934-4392-b05a-b4f777302170"),
                    ConfigKey = "WorkingStartHour",
                    ConfigValue = "8",
                    Description = "Default working start time in Vietnam timezone UTC+7.",
                    CreatedAt = defaultConfigCreatedAt,
                },

                //WorkingEndHour = 17 pm
                new()
                {
                    Id = Guid.Parse("8d456f5d-26ba-45f1-a57f-d88234758685"),
                    ConfigKey = "WorkingEndHour",
                    ConfigValue = "17",
                    Description = "Default working end time in Vietnam timezone UTC+7.",
                    CreatedAt = defaultConfigCreatedAt,
                },

                //SlotDurationMinutes = 15
                new()
                {
                    Id = Guid.Parse("490a0d6b-e4ca-4315-a387-b92b6f52c9bc"),
                    ConfigKey = "SlotDurationMinutes",
                    ConfigValue = "15",
                    Description = "Duration of each booking slot in minutes.",
                    CreatedAt = defaultConfigCreatedAt,
                },

                //SlotBreakMinutes = 0
                new()
                {
                    Id = Guid.Parse("7f3b0ad6-9b0b-4c3d-b8d1-5dc1d17a6c4e"),
                    ConfigKey = "SlotBreakMinutes",
                    ConfigValue = "0",
                    Description = "Break time in minutes between consecutive booking slots.",
                    CreatedAt = defaultConfigCreatedAt,
                },

                //BasePrice = 100000
                new()
                {
                    Id = Guid.Parse("f96ce391-eb3a-4a8e-ad76-18c3f8da6668"),
                    ConfigKey = "BasePrice",
                    ConfigValue = "100000",
                    Description = "Default base price for service.",
                    CreatedAt = defaultConfigCreatedAt,
                }, //SuvBasePrice = 30000
                new()
                {
                    Id = Guid.Parse("8b2e6d2b-0c74-47a0-9c5f-9d83029de001"),
                    ConfigKey = "SuvBasePrice",
                    ConfigValue = "30000",
                    Description = "Additional base price for SUV vehicles.",
                    CreatedAt = defaultConfigCreatedAt,
                },

                //SedanBasePrice = 0
                new()
                {
                    Id = Guid.Parse("f1a24c4e-1978-4db9-8d6a-2cb7a3f7f002"),
                    ConfigKey = "SedanBasePrice",
                    ConfigValue = "0",
                    Description = "Additional base price for Sedan vehicles.",
                    CreatedAt = defaultConfigCreatedAt,
                },

                //PaymentDeposite = 30
                new()
                {
                    Id = Guid.Parse("219a17c5-c218-4c0c-b0e0-6e95fd0c6b11"),
                    ConfigKey = "PaymentDeposite",
                    ConfigValue = "30",
                    Description = "Deposit percentage required for booking.",
                    CreatedAt = defaultConfigCreatedAt,
                },

                //BonusPoint = 10
                new()
                {
                    Id = Guid.Parse("09f7cba0-c348-4654-90d6-bdd3b21385fa"),
                    ConfigKey = "BonusPoint",
                    ConfigValue = "10",
                    Description = "Bonus points earned after checkout completed.",
                    CreatedAt = defaultConfigCreatedAt,
                },
            };

            builder.HasData(systemConfigs);
        });

        modelBuilder.Entity<Promotion>(builder =>
        {
            builder.ToTable("promotion");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            builder.Property(x => x.Name).HasColumnName("name").IsRequired();
            builder.Property(x => x.Description).HasColumnName("description").HasColumnType("text");
            builder.Property(x => x.DiscountType).HasColumnName("discount_type").HasConversion(DiscountTypeConverter)
                .IsRequired();
            builder.Property(x => x.DiscountValue).HasColumnName("discount_value").HasColumnType("numeric(12,2)")
                .IsRequired();
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

        modelBuilder.Entity<PersonalizedPromotionRule>(builder =>
        {
            builder.ToTable("personalized_promotion_rule");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            builder.Property(x => x.PromotionId).HasColumnName("promotion_id").IsRequired();
            builder.Property(x => x.TriggerType).HasColumnName("trigger_type")
                .HasConversion(PersonalizedVoucherTriggerTypeConverter).IsRequired();
            builder.Property(x => x.ThresholdDays).HasColumnName("threshold_days");
            builder.Property(x => x.VoucherValidityDays).HasColumnName("voucher_validity_days").IsRequired();
            builder.Property(x => x.Priority).HasColumnName("priority").HasDefaultValue(0).IsRequired();
            builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
            builder.Property(x => x.SendInAppNotification).HasColumnName("send_in_app_notification")
                .HasDefaultValue(false).IsRequired();
            builder.Property(x => x.SendEmail).HasColumnName("send_email").HasDefaultValue(false).IsRequired();
            builder.Property(x => x.NotificationTitleTemplate).HasColumnName("notification_title_template")
                .HasColumnType("text");
            builder.Property(x => x.NotificationContentTemplate).HasColumnName("notification_content_template")
                .HasColumnType("text");
            builder.Property(x => x.EmailSubjectTemplate).HasColumnName("email_subject_template")
                .HasColumnType("text");
            builder.Property(x => x.EmailBodyTemplate).HasColumnName("email_body_template").HasColumnType("text");
            builder.Property(x => x.CallToActionUrl).HasColumnName("call_to_action_url").HasMaxLength(500);
            builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => x.PromotionId);
            builder.HasIndex(x => x.TriggerType);
            builder.HasIndex(x => x.ThresholdDays);
            builder.HasIndex(x => new { x.IsActive, x.TriggerType });

            builder.HasOne(x => x.Promotion)
                .WithMany(x => x.PersonalizedPromotionRules)
                .HasForeignKey(x => x.PromotionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Reward>(builder =>
        {
            builder.ToTable("reward");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            builder.Property(x => x.Name).HasColumnName("name").IsRequired();
            builder.Property(x => x.RewardType).HasColumnName("reward_type").HasConversion(RewardTypeConverter)
                .IsRequired();
            builder.Property(x => x.PointsRequired).HasColumnName("points_required").IsRequired();
            builder.Property(x => x.QuantityAvailable).HasColumnName("quantity_available").HasDefaultValue(-1)
                .IsRequired();
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
            builder.Property(x => x.Status).HasColumnName("status").HasConversion(VoucherStatusConverter)
                .HasDefaultValue(VoucherStatus.Active).IsRequired();
            builder.Property(x => x.DiscountType).HasColumnName("discount_type").HasConversion(DiscountTypeConverter)
                .IsRequired();
            builder.Property(x => x.DiscountValue).HasColumnName("discount_value").HasColumnType("numeric(12,2)")
                .IsRequired();
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

        modelBuilder.Entity<PersonalizedVoucherIssuance>(builder =>
        {
            builder.ToTable("personalized_voucher_issuance");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            builder.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
            builder.Property(x => x.PromotionId).HasColumnName("promotion_id").IsRequired();
            builder.Property(x => x.PromotionRuleId).HasColumnName("promotion_rule_id").IsRequired();
            builder.Property(x => x.VoucherId).HasColumnName("voucher_id").IsRequired();
            builder.Property(x => x.TriggerType).HasColumnName("trigger_type")
                .HasConversion(PersonalizedVoucherTriggerTypeConverter).IsRequired();
            builder.Property(x => x.CycleKey).HasColumnName("cycle_key").HasMaxLength(200).IsRequired();
            builder.Property(x => x.TriggerReference).HasColumnName("trigger_reference").HasMaxLength(200);
            builder.Property(x => x.NotificationId).HasColumnName("notification_id");
            builder.Property(x => x.NotificationStatus).HasColumnName("notification_status")
                .HasConversion(PersonalizedVoucherDeliveryStatusConverter).IsRequired();
            builder.Property(x => x.NotificationAttemptCount).HasColumnName("notification_attempt_count")
                .HasDefaultValue(0).IsRequired();
            builder.Property(x => x.NotificationLastAttemptAt).HasColumnName("notification_last_attempt_at");
            builder.Property(x => x.NotificationSentAt).HasColumnName("notification_sent_at");
            builder.Property(x => x.NotificationLastError).HasColumnName("notification_last_error").HasMaxLength(200);
            builder.Property(x => x.EmailStatus).HasColumnName("email_status")
                .HasConversion(PersonalizedVoucherDeliveryStatusConverter).IsRequired();
            builder.Property(x => x.EmailAttemptCount).HasColumnName("email_attempt_count")
                .HasDefaultValue(0).IsRequired();
            builder.Property(x => x.EmailLastAttemptAt).HasColumnName("email_last_attempt_at");
            builder.Property(x => x.EmailSentAt).HasColumnName("email_sent_at");
            builder.Property(x => x.EmailLastError).HasColumnName("email_last_error").HasMaxLength(200);
            builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => x.CustomerId);
            builder.HasIndex(x => x.PromotionId);
            builder.HasIndex(x => x.PromotionRuleId);
            builder.HasIndex(x => x.VoucherId).IsUnique();
            builder.HasIndex(x => x.NotificationId).IsUnique().HasFilter("notification_id IS NOT NULL");
            builder.HasIndex(x => new { x.CustomerId, x.TriggerType, x.CycleKey })
                .IsUnique()
                .HasDatabaseName("UX_personalized_voucher_issuance_customer_trigger_cycle");
            builder.HasIndex(x => new { x.NotificationStatus, x.NotificationAttemptCount });
            builder.HasIndex(x => new { x.EmailStatus, x.EmailAttemptCount });

            builder.HasOne(x => x.Customer)
                .WithMany(x => x.PersonalizedVoucherIssuances)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Promotion)
                .WithMany(x => x.PersonalizedVoucherIssuances)
                .HasForeignKey(x => x.PromotionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.PromotionRule)
                .WithMany(x => x.Issuances)
                .HasForeignKey(x => x.PromotionRuleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Voucher)
                .WithOne(x => x.PersonalizedVoucherIssuance)
                .HasForeignKey<PersonalizedVoucherIssuance>(x => x.VoucherId)
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
            builder.Property(x => x.Status).HasColumnName("status").HasConversion(BookingStatusConverter)
                .HasSentinel(BookingStatus.Pending)
                .HasDefaultValue(BookingStatus.Pending).IsRequired();
            builder.Property(x => x.BasePrice).HasColumnName("base_price").HasColumnType("numeric(12,2)").IsRequired();
            builder.Property(x => x.DiscountAmount).HasColumnName("discount_amount").HasColumnType("numeric(12,2)")
                .HasDefaultValue(0m).IsRequired();
            builder.Property(x => x.FinalPrice).HasColumnName("final_price").HasColumnType("numeric(12,2)")
                .IsRequired();
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
            builder.HasIndex(x => new { x.BranchId, x.BookingDate, x.StartTime });

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

        modelBuilder.Entity<Transaction>(builder =>
        {
            builder.ToTable("transaction");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            builder.Property(x => x.Amount).HasColumnName("amount").HasColumnType("numeric(12,2)").IsRequired();
            builder.Property(x => x.Type).HasColumnName("type").HasConversion(TransactionTypeConverter).IsRequired();
            builder.Property(x => x.Description).HasColumnName("description").HasColumnType("text");
            builder.Property(x => x.TransactionDate).HasColumnName("transaction_date").IsRequired();
            builder.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
            builder.Property(x => x.BookingId).HasColumnName("booking_id");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => x.CustomerId);
            builder.HasIndex(x => x.BookingId);
            builder.HasIndex(x => x.Type);
            builder.HasIndex(x => x.TransactionDate);

            builder.HasOne(x => x.CustomerProfile)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Booking)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.BookingId)
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
            builder.Property(x => x.TransactionType).HasColumnName("transaction_type")
                .HasConversion(PointTransactionTypeConverter).IsRequired();
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

        modelBuilder.Entity<Conversation>(builder =>
        {
            builder.ToTable("conversation");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(120).IsRequired();
            builder.Property(x => x.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.UpdatedAt);
            builder.HasIndex(x => x.IsDeleted);

            builder.HasOne(x => x.User)
                .WithMany(x => x.Conversations)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ChatMessage>(builder =>
        {
            builder.ToTable("chat_message");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            builder.Property(x => x.ConversationId).HasColumnName("conversation_id").IsRequired();
            builder.Property(x => x.Role).HasColumnName("role").HasConversion(ChatMessageRoleConverter).IsRequired();
            builder.Property(x => x.Content).HasColumnName("content").HasColumnType("text").IsRequired();
            builder.Property(x => x.Intent).HasColumnName("intent").HasConversion(ChatIntentConverter);
            builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => x.ConversationId);
            builder.HasIndex(x => x.CreatedAt);
            builder.HasIndex(x => x.Intent);

            builder.HasOne(x => x.Conversation)
                .WithMany(x => x.ChatMessages)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

    }
}
