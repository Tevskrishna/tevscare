using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Tevscare.Application.Abstractions;
using Tevscare.Application.Contracts;
using Tevscare.Domain.Identity;

namespace Tevscare.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await _auth.RegisterAsync(request, cancellationToken));

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    public async Task<AuthResponse> Login(LoginRequest request, CancellationToken cancellationToken) =>
        await _auth.LoginAsync(request, cancellationToken);

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("refresh")]
    public async Task<AuthResponse> Refresh(RefreshRequest request, CancellationToken cancellationToken) =>
        await _auth.RefreshAsync(request, cancellationToken);

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        await _auth.LogoutAsync(request, cancellationToken);
        return NoContent();
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("forgot-password")]
    public async Task<MessageResponse> Forgot(ForgotPasswordRequest request, CancellationToken cancellationToken) =>
        await _auth.ForgotPasswordAsync(request, cancellationToken);

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("reset-password")]
    public async Task<MessageResponse> Reset(ResetPasswordRequest request, CancellationToken cancellationToken) =>
        await _auth.ResetPasswordAsync(request, cancellationToken);
}

[ApiController]
[Authorize]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profiles;
    private readonly ICurrentUser _current;

    public ProfileController(IProfileService profiles, ICurrentUser current)
    {
        _profiles = profiles;
        _current = current;
    }

    [HttpGet]
    public Task<ProfileResponse> Get(CancellationToken cancellationToken) => _profiles.GetAsync(_current.UserId, cancellationToken);

    [HttpPut]
    public Task<ProfileResponse> Update(UpdateProfileRequest request, CancellationToken cancellationToken) =>
        _profiles.UpdateAsync(_current.UserId, request, cancellationToken);

    [HttpDelete]
    public async Task<IActionResult> Delete(CancellationToken cancellationToken)
    {
        await _profiles.DeleteAccountAsync(_current.UserId, cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Authorize]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;
    private readonly ICurrentUser _current;

    public DashboardController(IDashboardService dashboard, ICurrentUser current)
    {
        _dashboard = dashboard;
        _current = current;
    }

    [HttpGet]
    public Task<DashboardResponse> Get([FromQuery] string? date, CancellationToken cancellationToken) =>
        _dashboard.GetAsync(_current.UserId, date, cancellationToken);
}

[ApiController]
[Authorize]
[Route("api/diet-plans")]
public class DietPlansController : ControllerBase
{
    private readonly IDietPlanService _plans;
    private readonly ICurrentUser _current;

    public DietPlansController(IDietPlanService plans, ICurrentUser current)
    {
        _plans = plans;
        _current = current;
    }

    [HttpGet]
    public Task<IReadOnlyList<DietPlanSummaryDto>> List(CancellationToken cancellationToken) => _plans.ListAsync(_current.UserId, cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<DietPlanDetailDto> Get(Guid id, CancellationToken cancellationToken) => _plans.GetAsync(_current.UserId, id, cancellationToken);

    [HttpGet("{id:guid}/days/{day:int}")]
    public Task<DietPlanDayDto> Day(Guid id, int day, CancellationToken cancellationToken) => _plans.GetDayAsync(_current.UserId, id, day, cancellationToken);
}

[ApiController]
[Authorize]
[Route("api/meals")]
public class MealsController : ControllerBase
{
    private readonly IDietPlanService _plans;
    private readonly IMealLogService _logs;
    private readonly ICurrentUser _current;

    public MealsController(IDietPlanService plans, IMealLogService logs, ICurrentUser current)
    {
        _plans = plans;
        _logs = logs;
        _current = current;
    }

    [HttpGet("today")]
    public Task<IReadOnlyList<MealCardDto>> Today([FromQuery] string? date, CancellationToken cancellationToken) =>
        _plans.GetTodayMealsAsync(_current.UserId, date, cancellationToken);

    [HttpGet("logs")]
    public Task<IReadOnlyList<MealLogDto>> Logs([FromQuery] string? date, CancellationToken cancellationToken) =>
        _logs.GetByDateAsync(_current.UserId, date, cancellationToken);

    [HttpPost("log")]
    public Task<MealLogDto> Log(LogMealRequest request, CancellationToken cancellationToken) =>
        _logs.LogAsync(_current.UserId, request, cancellationToken);
}

[ApiController]
[Authorize]
[Route("api/water")]
public class WaterController : ControllerBase
{
    private readonly IWaterService _water;
    private readonly ICurrentUser _current;

    public WaterController(IWaterService water, ICurrentUser current)
    {
        _water = water;
        _current = current;
    }

    [HttpGet("today")]
    public Task<WaterDayResponse> Today([FromQuery] string? date, CancellationToken cancellationToken) =>
        _water.GetTodayAsync(_current.UserId, date, cancellationToken);

    [HttpPost]
    public Task<WaterDayResponse> Add(LogWaterRequest request, CancellationToken cancellationToken) =>
        _water.AddAsync(_current.UserId, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public Task<WaterDayResponse> Delete(Guid id, CancellationToken cancellationToken) =>
        _water.DeleteAsync(_current.UserId, id, cancellationToken);
}

[ApiController]
[Authorize]
[Route("api/weight")]
public class WeightController : ControllerBase
{
    private readonly IWeightService _weight;
    private readonly ICurrentUser _current;

    public WeightController(IWeightService weight, ICurrentUser current)
    {
        _weight = weight;
        _current = current;
    }

    [HttpGet]
    public Task<WeightHistoryResponse> Get([FromQuery] int days = 90, CancellationToken cancellationToken = default) =>
        _weight.GetAsync(_current.UserId, days, cancellationToken);

    [HttpPost]
    public Task<WeightEntryDto> Log(LogWeightRequest request, CancellationToken cancellationToken) =>
        _weight.LogAsync(_current.UserId, request, cancellationToken);
}

[ApiController]
[Authorize]
[Route("api/activity")]
public class ActivityController : ControllerBase
{
    private readonly IActivityService _activity;
    private readonly ICurrentUser _current;

    public ActivityController(IActivityService activity, ICurrentUser current)
    {
        _activity = activity;
        _current = current;
    }

    [HttpGet]
    public Task<ActivityDto?> Get([FromQuery] string? date, CancellationToken cancellationToken) =>
        _activity.GetAsync(_current.UserId, date, cancellationToken);

    [HttpPost]
    public Task<ActivityDto> Log(LogActivityRequest request, CancellationToken cancellationToken) =>
        _activity.LogAsync(_current.UserId, request, cancellationToken);
}

[ApiController]
[Authorize]
[Route("api/sleep")]
public class SleepController : ControllerBase
{
    private readonly ISleepService _sleep;
    private readonly ICurrentUser _current;

    public SleepController(ISleepService sleep, ICurrentUser current)
    {
        _sleep = sleep;
        _current = current;
    }

    [HttpGet]
    public Task<SleepDto?> Get([FromQuery] string? date, CancellationToken cancellationToken) =>
        _sleep.GetAsync(_current.UserId, date, cancellationToken);

    [HttpPost]
    public Task<SleepDto> Log(LogSleepRequest request, CancellationToken cancellationToken) =>
        _sleep.LogAsync(_current.UserId, request, cancellationToken);
}

[ApiController]
[Authorize]
[Route("api/check-in")]
public class CheckInController : ControllerBase
{
    private readonly ICheckInService _checkIn;
    private readonly ICurrentUser _current;

    public CheckInController(ICheckInService checkIn, ICurrentUser current)
    {
        _checkIn = checkIn;
        _current = current;
    }

    [HttpGet]
    public Task<CheckInResponse> Get([FromQuery] string? date, CancellationToken cancellationToken) =>
        _checkIn.GetAsync(_current.UserId, date, cancellationToken);

    [HttpPost]
    public Task<CheckInResponse> Save(CheckInRequest request, CancellationToken cancellationToken) =>
        _checkIn.SaveAsync(_current.UserId, request, cancellationToken);
}

[ApiController]
[Authorize]
[Route("api/progress")]
public class ProgressController : ControllerBase
{
    private readonly IProgressService _progress;
    private readonly ICurrentUser _current;

    public ProgressController(IProgressService progress, ICurrentUser current)
    {
        _progress = progress;
        _current = current;
    }

    [HttpGet]
    public Task<ProgressResponse> Get([FromQuery] string range = "7", CancellationToken cancellationToken = default) =>
        _progress.GetAsync(_current.UserId, range, cancellationToken);
}

[ApiController]
[Authorize]
[Route("api/calendar")]
public class CalendarController : ControllerBase
{
    private readonly IProgressService _progress;
    private readonly ICurrentUser _current;

    public CalendarController(IProgressService progress, ICurrentUser current)
    {
        _progress = progress;
        _current = current;
    }

    [HttpGet]
    public Task<IReadOnlyList<CalendarDayDto>> Get([FromQuery] string? month, CancellationToken cancellationToken) =>
        _progress.GetCalendarAsync(_current.UserId, month, cancellationToken);
}

[ApiController]
[Authorize]
[Route("api/shopping-list")]
public class ShoppingListController : ControllerBase
{
    private readonly IShoppingListService _shopping;
    private readonly ICurrentUser _current;

    public ShoppingListController(IShoppingListService shopping, ICurrentUser current)
    {
        _shopping = shopping;
        _current = current;
    }

    [HttpGet]
    public Task<ShoppingListResponse> Get([FromQuery] int days = 15, [FromQuery] string? startDate = null, CancellationToken cancellationToken = default) =>
        _shopping.GetAsync(_current.UserId, days, startDate, cancellationToken);

    [HttpPost("toggle")]
    public Task<ShoppingListResponse> Toggle(ShoppingToggleRequest request, CancellationToken cancellationToken) =>
        _shopping.ToggleAsync(_current.UserId, request, cancellationToken);
}

[ApiController]
[Authorize]
[Route("api/budget")]
public class BudgetController : ControllerBase
{
    private readonly IBudgetService _budget;
    private readonly ICurrentUser _current;

    public BudgetController(IBudgetService budget, ICurrentUser current)
    {
        _budget = budget;
        _current = current;
    }

    [HttpGet]
    public Task<BudgetResponse> Get([FromQuery] string? date, CancellationToken cancellationToken) =>
        _budget.GetAsync(_current.UserId, date, cancellationToken);

    [HttpPost]
    public Task<BudgetResponse> Upsert(UpsertBudgetRequest request, CancellationToken cancellationToken) =>
        _budget.UpsertAsync(_current.UserId, request, cancellationToken);

    [HttpPost("prices")]
    public async Task<IActionResult> Price(UpsertFoodPriceRequest request, CancellationToken cancellationToken)
    {
        await _budget.SetFoodPriceAsync(_current.UserId, request, cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Authorize]
[Route("api/notifications/preferences")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationPreferenceService _notifications;
    private readonly ICurrentUser _current;

    public NotificationsController(INotificationPreferenceService notifications, ICurrentUser current)
    {
        _notifications = notifications;
        _current = current;
    }

    [HttpGet]
    public Task<NotificationSettingsResponse> Get(CancellationToken cancellationToken) =>
        _notifications.GetAsync(_current.UserId, cancellationToken);

    [HttpPut]
    public Task<NotificationSettingsResponse> Update(UpdateNotificationSettingsRequest request, CancellationToken cancellationToken) =>
        _notifications.UpdateAsync(_current.UserId, request, cancellationToken);
}

[ApiController]
[Authorize]
[Route("api/foods")]
public class FoodsController : ControllerBase
{
    private readonly IFoodService _foods;
    private readonly ICurrentUser _current;

    public FoodsController(IFoodService foods, ICurrentUser current)
    {
        _foods = foods;
        _current = current;
    }

    [HttpGet]
    public Task<IReadOnlyList<FoodDto>> Search([FromQuery] string? query, [FromQuery] string? category, [FromQuery] int page = 1, [FromQuery] int pageSize = 30, CancellationToken cancellationToken = default) =>
        _foods.SearchAsync(_current.UserId, query, category, page, pageSize, cancellationToken);

    [HttpGet("categories")]
    public Task<IReadOnlyList<CategoryDto>> Categories(CancellationToken cancellationToken) => _foods.CategoriesAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<FoodDto> Get(Guid id, CancellationToken cancellationToken) => _foods.GetAsync(_current.UserId, id, cancellationToken);
}

[ApiController]
[Authorize]
[Route("api/guidance")]
public class GuidanceController : ControllerBase
{
    private readonly IGuidanceService _guidance;
    private readonly ICurrentUser _current;

    public GuidanceController(IGuidanceService guidance, ICurrentUser current)
    {
        _guidance = guidance;
        _current = current;
    }

    [HttpGet]
    public Task<GuidanceResponse> Get([FromQuery] string? date, CancellationToken cancellationToken) =>
        _guidance.GetAsync(_current.UserId, date, cancellationToken);
}

[ApiController]
[Authorize]
[Route("api/entitlements")]
public class EntitlementsController : ControllerBase
{
    private readonly IEntitlementService _entitlements;
    private readonly ICurrentUser _current;

    public EntitlementsController(IEntitlementService entitlements, ICurrentUser current)
    {
        _entitlements = entitlements;
        _current = current;
    }

    [HttpGet("me")]
    public Task<EntitlementDto> Me(CancellationToken cancellationToken) => _entitlements.GetMineAsync(_current.UserId, cancellationToken);
}

[ApiController]
[Authorize]
[Route("api/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analytics;
    private readonly ICurrentUser _current;

    public AnalyticsController(IAnalyticsService analytics, ICurrentUser current)
    {
        _analytics = analytics;
        _current = current;
    }

    [HttpPost("events")]
    public async Task<IActionResult> Track(AnalyticsEventRequest request, CancellationToken cancellationToken)
    {
        await _analytics.TrackAsync(_current.UserId, request, cancellationToken);
        return Accepted();
    }
}

[ApiController]
[Authorize(Roles = $"{Roles.Admin},{Roles.Nutritionist}")]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminContentService _admin;
    private readonly IAdminDirectoryService _directory;
    private readonly IEntitlementService _entitlements;
    private readonly ICurrentUser _current;

    public AdminController(IAdminContentService admin, IAdminDirectoryService directory, IEntitlementService entitlements, ICurrentUser current)
    {
        _admin = admin;
        _directory = directory;
        _entitlements = entitlements;
        _current = current;
    }

    [HttpPost("foods")]
    public async Task<FoodDto> CreateFood(UpsertFoodRequest request, CancellationToken cancellationToken)
    {
        var food = await _admin.UpsertFoodAsync(null, request, cancellationToken);
        await _directory.RecordAsync(_current.UserId, "FoodCreated", "Food", food.Id.ToString(), food.Name, cancellationToken);
        return food;
    }

    [HttpPut("foods/{id:guid}")]
    public async Task<FoodDto> UpdateFood(Guid id, UpsertFoodRequest request, CancellationToken cancellationToken)
    {
        var food = await _admin.UpsertFoodAsync(id, request, cancellationToken);
        await _directory.RecordAsync(_current.UserId, "FoodUpdated", "Food", food.Id.ToString(), food.Name, cancellationToken);
        return food;
    }

    [HttpPost("diet-plans")]
    public async Task<DietPlanSummaryDto> CreatePlan(CreateDietPlanRequest request, CancellationToken cancellationToken)
    {
        var plan = await _admin.CreatePlanAsync(_current.UserId, request, cancellationToken);
        await _directory.RecordAsync(_current.UserId, "PlanCreated", "DietPlan", plan.Id.ToString(), plan.Name, cancellationToken);
        return plan;
    }

    [HttpPost("diet-plans/{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, [FromQuery] bool published = true, CancellationToken cancellationToken = default)
    {
        await _admin.PublishAsync(id, published, cancellationToken);
        await _directory.RecordAsync(_current.UserId, published ? "PlanPublished" : "PlanUnpublished", "DietPlan", id.ToString(), null, cancellationToken);
        return NoContent();
    }

    [HttpPost("diet-plans/{id:guid}/days")]
    public Task<DietPlanDayDto> AddDay(Guid id, CreatePlanDayRequest request, CancellationToken cancellationToken) =>
        _admin.AddDayAsync(id, request, cancellationToken);

    [HttpPost("diet-plans/{id:guid}/days/{day:int}/meals")]
    public Task<MealCardDto> AddMeal(Guid id, int day, CreateMealRequest request, CancellationToken cancellationToken) =>
        _admin.AddMealAsync(id, day, request, cancellationToken);

    [HttpPut("meals/{id:guid}")]
    public Task<MealCardDto> UpdateMeal(Guid id, UpdateMealRequest request, CancellationToken cancellationToken) =>
        _admin.UpdateMealAsync(id, request, cancellationToken);

    [HttpPost("assignments")]
    public async Task<IActionResult> Assign(AssignPlanRequest request, CancellationToken cancellationToken)
    {
        await _admin.AssignPlanAsync(request, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost("entitlements")]
    public Task<EntitlementDto> Grant(GrantEntitlementRequest request, CancellationToken cancellationToken) =>
        _entitlements.GrantAsync(request, cancellationToken);
}
