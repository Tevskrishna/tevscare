using Tevscare.Domain.Common;
using Tevscare.Domain.Enums;

namespace Tevscare.Domain.Tracking;

public class MealLog : AuditableEntity
{
    public Guid UserId { get; set; }
    public DateOnly LocalDate { get; set; }
    public MealType MealType { get; set; }
    public Guid? PlannedMealId { get; set; }
    public MealLogStatus Status { get; set; }
    public string? Notes { get; set; }
    public ICollection<MealLogItem> Items { get; set; } = new List<MealLogItem>();
}

public class MealLogItem : AuditableEntity
{
    public Guid MealLogId { get; set; }
    public MealLog MealLog { get; set; } = null!;
    public Guid? FoodId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "serving";
}

public class WaterEntry : AuditableEntity
{
    public Guid UserId { get; set; }
    public DateOnly LocalDate { get; set; }
    public DateTime LoggedAtUtc { get; set; }
    public int AmountMl { get; set; }
}

public class WeightEntry : AuditableEntity
{
    public Guid UserId { get; set; }
    public DateOnly LocalDate { get; set; }
    public DateTime LoggedAtUtc { get; set; }
    public decimal WeightKg { get; set; }
    public bool IsMorning { get; set; } = true;
    public string? Note { get; set; }
}

public class ActivityLog : AuditableEntity
{
    public Guid UserId { get; set; }
    public DateOnly LocalDate { get; set; }
    public string ActivityType { get; set; } = "Walk";
    public int DurationMinutes { get; set; }
    public bool Completed { get; set; }
    public string? Notes { get; set; }
}

public class SleepLog : AuditableEntity
{
    public Guid UserId { get; set; }
    public DateOnly LocalDate { get; set; }
    public int DurationMinutes { get; set; }
    public int? Quality { get; set; }
    public string? Notes { get; set; }
}

public class DailyCheckIn : AuditableEntity
{
    public Guid UserId { get; set; }
    public DateOnly LocalDate { get; set; }
    public int? Mood { get; set; }
    public int? Energy { get; set; }
    public string? Notes { get; set; }
}

public class HabitCheck : AuditableEntity
{
    public Guid UserId { get; set; }
    public string HabitCode { get; set; } = string.Empty;
    public DateOnly LocalDate { get; set; }
    public bool Completed { get; set; }
}

public class AdherenceSnapshot : AuditableEntity
{
    public Guid UserId { get; set; }
    public DateOnly LocalDate { get; set; }
    public int Score { get; set; }
    public int MealsPlanned { get; set; }
    public decimal MealsCompletedWeight { get; set; }
    public bool WaterGoalMet { get; set; }
    public bool ActivityLogged { get; set; }
    public bool SleepLogged { get; set; }
    public bool WeightLogged { get; set; }
}
