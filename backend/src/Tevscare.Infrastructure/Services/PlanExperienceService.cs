using System.Globalization;
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
using Tevscare.Domain.Tracking;
using Tevscare.Infrastructure.Identity;
using Tevscare.Infrastructure.Persistence;

namespace Tevscare.Infrastructure.Services;

public class PlanExperienceService : IDashboardService, IDietPlanService, IMealLogService, IBudgetService, IShoppingListService
{
    private readonly AppDbContext _db;

    public PlanExperienceService(AppDbContext db) => _db = db;

    public async Task<DashboardResponse> GetAsync(Guid userId, string? date, CancellationToken cancellationToken)
    {
        var loaded = await LoadAsync(userId, date, cancellationToken);
        var cards = Cards(loaded, false);
        var active = loaded.Day is not null && !loaded.State.Completed;
        var visibleCards = active ? cards : [];
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(loaded.UtcNow, DateTimeKind.Utc), TimezoneClock.Resolve(loaded.User.Timezone));
        var nowTime = TimeOnly.FromDateTime(localNow);
        MealCardDto? next = null;
        if (active)
        {
            var open = visibleCards.Where(card => card.LogStatus is not "Completed").ToList();
            next = open.FirstOrDefault(card => TimeOnly.Parse(card.ScheduledTime) >= nowTime) ?? open.FirstOrDefault();
        }

        var adherence = await Adherence(loaded, visibleCards.Count, cancellationToken);
        var settings = await EnsureNotificationsAsync(userId, cancellationToken);
        var schedule = NotificationScheduleBuilder.Build(NotificationDefaults.Inputs(settings), NotificationDefaults.Quiet(settings));
        var reminders = schedule
            .Where(item => item.Hour > nowTime.Hour || (item.Hour == nowTime.Hour && item.Minute >= nowTime.Minute))
            .Take(4)
            .Select(item => new ReminderDto(item.Category, item.Title, item.Body, $"{item.Hour:00}:{item.Minute:00}", true))
            .ToList();

        var latestWeight = await _db.WeightEntries.AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.LocalDate)
            .FirstOrDefaultAsync(cancellationToken);
        var current = latestWeight?.WeightKg ?? loaded.Profile.CurrentWeightKg;
        var water = await WaterSum(userId, loaded.Date, cancellationToken);
        var waterProgress = WaterCalculator.Progress(loaded.Profile.WaterGoalMl, water);
        var activity = await _db.ActivityLogs.AsNoTracking().FirstOrDefaultAsync(item => item.UserId == userId && item.LocalDate == loaded.Date, cancellationToken);
        var sleep = await _db.SleepLogs.AsNoTracking().FirstOrDefaultAsync(item => item.UserId == userId && item.LocalDate == loaded.Date, cancellationToken);

        return new DashboardResponse(
            TimezoneClock.Greeting(loaded.UtcNow, loaded.User.Timezone),
            loaded.Date.ToString("yyyy-MM-dd"),
            loaded.Date.ToString("dddd, d MMMM", CultureInfo.InvariantCulture),
            loaded.User.Timezone,
            loaded.Plan?.Name,
            loaded.Plan?.Id,
            loaded.Plan is null ? null : loaded.State.DayNumber,
            loaded.Plan?.DurationDays ?? 15,
            loaded.State.NotStarted,
            loaded.State.Completed,
            new WeightSummaryDto(
                current,
                loaded.Profile.TargetWeightKg,
                current is null || loaded.Profile.StartingWeightKg is null ? null : current - loaded.Profile.StartingWeightKg,
                WeightTrendCalculator.ProgressPercent(current, loaded.Profile.StartingWeightKg, loaded.Profile.TargetWeightKg)),
            new WaterSummaryDto(waterProgress.GoalMl, waterProgress.ConsumedMl, waterProgress.RemainingMl, waterProgress.Percent),
            new ActivitySummaryDto(loaded.Profile.ActivityGoalMinutes, activity?.DurationMinutes ?? 0, activity?.Completed == true),
            new SleepSummaryDto(loaded.Profile.SleepGoalMinutes, sleep?.DurationMinutes, sleep is not null),
            new AdherenceSummaryDto(adherence.Score, MealMapper.AdherenceExplanation),
            next,
            visibleCards,
            reminders,
            ProductDisclaimer.Text);
    }

    public async Task<IReadOnlyList<DietPlanSummaryDto>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        var assigned = await _db.PlanAssignments.AsNoTracking()
            .Where(item => item.UserId == userId)
            .Select(item => item.DietPlanId)
            .ToListAsync(cancellationToken);
        var plans = await _db.DietPlans.AsNoTracking()
            .Where(item => item.IsPublished || assigned.Contains(item.Id))
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
        return plans.Select(plan => new DietPlanSummaryDto(plan.Id, plan.Name, plan.Description, plan.DurationDays, plan.IsPublished, assigned.Contains(plan.Id))).ToList();
    }

    public async Task<DietPlanDetailDto> GetAsync(Guid userId, Guid planId, CancellationToken cancellationToken)
    {
        var plan = await _db.DietPlans.AsNoTracking().FirstOrDefaultAsync(item => item.Id == planId, cancellationToken)
            ?? throw AppException.NotFound("Diet plan was not found.");
        var assignment = await _db.PlanAssignments.AsNoTracking()
            .Where(item => item.UserId == userId && item.DietPlanId == planId)
            .OrderByDescending(item => item.StartDate)
            .FirstOrDefaultAsync(cancellationToken);
        var (user, _) = await UserLoader.RequireAsync(_db, userId, cancellationToken);
        var today = TimezoneClock.LocalToday(user.Timezone, DateTime.UtcNow);
        var state = assignment is null ? new PlanDayState(1, true, false) : PlanDayResolver.Resolve(assignment.StartDate, today, plan.DurationDays);
        var days = await _db.DietPlanDays.AsNoTracking()
            .Where(item => item.DietPlanId == planId)
            .OrderBy(item => item.DayNumber)
            .Select(item => new DietPlanDaySummaryDto(item.DayNumber, item.Notes, item.Meals.Count, assignment != null && !state.Completed && item.DayNumber == state.DayNumber, null))
            .ToListAsync(cancellationToken);

        return new DietPlanDetailDto(plan.Id, plan.Name, plan.Description, plan.DurationDays, plan.IsPublished, assignment?.StartDate, assignment is null ? null : state.DayNumber, state.Completed, string.IsNullOrWhiteSpace(plan.Disclaimer) ? ProductDisclaimer.Text : plan.Disclaimer, days);
    }

    public async Task<DietPlanDayDto> GetDayAsync(Guid userId, Guid planId, int day, CancellationToken cancellationToken)
    {
        var loaded = await LoadAsync(userId, null, cancellationToken);
        var planDay = await _db.DietPlanDays
            .Include(item => item.Meals).ThenInclude(meal => meal.Items).ThenInclude(item => item.Food)
            .Include(item => item.DietPlan)
            .FirstOrDefaultAsync(item => item.DietPlanId == planId && item.DayNumber == day, cancellationToken)
            ?? throw AppException.NotFound("That plan day was not found.");

        var logs = await _db.MealLogs.AsNoTracking().Where(item => item.UserId == userId && item.LocalDate == loaded.Date).ToListAsync(cancellationToken);
        var useLogs = loaded.Assignment?.DietPlanId == planId && loaded.State.DayNumber == day && !loaded.State.Completed;
        var cards = planDay.Meals.OrderBy(meal => meal.ScheduledTime).Select(meal =>
        {
            var log = useLogs ? logs.FirstOrDefault(item => item.MealType == meal.MealType) : null;
            return MealMapper.ToCard(meal, loaded.Profile.DietaryPreference, AllergyNames(loaded.Profile), loaded.Prices, log?.Status.ToString());
        }).ToList();

        DateOnly? date = null;
        if (loaded.Assignment?.DietPlanId == planId)
        {
            date = loaded.Assignment.StartDate.AddDays(day - 1);
        }

        return new DietPlanDayDto(planId, day, date, planDay.Notes, planDay.WaterNote, planDay.ActivityNote, planDay.SleepNote, useLogs, cards);
    }

    public async Task<IReadOnlyList<MealCardDto>> GetTodayMealsAsync(Guid userId, string? date, CancellationToken cancellationToken)
    {
        var loaded = await LoadAsync(userId, date, cancellationToken);
        if (loaded.Day is null || loaded.State.Completed)
        {
            return [];
        }

        return Cards(loaded, false);
    }

    public async Task<MealLogDto> LogAsync(Guid userId, LogMealRequest request, CancellationToken cancellationToken)
    {
        var loaded = await LoadAsync(userId, request.LocalDate, cancellationToken);
        var mealType = Parsers.ParseEnum<MealType>(request.MealType, "Meal");
        var status = Parsers.ParseEnum<MealLogStatus>(request.Status, "Status");
        var existing = await _db.MealLogs.Include(item => item.Items)
            .FirstOrDefaultAsync(item => item.UserId == userId && item.LocalDate == loaded.Date && item.MealType == mealType, cancellationToken);

        if (existing is null)
        {
            existing = new MealLog { UserId = userId, LocalDate = loaded.Date, MealType = mealType };
            _db.MealLogs.Add(existing);
        }
        else
        {
            _db.MealLogItems.RemoveRange(existing.Items);
            existing.Items.Clear();
        }

        existing.Status = status;
        existing.Notes = request.Notes?.Trim();
        existing.PlannedMealId = request.MealId;
        foreach (var item in request.Items ?? [])
        {
            var logged = new MealLogItem
            {
                MealLog = existing,
                FoodId = item.FoodId,
                Name = item.Name.Trim(),
                Quantity = item.Quantity,
                Unit = item.Unit.Trim()
            };
            _db.MealLogItems.Add(logged);
            existing.Items.Add(logged);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ToLog(existing);
    }

    public async Task<IReadOnlyList<MealLogDto>> GetByDateAsync(Guid userId, string? date, CancellationToken cancellationToken)
    {
        var loaded = await LoadAsync(userId, date, cancellationToken);
        var logs = await _db.MealLogs.AsNoTracking().Include(item => item.Items)
            .Where(item => item.UserId == userId && item.LocalDate == loaded.Date)
            .ToListAsync(cancellationToken);
        return logs.Select(ToLog).ToList();
    }

    Task<BudgetResponse> IBudgetService.GetAsync(Guid userId, string? date, CancellationToken cancellationToken) =>
        GetBudgetAsync(userId, date, cancellationToken);

    public async Task<BudgetResponse> GetBudgetAsync(Guid userId, string? date, CancellationToken cancellationToken)
    {
        var loaded = await LoadAsync(userId, date, cancellationToken);
        return await Budget(loaded, cancellationToken);
    }

    public async Task<BudgetResponse> UpsertAsync(Guid userId, UpsertBudgetRequest request, CancellationToken cancellationToken)
    {
        var budget = await _db.Budgets.FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (budget is null)
        {
            budget = new BudgetSettings { UserId = userId };
            _db.Budgets.Add(budget);
        }

        budget.DailyBudgetAmount = request.DailyBudgetAmount;
        budget.Currency = request.Currency.Trim().ToUpperInvariant();
        await _db.SaveChangesAsync(cancellationToken);
        return await GetBudgetAsync(userId, null, cancellationToken);
    }

    public async Task SetFoodPriceAsync(Guid userId, UpsertFoodPriceRequest request, CancellationToken cancellationToken)
    {
        var foodExists = await _db.Foods.AnyAsync(item => item.Id == request.FoodId, cancellationToken);
        if (!foodExists)
        {
            throw AppException.NotFound("Food was not found.");
        }

        var price = await _db.FoodPrices.FirstOrDefaultAsync(item => item.UserId == userId && item.FoodId == request.FoodId, cancellationToken);
        if (price is null)
        {
            price = new UserFoodPrice { UserId = userId, FoodId = request.FoodId, Currency = "INR" };
            _db.FoodPrices.Add(price);
        }

        price.PricePerServing = request.PricePerServing;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ShoppingListResponse> GetAsync(Guid userId, int days, string? startDate, CancellationToken cancellationToken)
    {
        if (days is not 1 and not 7 and not 15 and not 30)
        {
            throw new AppException("Shopping duration must be 1, 7, 15 or 30 days.");
        }

        var loaded = await LoadAsync(userId, startDate, cancellationToken);
        var lines = new List<ShoppingInputLine>();
        if (loaded.Plan is not null && loaded.Assignment is not null)
        {
            var planDays = await _db.DietPlanDays
                .Include(item => item.Meals).ThenInclude(meal => meal.Items).ThenInclude(item => item.Food)
                .Where(item => item.DietPlanId == loaded.Plan.Id)
                .ToListAsync(cancellationToken);
            var byNumber = planDays.ToDictionary(item => item.DayNumber);
            for (var offset = 0; offset < days; offset++)
            {
                var date = loaded.Date.AddDays(offset);
                var state = PlanDayResolver.Resolve(loaded.Assignment.StartDate, date, loaded.Plan.DurationDays);
                if (state.NotStarted || state.Completed || !byNumber.TryGetValue(state.DayNumber, out var planDay))
                {
                    continue;
                }

                if (date < loaded.Assignment.StartDate || date > loaded.Assignment.StartDate.AddDays(loaded.Plan.DurationDays - 1))
                {
                    continue;
                }

                foreach (var meal in planDay.Meals)
                {
                    foreach (var item in MealMapper.ShoppingItems(meal, loaded.Profile.DietaryPreference, AllergyNames(loaded.Profile), loaded.Prices))
                    {
                        lines.Add(new ShoppingInputLine(item.FoodId!.Value, item.Name, item.GroceryCategory, item.Quantity, item.Unit, item.UnitPrice));
                    }
                }
            }
        }

        var purchasedRows = await _db.ShoppingStates.AsNoTracking()
            .Where(item => item.UserId == userId && item.RangeStart == loaded.Date && item.RangeDays == days)
            .ToListAsync(cancellationToken);
        var purchased = purchasedRows.ToDictionary(item => item.FoodId, item => item.Purchased);
        var adjustments = purchasedRows.ToDictionary(
            item => item.FoodId,
            item => new ShoppingAdjustment(item.QuantityOverride, item.ActualUnitPrice, item.Notes));
        var built = ShoppingListCalculator.Build(lines, purchased, adjustments);
        var currency = loaded.Budget?.Currency ?? "INR";
        return new ShoppingListResponse(
            loaded.Date.ToString("yyyy-MM-dd"),
            days,
            built.TotalKnownCost,
            built.HasMissingPrices,
            currency,
            built.Lines.Select(line => new ShoppingLineDto(line.FoodId, line.Name, line.Category, line.Quantity, line.Unit, line.EstimatedCost, line.Purchased, line.PlannedQuantity, line.ActualUnitPrice, line.ActualCost, line.Notes)).ToList(),
            built.ActualKnownCost);
    }

    public async Task<ShoppingListResponse> ToggleAsync(Guid userId, ShoppingToggleRequest request, CancellationToken cancellationToken)
    {
        var start = Parsers.ParseDateOrToday(request.StartDate, (await UserLoader.RequireAsync(_db, userId, cancellationToken)).User.Timezone, DateTime.UtcNow);
        var state = await _db.ShoppingStates.FirstOrDefaultAsync(
            item => item.UserId == userId && item.FoodId == request.FoodId && item.RangeStart == start && item.RangeDays == request.RangeDays,
            cancellationToken);
        if (state is null)
        {
            state = new ShoppingItemState { UserId = userId, FoodId = request.FoodId, RangeStart = start, RangeDays = request.RangeDays };
            _db.ShoppingStates.Add(state);
        }

        state.Purchased = request.Purchased;
        if (request.UpdateDetails)
        {
            state.QuantityOverride = request.Quantity;
            state.ActualUnitPrice = request.ActualUnitPrice;
            state.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await GetAsync(userId, request.RangeDays, start.ToString("yyyy-MM-dd"), cancellationToken);
    }

    private async Task<BudgetResponse> Budget(LoadedDay loaded, CancellationToken cancellationToken)
    {
        var cards = loaded.Day is null || loaded.State.Completed ? [] : Cards(loaded, true);
        var logs = await _db.MealLogs.AsNoTracking().Include(item => item.Items)
            .Where(item => item.UserId == loaded.User.Id && item.LocalDate == loaded.Date)
            .ToListAsync(cancellationToken);
        var rows = new List<(string Meal, decimal? Planned, decimal Spent)>();
        foreach (var card in cards)
        {
            var log = logs.FirstOrDefault(item => item.MealType.ToString() == card.MealType);
            rows.Add((card.MealType, card.HasMissingPrices ? null : card.EstimatedCost, Spent(card, log, loaded.Prices)));
        }

        var budget = loaded.Budget?.DailyBudgetAmount ?? 675;
        var currency = loaded.Budget?.Currency ?? "INR";
        var result = BudgetCalculator.Calculate(budget, currency, rows, loaded.Plan?.DurationDays ?? 15);
        return new BudgetResponse(result.DailyBudget, result.Currency, result.Spent, result.Remaining, result.PlannedToday, result.ProjectedDaily, result.ProjectedPeriod, result.ProjectedMonthly, result.PeriodDays, result.HasMissingPrices, MealMapper.PriceNote, result.Meals.Select(item => new BudgetMealDto(item.MealName, item.EstimatedCost, item.SpentCost)).ToList());
    }

    private static decimal Spent(MealCardDto card, MealLog? log, IReadOnlyDictionary<Guid, decimal> prices)
    {
        if (log is null || log.Status == MealLogStatus.Skipped)
        {
            return 0;
        }

        if (log.Items.Count > 0)
        {
            decimal sum = 0;
            foreach (var item in log.Items)
            {
                if (item.FoodId is Guid foodId && prices.TryGetValue(foodId, out var price))
                {
                    sum += item.Quantity * price;
                }
            }

            if (sum > 0)
            {
                return decimal.Round(sum, 2);
            }
        }

        var planned = card.EstimatedCost;
        return log.Status == MealLogStatus.Partial ? decimal.Round(planned / 2m, 2) : planned;
    }

    private List<MealCardDto> Cards(LoadedDay loaded, bool includeCompletedDays)
    {
        if (loaded.Day is null || (!includeCompletedDays && loaded.State.Completed))
        {
            return [];
        }

        return loaded.Day.Meals
            .OrderBy(meal => meal.ScheduledTime)
            .Select(meal =>
            {
                loaded.Logs.TryGetValue(meal.MealType, out var log);
                return MealMapper.ToCard(meal, loaded.Profile.DietaryPreference, AllergyNames(loaded.Profile), loaded.Prices, log?.Status.ToString());
            })
            .ToList();
    }

    private async Task<AdherenceResult> Adherence(LoadedDay loaded, int mealsPlanned, CancellationToken cancellationToken)
    {
        var logs = loaded.Logs.Values.ToList();
        var completed = logs.Sum(log => AdherenceCalculator.StatusWeight(log.Status.ToString()));
        var water = await WaterSum(loaded.User.Id, loaded.Date, cancellationToken);
        var activity = await _db.ActivityLogs.AsNoTracking().FirstOrDefaultAsync(item => item.UserId == loaded.User.Id && item.LocalDate == loaded.Date, cancellationToken);
        var sleep = await _db.SleepLogs.AnyAsync(item => item.UserId == loaded.User.Id && item.LocalDate == loaded.Date, cancellationToken);
        var weight = await _db.WeightEntries.AnyAsync(item => item.UserId == loaded.User.Id && item.LocalDate == loaded.Date, cancellationToken);
        var activityLogged = activity is not null && (activity.Completed || activity.DurationMinutes >= loaded.Profile.ActivityGoalMinutes);
        var result = AdherenceCalculator.Calculate(mealsPlanned, completed, water >= loaded.Profile.WaterGoalMl, activityLogged, sleep, weight);
        var snapshot = await _db.AdherenceSnapshots.FirstOrDefaultAsync(item => item.UserId == loaded.User.Id && item.LocalDate == loaded.Date, cancellationToken);
        if (snapshot is null)
        {
            snapshot = new AdherenceSnapshot { UserId = loaded.User.Id, LocalDate = loaded.Date };
            _db.AdherenceSnapshots.Add(snapshot);
        }

        snapshot.Score = result.Score;
        snapshot.MealsPlanned = result.MealsPlanned;
        snapshot.MealsCompletedWeight = result.MealsCompletedWeight;
        snapshot.WaterGoalMet = result.WaterGoalMet;
        snapshot.ActivityLogged = result.ActivityLogged;
        snapshot.SleepLogged = result.SleepLogged;
        snapshot.WeightLogged = result.WeightLogged;
        await _db.SaveChangesAsync(cancellationToken);
        return result;
    }

    private async Task<int> WaterSum(Guid userId, DateOnly date, CancellationToken cancellationToken) =>
        await _db.WaterEntries.Where(item => item.UserId == userId && item.LocalDate == date).SumAsync(item => (int?)item.AmountMl, cancellationToken) ?? 0;

    private async Task<UserNotificationSettings> EnsureNotificationsAsync(Guid userId, CancellationToken cancellationToken)
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

    private async Task<LoadedDay> LoadAsync(Guid userId, string? date, CancellationToken cancellationToken)
    {
        var (user, profile) = await UserLoader.RequireAsync(_db, userId, cancellationToken);
        var utcNow = DateTime.UtcNow;
        var localDate = Parsers.ParseDateOrToday(date, user.Timezone, utcNow);
        var prices = await _db.Foods.AsNoTracking()
            .Where(item => item.ReferencePriceInr != null)
            .ToDictionaryAsync(item => item.Id, item => item.ReferencePriceInr!.Value, cancellationToken);
        var customPrices = await _db.FoodPrices.AsNoTracking().Where(item => item.UserId == userId).ToListAsync(cancellationToken);
        foreach (var custom in customPrices)
        {
            prices[custom.FoodId] = custom.PricePerServing;
        }
        var budget = await _db.Budgets.AsNoTracking().FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        var assignment = await _db.PlanAssignments.Include(item => item.DietPlan)
            .Where(item => item.UserId == userId && item.Status == PlanAssignmentStatus.Active)
            .OrderByDescending(item => item.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        DietPlanDay? day = null;
        var state = new PlanDayState(1, true, false);
        if (assignment is not null)
        {
            state = PlanDayResolver.Resolve(assignment.StartDate, localDate, assignment.DietPlan.DurationDays);
            day = await _db.DietPlanDays
                .Include(item => item.Meals).ThenInclude(meal => meal.Items).ThenInclude(item => item.Food)
                .FirstOrDefaultAsync(item => item.DietPlanId == assignment.DietPlanId && item.DayNumber == state.DayNumber, cancellationToken);
        }

        var logs = await _db.MealLogs.AsNoTracking()
            .Where(item => item.UserId == userId && item.LocalDate == localDate)
            .ToDictionaryAsync(item => item.MealType, cancellationToken);

        return new LoadedDay(user, profile, utcNow, localDate, prices, budget, assignment, assignment?.DietPlan, day, state, logs);
    }

    private static List<string> AllergyNames(UserProfile profile) => profile.Allergies.Select(item => item.Name).ToList();

    private static MealLogDto ToLog(MealLog log) => new(
        log.Id,
        log.LocalDate.ToString("yyyy-MM-dd"),
        log.MealType.ToString(),
        log.Status.ToString(),
        log.Notes,
        log.Items.Select(item => new LogMealItemRequest(item.FoodId, item.Name, item.Quantity, item.Unit)).ToList());

    private sealed record LoadedDay(
        ApplicationUser User,
        UserProfile Profile,
        DateTime UtcNow,
        DateOnly Date,
        Dictionary<Guid, decimal> Prices,
        BudgetSettings? Budget,
        UserPlanAssignment? Assignment,
        DietPlan? Plan,
        DietPlanDay? Day,
        PlanDayState State,
        Dictionary<MealType, MealLog> Logs);
}
