using Tevscare.Domain.Catalog;
using Tevscare.Domain.Common;
using Tevscare.Domain.Enums;

namespace Tevscare.Domain.Planning;

public class DietPlan : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public bool IsPublished { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string Disclaimer { get; set; } = string.Empty;
    public ICollection<DietPlanDay> Days { get; set; } = new List<DietPlanDay>();
    public ICollection<ProviderGuidance> Guidance { get; set; } = new List<ProviderGuidance>();
}

public class DietPlanDay : AuditableEntity
{
    public Guid DietPlanId { get; set; }
    public DietPlan DietPlan { get; set; } = null!;
    public int DayNumber { get; set; }
    public string? Notes { get; set; }
    public string? WaterNote { get; set; }
    public string? ActivityNote { get; set; }
    public string? SleepNote { get; set; }
    public ICollection<Meal> Meals { get; set; } = new List<Meal>();
}

public class Meal : AuditableEntity
{
    public Guid DietPlanDayId { get; set; }
    public DietPlanDay DietPlanDay { get; set; } = null!;
    public MealType MealType { get; set; }
    public string Title { get; set; } = string.Empty;
    public TimeOnly ScheduledTime { get; set; }
    public string? Notes { get; set; }
    public ICollection<MealItem> Items { get; set; } = new List<MealItem>();
}

public class MealItem : AuditableEntity
{
    public Guid MealId { get; set; }
    public Meal Meal { get; set; } = null!;
    public Guid? FoodId { get; set; }
    public Food? Food { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public string Unit { get; set; } = "serving";
    public int SortOrder { get; set; }
    public bool IsOptional { get; set; }
    public string? AlternativeGroup { get; set; }
    public bool IsDefaultAlternative { get; set; }
    public string? Note { get; set; }
}

public class UserPlanAssignment : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid DietPlanId { get; set; }
    public DietPlan DietPlan { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public PlanAssignmentStatus Status { get; set; } = PlanAssignmentStatus.Active;
}

public class ProviderGuidance : AuditableEntity
{
    public Guid? DietPlanId { get; set; }
    public DietPlan? DietPlan { get; set; }
    public GuidanceCategory Category { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Label { get; set; } = "Plan guidance";
    public string? ConditionKey { get; set; }
    public int SortOrder { get; set; }
}

public class HabitDefinition : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
