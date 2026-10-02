using Tevscare.Domain.Common;
using Tevscare.Domain.Enums;

namespace Tevscare.Domain.Account;

public class UserProfile : AuditableEntity
{
    public Guid UserId { get; set; }
    public int? Age { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? CurrentWeightKg { get; set; }
    public decimal? TargetWeightKg { get; set; }
    public decimal? StartingWeightKg { get; set; }
    public DietaryPreference DietaryPreference { get; set; } = DietaryPreference.Eggetarian;
    public ActivityLevel ActivityLevel { get; set; } = ActivityLevel.Light;
    public int WaterGoalMl { get; set; } = 2500;
    public int SleepGoalMinutes { get; set; } = 450;
    public int ActivityGoalMinutes { get; set; } = 30;
    public TimeOnly WakeTime { get; set; } = new(6, 30);
    public TimeOnly BreakfastTime { get; set; } = new(8, 0);
    public TimeOnly LunchTime { get; set; } = new(13, 0);
    public TimeOnly DinnerTime { get; set; } = new(19, 30);
    public TimeOnly SleepTime { get; set; } = new(22, 0);
    public bool OnboardingCompleted { get; set; }
    public string PreferredLanguage { get; set; } = "en";
    public ICollection<UserAllergy> Allergies { get; set; } = new List<UserAllergy>();
    public ICollection<UserFoodPreference> FoodPreferences { get; set; } = new List<UserFoodPreference>();
}

public class UserAllergy : AuditableEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class UserFoodPreference : AuditableEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public FoodPreferenceKind Kind { get; set; }
}

public class RefreshToken : AuditableEntity
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByTokenHash { get; set; }
}

public class PasswordResetToken : AuditableEntity
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
}

public class UserNotificationSettings : AuditableEntity
{
    public Guid UserId { get; set; }
    public bool QuietHoursEnabled { get; set; } = true;
    public TimeOnly QuietStart { get; set; } = new(22, 0);
    public TimeOnly QuietEnd { get; set; } = new(7, 0);
    public ICollection<NotificationPreference> Preferences { get; set; } = new List<NotificationPreference>();
}

public class NotificationPreference : AuditableEntity
{
    public Guid SettingsId { get; set; }
    public UserNotificationSettings Settings { get; set; } = null!;
    public NotificationCategory Category { get; set; }
    public bool Enabled { get; set; } = true;
    public TimeOnly? LocalTime { get; set; }
    public int? IntervalMinutes { get; set; }
    public TimeOnly? WindowStart { get; set; }
    public TimeOnly? WindowEnd { get; set; }
}

public class BudgetSettings : AuditableEntity
{
    public Guid UserId { get; set; }
    public decimal DailyBudgetAmount { get; set; } = 675m;
    public string Currency { get; set; } = "INR";
}

public class ShoppingItemState : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid FoodId { get; set; }
    public DateOnly RangeStart { get; set; }
    public int RangeDays { get; set; }
    public bool Purchased { get; set; }
    public decimal? QuantityOverride { get; set; }
    public decimal? ActualUnitPrice { get; set; }
    public string? Notes { get; set; }
}

public class Entitlement : AuditableEntity
{
    public Guid UserId { get; set; }
    public EntitlementPlan Plan { get; set; } = EntitlementPlan.Free;
    public EntitlementStatus Status { get; set; } = EntitlementStatus.Active;
    public DateTime StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public string Source { get; set; } = "system";
}

public class ProductEvent : AuditableEntity
{
    public Guid? UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public string? PropertiesJson { get; set; }
}
