using Tevscare.Application.Contracts;

namespace Tevscare.Application.Abstractions;

public interface ICurrentUser
{
    Guid UserId { get; }
    bool IsAuthenticated { get; }
}

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken);
}

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken);
    Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken);
    Task<MessageResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken);
    Task<MessageResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);
}

public interface IProfileService
{
    Task<ProfileResponse> GetAsync(Guid userId, CancellationToken cancellationToken);
    Task<ProfileResponse> UpdateAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken);
    Task DeleteAccountAsync(Guid userId, CancellationToken cancellationToken);
}

public interface IDashboardService
{
    Task<DashboardResponse> GetAsync(Guid userId, string? date, CancellationToken cancellationToken);
}

public interface IDietPlanService
{
    Task<IReadOnlyList<DietPlanSummaryDto>> ListAsync(Guid userId, CancellationToken cancellationToken);
    Task<DietPlanDetailDto> GetAsync(Guid userId, Guid planId, CancellationToken cancellationToken);
    Task<DietPlanDayDto> GetDayAsync(Guid userId, Guid planId, int day, CancellationToken cancellationToken);
    Task<IReadOnlyList<MealCardDto>> GetTodayMealsAsync(Guid userId, string? date, CancellationToken cancellationToken);
}

public interface IMealLogService
{
    Task<MealLogDto> LogAsync(Guid userId, LogMealRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<MealLogDto>> GetByDateAsync(Guid userId, string? date, CancellationToken cancellationToken);
}

public interface IWaterService
{
    Task<WaterDayResponse> GetTodayAsync(Guid userId, string? date, CancellationToken cancellationToken);
    Task<WaterDayResponse> AddAsync(Guid userId, LogWaterRequest request, CancellationToken cancellationToken);
    Task<WaterDayResponse> DeleteAsync(Guid userId, Guid entryId, CancellationToken cancellationToken);
}

public interface IWeightService
{
    Task<WeightHistoryResponse> GetAsync(Guid userId, int days, CancellationToken cancellationToken);
    Task<WeightEntryDto> LogAsync(Guid userId, LogWeightRequest request, CancellationToken cancellationToken);
}

public interface IActivityService
{
    Task<ActivityDto?> GetAsync(Guid userId, string? date, CancellationToken cancellationToken);
    Task<ActivityDto> LogAsync(Guid userId, LogActivityRequest request, CancellationToken cancellationToken);
}

public interface ISleepService
{
    Task<SleepDto?> GetAsync(Guid userId, string? date, CancellationToken cancellationToken);
    Task<SleepDto> LogAsync(Guid userId, LogSleepRequest request, CancellationToken cancellationToken);
}

public interface ICheckInService
{
    Task<CheckInResponse> GetAsync(Guid userId, string? date, CancellationToken cancellationToken);
    Task<CheckInResponse> SaveAsync(Guid userId, CheckInRequest request, CancellationToken cancellationToken);
}

public interface IProgressService
{
    Task<ProgressResponse> GetAsync(Guid userId, string range, CancellationToken cancellationToken);
    Task<IReadOnlyList<CalendarDayDto>> GetCalendarAsync(Guid userId, string? month, CancellationToken cancellationToken);
}

public interface IBudgetService
{
    Task<BudgetResponse> GetAsync(Guid userId, string? date, CancellationToken cancellationToken);
    Task<BudgetResponse> UpsertAsync(Guid userId, UpsertBudgetRequest request, CancellationToken cancellationToken);
    Task SetFoodPriceAsync(Guid userId, UpsertFoodPriceRequest request, CancellationToken cancellationToken);
}

public interface IShoppingListService
{
    Task<ShoppingListResponse> GetAsync(Guid userId, int days, string? startDate, CancellationToken cancellationToken);
    Task<ShoppingListResponse> ToggleAsync(Guid userId, ShoppingToggleRequest request, CancellationToken cancellationToken);
}

public interface IFoodService
{
    Task<IReadOnlyList<FoodDto>> SearchAsync(Guid userId, string? query, string? category, int page, int pageSize, CancellationToken cancellationToken);
    Task<FoodDto> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<CategoryDto>> CategoriesAsync(CancellationToken cancellationToken);
}

public interface INotificationPreferenceService
{
    Task<NotificationSettingsResponse> GetAsync(Guid userId, CancellationToken cancellationToken);
    Task<NotificationSettingsResponse> UpdateAsync(Guid userId, UpdateNotificationSettingsRequest request, CancellationToken cancellationToken);
}

public interface IGuidanceService
{
    Task<GuidanceResponse> GetAsync(Guid userId, string? date, CancellationToken cancellationToken);
}

public interface IEntitlementService
{
    Task<EntitlementDto> GetMineAsync(Guid userId, CancellationToken cancellationToken);
    Task<EntitlementDto> GrantAsync(GrantEntitlementRequest request, CancellationToken cancellationToken);
}

public interface IAnalyticsService
{
    Task TrackAsync(Guid? userId, AnalyticsEventRequest request, CancellationToken cancellationToken);
}

public interface IAdminContentService
{
    Task<FoodDto> UpsertFoodAsync(Guid? id, UpsertFoodRequest request, CancellationToken cancellationToken);
    Task<DietPlanSummaryDto> CreatePlanAsync(Guid actorId, CreateDietPlanRequest request, CancellationToken cancellationToken);
    Task<DietPlanDayDto> AddDayAsync(Guid planId, CreatePlanDayRequest request, CancellationToken cancellationToken);
    Task<MealCardDto> AddMealAsync(Guid planId, int dayNumber, CreateMealRequest request, CancellationToken cancellationToken);
    Task<MealCardDto> UpdateMealAsync(Guid mealId, UpdateMealRequest request, CancellationToken cancellationToken);
    Task AssignPlanAsync(AssignPlanRequest request, CancellationToken cancellationToken);
    Task PublishAsync(Guid planId, bool published, CancellationToken cancellationToken);
}

public interface IAdminDirectoryService
{
    Task<AdminDashboardResponse> DashboardAsync(CancellationToken cancellationToken);
    Task<AdminUserPage> UsersAsync(string? query, int page, int pageSize, CancellationToken cancellationToken);
    Task<AdminUserDetail> UserAsync(Guid userId, CancellationToken cancellationToken);
    Task LockAsync(Guid actorId, Guid userId, bool locked, CancellationToken cancellationToken);
    Task<AdminUserListItem> CreateNutritionistAsync(Guid actorId, CreateNutritionistRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<AdminPlanDto>> PlansAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<GuidanceDto>> GuidanceAsync(CancellationToken cancellationToken);
    Task<AdminAuditPage> AuditAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task RecordAsync(Guid actorId, string action, string entityName, string? entityId, string? detail, CancellationToken cancellationToken);
}
