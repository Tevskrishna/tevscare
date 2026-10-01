namespace Tevscare.Application.Contracts;

public record UserSummary(
    Guid Id,
    string FullName,
    string Email,
    string Timezone,
    IReadOnlyList<string> Roles,
    bool OnboardingCompleted,
    string EntitlementPlan);

public record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc,
    UserSummary User,
    string? DevelopmentResetToken = null);

public record RegisterRequest(string FullName, string Email, string Password, string? Timezone);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record LogoutRequest(string RefreshToken);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Token, string NewPassword);
public record MessageResponse(string Message, string? DevelopmentResetToken = null);

public record FoodPreferenceDto(string Name, string Kind);

public record ProfileResponse(
    Guid UserId,
    string FullName,
    string Email,
    string Timezone,
    int? Age,
    decimal? HeightCm,
    decimal? CurrentWeightKg,
    decimal? TargetWeightKg,
    decimal? StartingWeightKg,
    string DietaryPreference,
    string ActivityLevel,
    int WaterGoalMl,
    int SuggestedWaterGoalMl,
    bool WaterGoalOutsideSuggestedRange,
    int SleepGoalMinutes,
    int ActivityGoalMinutes,
    string WakeTime,
    string BreakfastTime,
    string LunchTime,
    string DinnerTime,
    string SleepTime,
    bool OnboardingCompleted,
    IReadOnlyList<string> Allergies,
    IReadOnlyList<FoodPreferenceDto> FoodPreferences,
    string EntitlementPlan,
    string Disclaimer);

public record UpdateProfileRequest(
    string FullName,
    int Age,
    decimal HeightCm,
    decimal CurrentWeightKg,
    decimal TargetWeightKg,
    string DietaryPreference,
    string ActivityLevel,
    int WaterGoalMl,
    int SleepGoalMinutes,
    int ActivityGoalMinutes,
    string Timezone,
    IReadOnlyList<string>? Allergies,
    IReadOnlyList<FoodPreferenceDto>? FoodPreferences,
    string? WakeTime,
    string? BreakfastTime,
    string? LunchTime,
    string? DinnerTime,
    string? SleepTime);

public record MealItemDto(
    Guid Id,
    Guid? FoodId,
    string Name,
    string? LocalName,
    decimal Quantity,
    string Unit,
    bool IsOptional,
    string? AlternativeGroup,
    bool IsDefaultAlternative,
    string? Note,
    decimal? EstimatedCost,
    decimal? Calories,
    decimal? ProteinG);

public record MealCardDto(
    Guid Id,
    string MealType,
    string Title,
    string ScheduledTime,
    string? Notes,
    decimal EstimatedCost,
    bool HasMissingPrices,
    string? LogStatus,
    IReadOnlyList<MealItemDto> Items);

public record ReminderDto(string Category, string Title, string Body, string Time, bool Enabled);

public record WeightSummaryDto(decimal? CurrentKg, decimal? TargetKg, decimal? ChangeKg, int? ProgressPercent);
public record WaterSummaryDto(int GoalMl, int ConsumedMl, int RemainingMl, int Percent);
public record ActivitySummaryDto(int GoalMinutes, int LoggedMinutes, bool Completed);
public record SleepSummaryDto(int GoalMinutes, int? LoggedMinutes, bool Logged);
public record AdherenceSummaryDto(int Score, string Explanation);

public record DashboardResponse(
    string Greeting,
    string LocalDate,
    string DisplayDate,
    string Timezone,
    string? PlanName,
    Guid? PlanId,
    int? PlanDayNumber,
    int PlanDurationDays,
    bool PlanNotStarted,
    bool PlanCompleted,
    WeightSummaryDto Weight,
    WaterSummaryDto Water,
    ActivitySummaryDto Activity,
    SleepSummaryDto Sleep,
    AdherenceSummaryDto Adherence,
    MealCardDto? NextMeal,
    IReadOnlyList<MealCardDto> Meals,
    IReadOnlyList<ReminderDto> Reminders,
    string Disclaimer);

public record DietPlanSummaryDto(Guid Id, string Name, string Description, int DurationDays, bool IsPublished, bool IsAssigned);
public record DietPlanDaySummaryDto(int DayNumber, string? Notes, int MealCount, bool IsToday, string? AdherenceHint);

public record DietPlanDetailDto(
    Guid Id,
    string Name,
    string Description,
    int DurationDays,
    bool IsPublished,
    DateOnly? StartDate,
    int? CurrentDayNumber,
    bool PlanCompleted,
    string Disclaimer,
    IReadOnlyList<DietPlanDaySummaryDto> Days);

public record DietPlanDayDto(
    Guid PlanId,
    int DayNumber,
    DateOnly? Date,
    string? Notes,
    string? WaterNote,
    string? ActivityNote,
    string? SleepNote,
    bool IsToday,
    IReadOnlyList<MealCardDto> Meals);

public record WaterEntryDto(Guid Id, int AmountMl, DateTime LoggedAtUtc);
public record WaterDayResponse(string LocalDate, WaterSummaryDto Summary, string? Guidance, IReadOnlyList<WaterEntryDto> Entries);

public record LogWaterRequest(int AmountMl, string? LocalDate);

public record WeightEntryDto(Guid Id, string LocalDate, decimal WeightKg, bool IsMorning, string? Note);
public record WeightHistoryResponse(
    decimal? CurrentKg,
    decimal? TargetKg,
    decimal? WeeklyChangeKg,
    decimal? MonthlyChangeKg,
    int? ProgressPercent,
    IReadOnlyList<WeightEntryDto> Entries);
public record LogWeightRequest(decimal WeightKg, bool IsMorning, string? Note, string? LocalDate);

public record LogMealItemRequest(Guid? FoodId, string Name, decimal Quantity, string Unit);
public record LogMealRequest(string MealType, string Status, string? Notes, string? LocalDate, Guid? MealId, IReadOnlyList<LogMealItemRequest>? Items);
public record MealLogDto(Guid Id, string LocalDate, string MealType, string Status, string? Notes, IReadOnlyList<LogMealItemRequest> Items);

public record LogActivityRequest(string ActivityType, int DurationMinutes, string? Notes, bool Completed, string? LocalDate);
public record ActivityDto(Guid Id, string LocalDate, string ActivityType, int DurationMinutes, bool Completed, string? Notes, int GoalMinutes);

public record LogSleepRequest(int DurationMinutes, int? Quality, string? Notes, string? LocalDate);
public record SleepDto(Guid Id, string LocalDate, int DurationMinutes, int? Quality, string? Notes, int GoalMinutes);

public record CheckInRequest(int? Mood, int? Energy, string? Notes, IReadOnlyList<string>? CompletedHabitCodes, string? LocalDate);
public record HabitDto(string Code, string Title, string Description, bool Completed);
public record CheckInResponse(string LocalDate, int? Mood, int? Energy, string? Notes, AdherenceSummaryDto Adherence, IReadOnlyList<HabitDto> Habits);

public record ProgressPointDto(string Date, decimal Value);
public record ProgressResponse(
    string Range,
    IReadOnlyList<ProgressPointDto> Weight,
    IReadOnlyList<ProgressPointDto> Adherence,
    IReadOnlyList<ProgressPointDto> Water,
    IReadOnlyList<ProgressPointDto> Meals,
    IReadOnlyList<ProgressPointDto> Activity,
    IReadOnlyList<ProgressPointDto> Sleep,
    decimal? WeightChange,
    decimal AverageAdherence);

public record CalendarDayDto(
    string Date,
    int? PlanDayNumber,
    bool HasMeals,
    int MealsCompleted,
    int MealsPlanned,
    int WaterMl,
    decimal? WeightKg,
    int? ActivityMinutes,
    int? SleepMinutes,
    int? AdherenceScore);

public record FoodDto(
    Guid Id,
    string Name,
    string? LocalName,
    string Category,
    string GroceryCategory,
    string ServingLabel,
    decimal ServingQuantity,
    string ServingUnit,
    decimal Calories,
    decimal ProteinG,
    decimal CarbohydratesG,
    decimal FatG,
    decimal FibreG,
    string? Allergens,
    string? Tags,
    string? MicronutrientsNote,
    string? ProviderNote,
    decimal? ReferencePriceInr,
    decimal? YourPriceInr);

public record ShoppingLineDto(Guid FoodId, string Name, string Category, decimal Quantity, string Unit, decimal? EstimatedCost, bool Purchased);
public record ShoppingListResponse(string StartDate, int Days, decimal TotalKnownCost, bool HasMissingPrices, string Currency, IReadOnlyList<ShoppingLineDto> Lines);
public record ShoppingToggleRequest(Guid FoodId, int RangeDays, string? StartDate, bool Purchased);

public record BudgetMealDto(string MealName, decimal EstimatedCost, decimal SpentCost);
public record BudgetResponse(
    decimal DailyBudget,
    string Currency,
    decimal Spent,
    decimal Remaining,
    decimal PlannedToday,
    decimal ProjectedDaily,
    decimal ProjectedPeriod,
    decimal ProjectedMonthly,
    int PeriodDays,
    bool HasMissingPrices,
    string PriceNote,
    IReadOnlyList<BudgetMealDto> Meals);
public record UpsertBudgetRequest(decimal DailyBudgetAmount, string Currency);
public record UpsertFoodPriceRequest(Guid FoodId, decimal PricePerServing);

public record NotificationPreferenceDto(string Category, bool Enabled, string? LocalTime, int? IntervalMinutes, string? WindowStart, string? WindowEnd);
public record NotificationSettingsResponse(bool QuietHoursEnabled, string QuietStart, string QuietEnd, IReadOnlyList<NotificationPreferenceDto> Preferences, IReadOnlyList<ReminderDto> Schedule);
public record UpdateNotificationSettingsRequest(bool QuietHoursEnabled, string QuietStart, string QuietEnd, IReadOnlyList<NotificationPreferenceDto> Preferences);

public record GuidanceDto(Guid Id, string Category, string Title, string Body, string Label, string? ConditionKey);
public record GuidanceResponse(string Disclaimer, IReadOnlyList<GuidanceDto> Items, IReadOnlyList<HabitDto> Habits);

public record EntitlementDto(string Plan, string Status, DateTime StartsAtUtc, DateTime? EndsAtUtc, string Source, IReadOnlyList<string> Features);
public record GrantEntitlementRequest(Guid UserId, string Plan, string? EndsAtUtc);

public record AnalyticsEventRequest(string Name, string? PropertiesJson);

public record UpsertFoodRequest(
    string Name,
    string? LocalName,
    Guid CategoryId,
    string ServingLabel,
    decimal ServingQuantity,
    string ServingUnit,
    decimal Calories,
    decimal ProteinG,
    decimal CarbohydratesG,
    decimal FatG,
    decimal FibreG,
    string? Allergens,
    string? Tags,
    string GroceryCategory,
    string SuitableFor,
    decimal? ReferencePriceInr,
    string? ProviderNote,
    bool IsActive);

public record CreateDietPlanRequest(string Name, string Description, int DurationDays);
public record CreatePlanDayRequest(int DayNumber, string? Notes, string? WaterNote, string? ActivityNote, string? SleepNote);
public record CreateMealRequest(string MealType, string Title, string ScheduledTime, string? Notes);
public record MealItemWriteRequest(Guid? FoodId, string DisplayName, decimal Quantity, string Unit, int SortOrder, bool IsOptional, string? AlternativeGroup, bool IsDefaultAlternative, string? Note);
public record UpdateMealRequest(string Title, string? Notes, string ScheduledTime, IReadOnlyList<MealItemWriteRequest> Items);
public record AssignPlanRequest(Guid UserId, Guid DietPlanId, string StartDate);
public record CategoryDto(Guid Id, string Name, string Slug, string GroceryCategory);
