using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Tevscare.Domain.Account;
using Tevscare.Domain.Catalog;
using Tevscare.Domain.Planning;
using Tevscare.Domain.Tracking;
using Tevscare.Infrastructure.Identity;

namespace Tevscare.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<UserProfile> Profiles => Set<UserProfile>();
    public DbSet<UserAllergy> Allergies => Set<UserAllergy>();
    public DbSet<UserFoodPreference> FoodPreferences => Set<UserFoodPreference>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<UserNotificationSettings> NotificationSettings => Set<UserNotificationSettings>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<BudgetSettings> Budgets => Set<BudgetSettings>();
    public DbSet<ShoppingItemState> ShoppingStates => Set<ShoppingItemState>();
    public DbSet<Entitlement> Entitlements => Set<Entitlement>();
    public DbSet<ProductEvent> ProductEvents => Set<ProductEvent>();
    public DbSet<FoodCategory> FoodCategories => Set<FoodCategory>();
    public DbSet<Food> Foods => Set<Food>();
    public DbSet<UserFoodPrice> FoodPrices => Set<UserFoodPrice>();
    public DbSet<DietPlan> DietPlans => Set<DietPlan>();
    public DbSet<DietPlanDay> DietPlanDays => Set<DietPlanDay>();
    public DbSet<Meal> Meals => Set<Meal>();
    public DbSet<MealItem> MealItems => Set<MealItem>();
    public DbSet<UserPlanAssignment> PlanAssignments => Set<UserPlanAssignment>();
    public DbSet<ProviderGuidance> Guidance => Set<ProviderGuidance>();
    public DbSet<HabitDefinition> HabitDefinitions => Set<HabitDefinition>();
    public DbSet<MealLog> MealLogs => Set<MealLog>();
    public DbSet<MealLogItem> MealLogItems => Set<MealLogItem>();
    public DbSet<WaterEntry> WaterEntries => Set<WaterEntry>();
    public DbSet<WeightEntry> WeightEntries => Set<WeightEntry>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<SleepLog> SleepLogs => Set<SleepLog>();
    public DbSet<DailyCheckIn> CheckIns => Set<DailyCheckIn>();
    public DbSet<HabitCheck> HabitChecks => Set<HabitCheck>();
    public DbSet<AdherenceSnapshot> AdherenceSnapshots => Set<AdherenceSnapshot>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.FullName).HasMaxLength(120);
            entity.Property(x => x.Timezone).HasMaxLength(80);
        });

        builder.Entity<UserProfile>(entity =>
        {
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.Property(x => x.DietaryPreference).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.ActivityLevel).HasConversion<string>().HasMaxLength(40);
        });

        builder.Entity<UserAllergy>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(80);
            entity.HasIndex(x => new { x.UserId, x.Name }).IsUnique();
        });

        builder.Entity<UserFoodPreference>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(80);
            entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(x => new { x.UserId, x.Name }).IsUnique();
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.Property(x => x.TokenHash).HasMaxLength(128);
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => x.UserId);
        });

        builder.Entity<PasswordResetToken>(entity =>
        {
            entity.Property(x => x.TokenHash).HasMaxLength(128);
            entity.HasIndex(x => x.TokenHash);
        });

        builder.Entity<UserNotificationSettings>(entity => entity.HasIndex(x => x.UserId).IsUnique());

        builder.Entity<NotificationPreference>(entity =>
        {
            entity.Property(x => x.Category).HasConversion<string>().HasMaxLength(40);
            entity.HasOne(x => x.Settings).WithMany(x => x.Preferences).HasForeignKey(x => x.SettingsId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.SettingsId, x.Category }).IsUnique();
        });

        builder.Entity<BudgetSettings>(entity =>
        {
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.HasIndex(x => x.UserId).IsUnique();
        });

        builder.Entity<ShoppingItemState>(entity =>
        {
            entity.HasIndex(x => new { x.UserId, x.FoodId, x.RangeStart, x.RangeDays }).IsUnique();
        });

        builder.Entity<Entitlement>(entity =>
        {
            entity.Property(x => x.Plan).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.Source).HasMaxLength(40);
            entity.HasIndex(x => new { x.UserId, x.Status });
        });

        builder.Entity<ProductEvent>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(80);
            entity.Property(x => x.PropertiesJson).HasMaxLength(500);
            entity.HasIndex(x => new { x.Name, x.OccurredAtUtc });
        });

        builder.Entity<FoodCategory>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(80);
            entity.Property(x => x.Slug).HasMaxLength(80);
            entity.Property(x => x.GroceryCategory).HasConversion<string>().HasMaxLength(40);
            entity.HasIndex(x => x.Slug).IsUnique();
        });

        builder.Entity<Food>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(160);
            entity.Property(x => x.LocalName).HasMaxLength(160);
            entity.Property(x => x.ServingLabel).HasMaxLength(80);
            entity.Property(x => x.ServingUnit).HasMaxLength(40);
            entity.Property(x => x.Allergens).HasMaxLength(200);
            entity.Property(x => x.Tags).HasMaxLength(300);
            entity.Property(x => x.MicronutrientsNote).HasMaxLength(300);
            entity.Property(x => x.ProviderNote).HasMaxLength(500);
            entity.Property(x => x.SuitableFor).HasConversion<int>();
            entity.Property(x => x.GroceryCategory).HasConversion<string>().HasMaxLength(40);
            entity.HasIndex(x => x.Name);
            entity.HasOne(x => x.Category).WithMany(x => x.Foods).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<UserFoodPrice>(entity =>
        {
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.HasIndex(x => new { x.UserId, x.FoodId }).IsUnique();
            entity.HasOne(x => x.Food).WithMany().HasForeignKey(x => x.FoodId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DietPlan>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(160);
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.Disclaimer).HasMaxLength(1000);
            entity.HasIndex(x => x.Name);
        });

        builder.Entity<DietPlanDay>(entity =>
        {
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.Property(x => x.WaterNote).HasMaxLength(500);
            entity.Property(x => x.ActivityNote).HasMaxLength(500);
            entity.Property(x => x.SleepNote).HasMaxLength(500);
            entity.HasIndex(x => new { x.DietPlanId, x.DayNumber }).IsUnique();
            entity.HasOne(x => x.DietPlan).WithMany(x => x.Days).HasForeignKey(x => x.DietPlanId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Meal>(entity =>
        {
            entity.Property(x => x.MealType).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.Title).HasMaxLength(160);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.HasIndex(x => new { x.DietPlanDayId, x.MealType }).IsUnique();
            entity.HasOne(x => x.DietPlanDay).WithMany(x => x.Meals).HasForeignKey(x => x.DietPlanDayId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MealItem>(entity =>
        {
            entity.Property(x => x.DisplayName).HasMaxLength(160);
            entity.Property(x => x.Unit).HasMaxLength(40);
            entity.Property(x => x.AlternativeGroup).HasMaxLength(80);
            entity.Property(x => x.Note).HasMaxLength(300);
            entity.HasOne(x => x.Meal).WithMany(x => x.Items).HasForeignKey(x => x.MealId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Food).WithMany().HasForeignKey(x => x.FoodId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<UserPlanAssignment>(entity =>
        {
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
            entity.HasIndex(x => new { x.UserId, x.Status });
            entity.HasOne(x => x.DietPlan).WithMany().HasForeignKey(x => x.DietPlanId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ProviderGuidance>(entity =>
        {
            entity.Property(x => x.Category).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.Title).HasMaxLength(160);
            entity.Property(x => x.Body).HasMaxLength(2000);
            entity.Property(x => x.Label).HasMaxLength(80);
            entity.Property(x => x.ConditionKey).HasMaxLength(80);
            entity.HasOne(x => x.DietPlan).WithMany(x => x.Guidance).HasForeignKey(x => x.DietPlanId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<HabitDefinition>(entity =>
        {
            entity.Property(x => x.Code).HasMaxLength(80);
            entity.Property(x => x.Title).HasMaxLength(160);
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.HasIndex(x => x.Code).IsUnique();
        });

        builder.Entity<MealLog>(entity =>
        {
            entity.Property(x => x.MealType).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.HasIndex(x => new { x.UserId, x.LocalDate, x.MealType }).IsUnique();
        });

        builder.Entity<MealLogItem>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(160);
            entity.Property(x => x.Unit).HasMaxLength(40);
            entity.HasOne(x => x.MealLog).WithMany(x => x.Items).HasForeignKey(x => x.MealLogId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<WaterEntry>(entity => entity.HasIndex(x => new { x.UserId, x.LocalDate }));
        builder.Entity<WeightEntry>(entity => entity.HasIndex(x => new { x.UserId, x.LocalDate }).IsUnique());
        builder.Entity<ActivityLog>(entity =>
        {
            entity.Property(x => x.ActivityType).HasMaxLength(80);
            entity.Property(x => x.Notes).HasMaxLength(300);
            entity.HasIndex(x => new { x.UserId, x.LocalDate }).IsUnique();
        });
        builder.Entity<SleepLog>(entity =>
        {
            entity.Property(x => x.Notes).HasMaxLength(300);
            entity.HasIndex(x => new { x.UserId, x.LocalDate }).IsUnique();
        });
        builder.Entity<DailyCheckIn>(entity =>
        {
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.HasIndex(x => new { x.UserId, x.LocalDate }).IsUnique();
        });
        builder.Entity<HabitCheck>(entity =>
        {
            entity.Property(x => x.HabitCode).HasMaxLength(80);
            entity.HasIndex(x => new { x.UserId, x.HabitCode, x.LocalDate }).IsUnique();
        });
        builder.Entity<AdherenceSnapshot>(entity => entity.HasIndex(x => new { x.UserId, x.LocalDate }).IsUnique());

        foreach (var property in builder.Model.GetEntityTypes().SelectMany(t => t.GetProperties()))
        {
            if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
            {
                property.SetPrecision(12);
                property.SetScale(2);
            }
        }
    }
}
