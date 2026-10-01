using Microsoft.EntityFrameworkCore;
using Tevscare.Application.Abstractions;
using Tevscare.Application.Common;
using Tevscare.Application.Contracts;
using Tevscare.Application.Planning;
using Tevscare.Domain.Account;
using Tevscare.Domain.Catalog;
using Tevscare.Domain.Enums;
using Tevscare.Domain.Identity;
using Tevscare.Domain.Planning;
using Tevscare.Infrastructure.Persistence;

namespace Tevscare.Infrastructure.Services;

public class ContentService : IFoodService, INotificationPreferenceService, IGuidanceService, IEntitlementService, IAnalyticsService, IAdminContentService
{
    private static readonly HashSet<string> AnalyticsNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "app_opened", "onboarding_completed", "meal_viewed", "meal_completed", "water_logged", "weight_logged", "plan_completed", "notification_opened"
    };

    private readonly AppDbContext _db;

    public ContentService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<FoodDto>> SearchAsync(Guid userId, string? query, string? category, int page, int pageSize, CancellationToken cancellationToken)
    {
        var size = Math.Clamp(pageSize, 1, 50);
        var skip = Math.Max(0, page - 1) * size;
        var foods = _db.Foods.AsNoTracking().Include(item => item.Category).Where(item => item.IsActive);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToLower();
            foods = foods.Where(item => item.Name.ToLower().Contains(term) || (item.LocalName != null && item.LocalName.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            foods = foods.Where(item => item.Category.Slug == category || item.Category.Name == category);
        }

        var pageItems = await foods.OrderBy(item => item.Name).Skip(skip).Take(size).ToListAsync(cancellationToken);
        var ids = pageItems.Select(item => item.Id).ToList();
        var prices = await _db.FoodPrices.AsNoTracking().Where(item => item.UserId == userId && ids.Contains(item.FoodId)).ToDictionaryAsync(item => item.FoodId, item => item.PricePerServing, cancellationToken);
        return pageItems.Select(item => MapFood(item, prices.TryGetValue(item.Id, out var price) ? price : null)).ToList();
    }

    public async Task<FoodDto> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var food = await _db.Foods.AsNoTracking().Include(item => item.Category).FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw AppException.NotFound("Food was not found.");
        var price = await _db.FoodPrices.AsNoTracking().Where(item => item.UserId == userId && item.FoodId == id).Select(item => (decimal?)item.PricePerServing).FirstOrDefaultAsync(cancellationToken);
        return MapFood(food, price);
    }

    public async Task<IReadOnlyList<CategoryDto>> CategoriesAsync(CancellationToken cancellationToken) =>
        await _db.FoodCategories.AsNoTracking().OrderBy(item => item.SortOrder)
            .Select(item => new CategoryDto(item.Id, item.Name, item.Slug, item.GroceryCategory.ToString()))
            .ToListAsync(cancellationToken);

    public async Task<NotificationSettingsResponse> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var settings = await EnsureAsync(userId, cancellationToken);
        return MapNotifications(settings);
    }

    public async Task<NotificationSettingsResponse> UpdateAsync(Guid userId, UpdateNotificationSettingsRequest request, CancellationToken cancellationToken)
    {
        var settings = await EnsureAsync(userId, cancellationToken);
        settings.QuietHoursEnabled = request.QuietHoursEnabled;
        settings.QuietStart = Parsers.ParseTime(request.QuietStart, "Quiet hours start");
        settings.QuietEnd = Parsers.ParseTime(request.QuietEnd, "Quiet hours end");
        _db.NotificationPreferences.RemoveRange(settings.Preferences.ToList());
        settings.Preferences.Clear();
        foreach (var preference in request.Preferences)
        {
            var entity = new NotificationPreference
            {
                SettingsId = settings.Id,
                Category = Parsers.ParseEnum<NotificationCategory>(preference.Category, "Notification"),
                Enabled = preference.Enabled,
                LocalTime = string.IsNullOrWhiteSpace(preference.LocalTime) ? null : Parsers.ParseTime(preference.LocalTime, "Time"),
                IntervalMinutes = preference.IntervalMinutes,
                WindowStart = string.IsNullOrWhiteSpace(preference.WindowStart) ? null : Parsers.ParseTime(preference.WindowStart, "Window start"),
                WindowEnd = string.IsNullOrWhiteSpace(preference.WindowEnd) ? null : Parsers.ParseTime(preference.WindowEnd, "Window end")
            };
            _db.NotificationPreferences.Add(entity);
            settings.Preferences.Add(entity);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return MapNotifications(settings);
    }

    public async Task<GuidanceResponse> GetAsync(Guid userId, string? date, CancellationToken cancellationToken)
    {
        var (user, _) = await UserLoader.RequireAsync(_db, userId, cancellationToken);
        var local = Parsers.ParseDateOrToday(date, user.Timezone, DateTime.UtcNow);
        var assignment = await _db.PlanAssignments.AsNoTracking()
            .Where(item => item.UserId == userId && item.Status == PlanAssignmentStatus.Active)
            .OrderByDescending(item => item.StartDate)
            .FirstOrDefaultAsync(cancellationToken);
        var guidanceQuery = _db.Guidance.AsNoTracking().AsQueryable();
        if (assignment is not null)
        {
            guidanceQuery = guidanceQuery.Where(item => item.DietPlanId == null || item.DietPlanId == assignment.DietPlanId);
        }

        var guidance = await guidanceQuery.OrderBy(item => item.SortOrder).ToListAsync(cancellationToken);

        var habits = await _db.HabitDefinitions.AsNoTracking().OrderBy(item => item.SortOrder).ToListAsync(cancellationToken);
        var done = await _db.HabitChecks.AsNoTracking().Where(item => item.UserId == userId && item.LocalDate == local && item.Completed).Select(item => item.HabitCode).ToListAsync(cancellationToken);
        return new GuidanceResponse(
            ProductDisclaimer.Text,
            guidance.Select(item => new GuidanceDto(item.Id, item.Category.ToString(), item.Title, item.Body, item.Label, item.ConditionKey)).ToList(),
            habits.Select(item => new HabitDto(item.Code, item.Title, item.Description, done.Contains(item.Code))).ToList());
    }

    public async Task<EntitlementDto> GetMineAsync(Guid userId, CancellationToken cancellationToken)
    {
        var entitlement = await _db.Entitlements.AsNoTracking()
            .Where(item => item.UserId == userId && item.Status == EntitlementStatus.Active && (item.EndsAtUtc == null || item.EndsAtUtc > DateTime.UtcNow))
            .OrderByDescending(item => item.StartsAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        entitlement ??= new Entitlement { Plan = EntitlementPlan.Free, Status = EntitlementStatus.Active, StartsAtUtc = DateTime.UtcNow, Source = "system" };
        return MapEntitlement(entitlement);
    }

    public async Task<EntitlementDto> GrantAsync(GrantEntitlementRequest request, CancellationToken cancellationToken)
    {
        var plan = Parsers.ParseEnum<EntitlementPlan>(request.Plan, "Plan");
        var current = await _db.Entitlements.Where(item => item.UserId == request.UserId && item.Status == EntitlementStatus.Active).ToListAsync(cancellationToken);
        foreach (var item in current)
        {
            item.Status = EntitlementStatus.Cancelled;
        }

        DateTime? ends = null;
        if (!string.IsNullOrWhiteSpace(request.EndsAtUtc))
        {
            if (!DateTime.TryParse(request.EndsAtUtc, out var parsed))
            {
                throw new AppException("End date is not valid.");
            }

            ends = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
        }

        var entitlement = new Entitlement
        {
            UserId = request.UserId,
            Plan = plan,
            Status = EntitlementStatus.Active,
            StartsAtUtc = DateTime.UtcNow,
            EndsAtUtc = ends,
            Source = "manual"
        };
        _db.Entitlements.Add(entitlement);
        await _db.SaveChangesAsync(cancellationToken);
        return MapEntitlement(entitlement);
    }

    public async Task TrackAsync(Guid? userId, AnalyticsEventRequest request, CancellationToken cancellationToken)
    {
        if (!AnalyticsNames.Contains(request.Name))
        {
            throw new AppException("Unknown analytics event.");
        }

        if (request.PropertiesJson?.Contains("password", StringComparison.OrdinalIgnoreCase) == true
            || request.PropertiesJson?.Contains("token", StringComparison.OrdinalIgnoreCase) == true)
        {
            throw new AppException("Analytics properties cannot include secrets.");
        }

        _db.ProductEvents.Add(new ProductEvent
        {
            UserId = userId,
            Name = request.Name,
            OccurredAtUtc = DateTime.UtcNow,
            PropertiesJson = request.PropertiesJson
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<FoodDto> UpsertFoodAsync(Guid? id, UpsertFoodRequest request, CancellationToken cancellationToken)
    {
        var category = await _db.FoodCategories.FirstOrDefaultAsync(item => item.Id == request.CategoryId, cancellationToken)
            ?? throw AppException.NotFound("Food category was not found.");
        var food = id is null ? new Food() : await _db.Foods.FirstOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw AppException.NotFound("Food was not found.");
        if (id is null)
        {
            _db.Foods.Add(food);
        }

        food.CategoryId = category.Id;
        food.Name = request.Name.Trim();
        food.LocalName = request.LocalName?.Trim();
        food.ServingLabel = request.ServingLabel.Trim();
        food.ServingQuantity = request.ServingQuantity;
        food.ServingUnit = request.ServingUnit.Trim();
        food.Calories = request.Calories;
        food.ProteinG = request.ProteinG;
        food.CarbohydratesG = request.CarbohydratesG;
        food.FatG = request.FatG;
        food.FibreG = request.FibreG;
        food.Allergens = request.Allergens;
        food.Tags = request.Tags;
        food.GroceryCategory = Parsers.ParseGrocery(request.GroceryCategory);
        food.SuitableFor = Parsers.ParseEnum<DietFlags>(request.SuitableFor, "Suitable for");
        food.ReferencePriceInr = request.ReferencePriceInr;
        food.ProviderNote = request.ProviderNote;
        food.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        food.Category = category;
        return MapFood(food, null);
    }

    public async Task<DietPlanSummaryDto> CreatePlanAsync(Guid actorId, CreateDietPlanRequest request, CancellationToken cancellationToken)
    {
        var plan = new DietPlan
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            DurationDays = request.DurationDays,
            IsPublished = false,
            CreatedByUserId = actorId,
            Disclaimer = ProductDisclaimer.Text
        };
        _db.DietPlans.Add(plan);
        await _db.SaveChangesAsync(cancellationToken);
        return new DietPlanSummaryDto(plan.Id, plan.Name, plan.Description, plan.DurationDays, plan.IsPublished, false);
    }

    public async Task<DietPlanDayDto> AddDayAsync(Guid planId, CreatePlanDayRequest request, CancellationToken cancellationToken)
    {
        var plan = await _db.DietPlans.FirstOrDefaultAsync(item => item.Id == planId, cancellationToken) ?? throw AppException.NotFound("Diet plan was not found.");
        if (request.DayNumber < 1 || request.DayNumber > plan.DurationDays)
        {
            throw new AppException("Day number is outside the plan length.");
        }

        if (await _db.DietPlanDays.AnyAsync(item => item.DietPlanId == planId && item.DayNumber == request.DayNumber, cancellationToken))
        {
            throw new AppException("That day already exists.");
        }

        var day = new DietPlanDay
        {
            DietPlanId = planId,
            DayNumber = request.DayNumber,
            Notes = request.Notes,
            WaterNote = request.WaterNote,
            ActivityNote = request.ActivityNote,
            SleepNote = request.SleepNote
        };
        _db.DietPlanDays.Add(day);
        await _db.SaveChangesAsync(cancellationToken);
        return new DietPlanDayDto(planId, day.DayNumber, null, day.Notes, day.WaterNote, day.ActivityNote, day.SleepNote, false, []);
    }

    public async Task<MealCardDto> AddMealAsync(Guid planId, int dayNumber, CreateMealRequest request, CancellationToken cancellationToken)
    {
        var day = await _db.DietPlanDays.FirstOrDefaultAsync(item => item.DietPlanId == planId && item.DayNumber == dayNumber, cancellationToken)
            ?? throw AppException.NotFound("That plan day was not found.");
        var meal = new Meal
        {
            DietPlanDayId = day.Id,
            MealType = Parsers.ParseEnum<MealType>(request.MealType, "Meal"),
            Title = request.Title.Trim(),
            ScheduledTime = Parsers.ParseTime(request.ScheduledTime, "Time"),
            Notes = request.Notes
        };
        _db.Meals.Add(meal);
        await _db.SaveChangesAsync(cancellationToken);
        return MealMapper.ToCard(meal, DietaryPreference.NonVegetarian, [], new Dictionary<Guid, decimal>(), null, false);
    }

    public async Task<MealCardDto> UpdateMealAsync(Guid mealId, UpdateMealRequest request, CancellationToken cancellationToken)
    {
        var meal = await _db.Meals.Include(item => item.Items).ThenInclude(item => item.Food).FirstOrDefaultAsync(item => item.Id == mealId, cancellationToken)
            ?? throw AppException.NotFound("Meal was not found.");
        meal.Title = request.Title.Trim();
        meal.Notes = request.Notes;
        meal.ScheduledTime = Parsers.ParseTime(request.ScheduledTime, "Time");
        _db.MealItems.RemoveRange(meal.Items.ToList());
        meal.Items.Clear();
        foreach (var item in request.Items)
        {
            if (item.FoodId is Guid foodId && !await _db.Foods.AnyAsync(food => food.Id == foodId, cancellationToken))
            {
                throw AppException.NotFound("A meal item references a missing food.");
            }

            var entity = new MealItem
            {
                MealId = meal.Id,
                FoodId = item.FoodId,
                DisplayName = item.DisplayName.Trim(),
                Quantity = item.Quantity,
                Unit = item.Unit.Trim(),
                SortOrder = item.SortOrder,
                IsOptional = item.IsOptional,
                AlternativeGroup = item.AlternativeGroup,
                IsDefaultAlternative = item.IsDefaultAlternative,
                Note = item.Note
            };
            _db.MealItems.Add(entity);
            meal.Items.Add(entity);
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _db.Entry(meal).Collection(item => item.Items).Query().Include(item => item.Food).LoadAsync(cancellationToken);
        return MealMapper.ToCard(meal, DietaryPreference.NonVegetarian, [], new Dictionary<Guid, decimal>(), null, false);
    }

    public async Task AssignPlanAsync(AssignPlanRequest request, CancellationToken cancellationToken)
    {
        var planExists = await _db.DietPlans.AnyAsync(item => item.Id == request.DietPlanId && item.IsPublished, cancellationToken);
        if (!planExists)
        {
            throw AppException.NotFound("Published diet plan was not found.");
        }

        var userExists = await _db.Users.AnyAsync(item => item.Id == request.UserId, cancellationToken);
        if (!userExists)
        {
            throw AppException.NotFound("User was not found.");
        }

        var active = await _db.PlanAssignments.Where(item => item.UserId == request.UserId && item.Status == PlanAssignmentStatus.Active).ToListAsync(cancellationToken);
        foreach (var item in active)
        {
            item.Status = PlanAssignmentStatus.Paused;
        }

        _db.PlanAssignments.Add(new UserPlanAssignment
        {
            UserId = request.UserId,
            DietPlanId = request.DietPlanId,
            StartDate = Parsers.ParseDateOrToday(request.StartDate, "Asia/Kolkata", DateTime.UtcNow),
            Status = PlanAssignmentStatus.Active
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task PublishAsync(Guid planId, bool published, CancellationToken cancellationToken)
    {
        var plan = await _db.DietPlans.FirstOrDefaultAsync(item => item.Id == planId, cancellationToken) ?? throw AppException.NotFound("Diet plan was not found.");
        plan.IsPublished = published;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<UserNotificationSettings> EnsureAsync(Guid userId, CancellationToken cancellationToken)
    {
        var settings = await _db.NotificationSettings.Include(item => item.Preferences).FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (settings is not null)
        {
            return settings;
        }

        settings = NotificationDefaults.Create(userId);
        _db.NotificationSettings.Add(settings);
        await _db.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private static NotificationSettingsResponse MapNotifications(UserNotificationSettings settings)
    {
        var schedule = NotificationScheduleBuilder.Build(NotificationDefaults.Inputs(settings), NotificationDefaults.Quiet(settings));
        return new NotificationSettingsResponse(
            settings.QuietHoursEnabled,
            Parsers.Clock(settings.QuietStart),
            Parsers.Clock(settings.QuietEnd),
            settings.Preferences.OrderBy(item => item.Category).Select(item => new NotificationPreferenceDto(
                item.Category.ToString(),
                item.Enabled,
                item.LocalTime is null ? null : Parsers.Clock(item.LocalTime.Value),
                item.IntervalMinutes,
                item.WindowStart is null ? null : Parsers.Clock(item.WindowStart.Value),
                item.WindowEnd is null ? null : Parsers.Clock(item.WindowEnd.Value))).ToList(),
            schedule.Select(item => new ReminderDto(item.Category, item.Title, item.Body, $"{item.Hour:00}:{item.Minute:00}", true)).ToList());
    }

    private static FoodDto MapFood(Food food, decimal? yourPrice) => new(
        food.Id,
        food.Name,
        food.LocalName,
        food.Category?.Name ?? string.Empty,
        Parsers.Grocery(food.GroceryCategory),
        food.ServingLabel,
        food.ServingQuantity,
        food.ServingUnit,
        food.Calories,
        food.ProteinG,
        food.CarbohydratesG,
        food.FatG,
        food.FibreG,
        food.Allergens,
        food.Tags,
        food.MicronutrientsNote,
        food.ProviderNote,
        food.ReferencePriceInr,
        yourPrice);

    private static EntitlementDto MapEntitlement(Entitlement entitlement) => new(
        entitlement.Plan.ToString(),
        entitlement.Status.ToString(),
        entitlement.StartsAtUtc,
        entitlement.EndsAtUtc,
        entitlement.Source,
        AccountFactory.FeaturesFor(entitlement.Plan));
}
