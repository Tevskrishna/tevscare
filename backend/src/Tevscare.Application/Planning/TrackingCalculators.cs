namespace Tevscare.Application.Planning;

public readonly record struct WaterProgress(int GoalMl, int ConsumedMl, int RemainingMl, int Percent, bool OutsideSuggestedRange);

public static class WaterCalculator
{
    public const int MinGoalMl = 1000;
    public const int MaxGoalMl = 4000;
    public const int SuggestionMinMl = 2000;
    public const int SuggestionMaxMl = 3500;

    public static int SuggestGoalMl(decimal weightKg)
    {
        if (weightKg <= 0)
        {
            return 2500;
        }

        var raw = (int)Math.Round(weightKg * 35m, MidpointRounding.AwayFromZero);
        return Math.Clamp(raw, SuggestionMinMl, SuggestionMaxMl);
    }

    public static WaterProgress Progress(int goalMl, int consumedMl)
    {
        var goal = goalMl <= 0 ? 2500 : goalMl;
        var consumed = Math.Max(0, consumedMl);
        var remaining = Math.Max(0, goal - consumed);
        var percent = (int)Math.Min(100, Math.Round(consumed * 100d / goal, MidpointRounding.AwayFromZero));
        return new WaterProgress(goal, consumed, remaining, percent, goal < SuggestionMinMl || goal > SuggestionMaxMl);
    }
}

public sealed record AdherenceResult(
    int Score,
    int MealsPlanned,
    decimal MealsCompletedWeight,
    bool WaterGoalMet,
    bool ActivityLogged,
    bool SleepLogged,
    bool WeightLogged);

public static class AdherenceCalculator
{
    public static AdherenceResult Calculate(
        int mealsPlanned,
        decimal mealsCompletedWeight,
        bool waterGoalMet,
        bool activityLogged,
        bool sleepLogged,
        bool weightLogged)
    {
        var fraction = mealsPlanned <= 0
            ? 0m
            : Math.Clamp(mealsCompletedWeight / mealsPlanned, 0m, 1m);

        var score = (int)Math.Round(
            fraction * 40m
            + (waterGoalMet ? 20 : 0)
            + (activityLogged ? 15 : 0)
            + (sleepLogged ? 15 : 0)
            + (weightLogged ? 10 : 0),
            MidpointRounding.AwayFromZero);

        return new AdherenceResult(
            Math.Clamp(score, 0, 100),
            mealsPlanned,
            mealsCompletedWeight,
            waterGoalMet,
            activityLogged,
            sleepLogged,
            weightLogged);
    }

    public static decimal StatusWeight(string status) => status switch
    {
        "Completed" => 1m,
        "Partial" => 0.5m,
        _ => 0m
    };
}

public sealed record WeightPoint(DateOnly Date, decimal WeightKg);

public static class WeightTrendCalculator
{
    public static decimal? Change(IReadOnlyList<WeightPoint> points, int days)
    {
        if (points.Count == 0)
        {
            return null;
        }

        var ordered = points.OrderBy(p => p.Date).ToList();
        var latest = ordered[^1];
        var cutoff = latest.Date.AddDays(-Math.Max(1, days));
        var earlier = ordered.LastOrDefault(p => p.Date <= cutoff) ?? ordered[0];
        return latest.WeightKg - earlier.WeightKg;
    }

    public static decimal? Average(IReadOnlyList<WeightPoint> points, int days)
    {
        if (points.Count == 0)
        {
            return null;
        }

        var latest = points.Max(point => point.Date);
        var cutoff = latest.AddDays(1 - Math.Max(1, days));
        var window = points.Where(point => point.Date >= cutoff && point.Date <= latest).ToList();
        return window.Count == 0 ? null : decimal.Round(window.Average(point => point.WeightKg), 2);
    }

    public static int? ProgressPercent(decimal? current, decimal? start, decimal? target)
    {
        if (current is null || start is null || target is null)
        {
            return null;
        }

        var total = start.Value - target.Value;
        if (total == 0)
        {
            return 100;
        }

        var done = (start.Value - current.Value) / total * 100m;
        return (int)Math.Clamp(Math.Round(done, MidpointRounding.AwayFromZero), 0, 100);
    }
}

public sealed record BudgetLine(string MealName, decimal EstimatedCost, decimal SpentCost);

public sealed record BudgetResult(
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
    IReadOnlyList<BudgetLine> Meals);

public static class BudgetCalculator
{
    public static BudgetResult Calculate(
        decimal dailyBudget,
        string currency,
        IReadOnlyList<(string Meal, decimal? Planned, decimal Spent)> meals,
        int periodDays)
    {
        var safePeriod = periodDays <= 0 ? 15 : periodDays;
        decimal planned = 0;
        decimal spent = 0;
        var missing = false;
        var lines = new List<BudgetLine>(meals.Count);
        foreach (var meal in meals)
        {
            if (meal.Planned is null)
            {
                missing = true;
            }

            var plannedCost = meal.Planned ?? 0;
            planned += plannedCost;
            spent += meal.Spent;
            lines.Add(new BudgetLine(meal.Meal, plannedCost, meal.Spent));
        }

        return new BudgetResult(
            dailyBudget,
            string.IsNullOrWhiteSpace(currency) ? "INR" : currency,
            decimal.Round(spent, 2),
            decimal.Round(dailyBudget - spent, 2),
            decimal.Round(planned, 2),
            decimal.Round(planned, 2),
            decimal.Round(planned * safePeriod, 2),
            decimal.Round(planned * 30, 2),
            safePeriod,
            missing,
            lines);
    }
}

public sealed record ShoppingInputLine(
    Guid FoodId,
    string Name,
    string Category,
    decimal Quantity,
    string Unit,
    decimal? UnitPrice);

public sealed record ShoppingAdjustment(decimal? Quantity, decimal? ActualUnitPrice, string? Notes);

public sealed record ShoppingResultLine(
    Guid FoodId,
    string Name,
    string Category,
    decimal Quantity,
    string Unit,
    decimal? EstimatedCost,
    bool Purchased,
    decimal PlannedQuantity = 0,
    decimal? ActualUnitPrice = null,
    decimal? ActualCost = null,
    string? Notes = null);

public static class ShoppingListCalculator
{
    public static (IReadOnlyList<ShoppingResultLine> Lines, decimal TotalKnownCost, bool HasMissingPrices, decimal ActualKnownCost) Build(
        IEnumerable<ShoppingInputLine> items,
        IReadOnlyDictionary<Guid, bool> purchased,
        IReadOnlyDictionary<Guid, ShoppingAdjustment>? adjustments = null)
    {
        var lines = items
            .GroupBy(i => (i.FoodId, i.Name, i.Category, i.Unit))
            .Select(g =>
            {
                var planned = decimal.Round(g.Sum(x => x.Quantity), 2);
                ShoppingAdjustment? adjustment = null;
                if (adjustments is not null && adjustments.TryGetValue(g.Key.FoodId, out var found))
                {
                    adjustment = found;
                }

                var quantity = adjustment?.Quantity is > 0 ? decimal.Round(adjustment.Quantity.Value, 2) : planned;
                var price = g.Select(x => x.UnitPrice).FirstOrDefault(p => p is not null);
                decimal? estimated = price is null ? null : decimal.Round(planned * price.Value, 2);
                decimal? actual = adjustment?.ActualUnitPrice is decimal actualPrice
                    ? decimal.Round(quantity * actualPrice, 2)
                    : null;
                purchased.TryGetValue(g.Key.FoodId, out var isPurchased);
                return new ShoppingResultLine(
                    g.Key.FoodId,
                    g.Key.Name,
                    g.Key.Category,
                    quantity,
                    g.Key.Unit,
                    estimated,
                    isPurchased,
                    planned,
                    adjustment?.ActualUnitPrice,
                    actual,
                    adjustment?.Notes);
            })
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Name)
            .ToList();

        var missing = lines.Any(x => x.EstimatedCost is null);
        var total = lines.Sum(x => x.EstimatedCost ?? 0);
        var actualTotal = lines.Sum(x => x.ActualCost ?? 0);
        return (lines, decimal.Round(total, 2), missing, decimal.Round(actualTotal, 2));
    }
}

public sealed record QuietHours(bool Enabled, TimeOnly Start, TimeOnly End);

public sealed record NotificationPreferenceInput(
    string Category,
    bool Enabled,
    TimeOnly? LocalTime,
    int? IntervalMinutes,
    TimeOnly? WindowStart,
    TimeOnly? WindowEnd);

public sealed record ScheduledNotification(string Id, string Category, string Title, string Body, int Hour, int Minute);

public static class NotificationScheduleBuilder
{
    public static IReadOnlyList<ScheduledNotification> Build(
        IReadOnlyList<NotificationPreferenceInput> preferences,
        QuietHours quiet)
    {
        var list = new List<ScheduledNotification>();
        foreach (var preference in preferences.Where(p => p.Enabled))
        {
            if (string.Equals(preference.Category, "Water", StringComparison.OrdinalIgnoreCase)
                && preference.IntervalMinutes is > 0 and <= 240)
            {
                var start = preference.WindowStart ?? new TimeOnly(8, 0);
                var end = preference.WindowEnd ?? new TimeOnly(20, 0);
                var cursor = start;
                var guard = 0;
                while (cursor <= end && guard < 24)
                {
                    AddIfAudible(list, preference.Category, cursor, quiet);
                    cursor = cursor.AddMinutes(preference.IntervalMinutes.Value);
                    guard++;
                }

                continue;
            }

            if (preference.LocalTime is TimeOnly time)
            {
                AddIfAudible(list, preference.Category, time, quiet);
            }
        }

        return list;
    }

    public static bool IsQuiet(TimeOnly time, QuietHours quiet)
    {
        if (!quiet.Enabled)
        {
            return false;
        }

        if (quiet.Start == quiet.End)
        {
            return false;
        }

        if (quiet.Start < quiet.End)
        {
            return time >= quiet.Start && time < quiet.End;
        }

        return time >= quiet.Start || time < quiet.End;
    }

    public static string TitleFor(string category) => category switch
    {
        "MorningWeight" => "Morning weight",
        "Breakfast" => "Breakfast",
        "Water" => "Water",
        "Lunch" => "Lunch",
        "Activity" => "Activity",
        "AfternoonSnack" => "Afternoon snack",
        "Dinner" => "Dinner",
        "Sleep" => "Sleep",
        "CheckIn" => "Check-in",
        _ => "TEVSCARE"
    };

    public static string BodyFor(string category) => category switch
    {
        "MorningWeight" => "Good morning. Log your morning weight.",
        "Breakfast" => "Breakfast time — check today's meal plan.",
        "Water" => "Time for your next glass of water.",
        "Lunch" => "Check your lunch plan.",
        "Activity" => "Time for your daily walk.",
        "AfternoonSnack" => "Check the afternoon snack on today's plan.",
        "Dinner" => "Your dinner window is approaching.",
        "Sleep" => "Start winding down for the night.",
        "CheckIn" => "A short check-in if you want one.",
        _ => "Open TEVSCARE to check today's plan."
    };

    private static void AddIfAudible(List<ScheduledNotification> list, string category, TimeOnly time, QuietHours quiet)
    {
        if (IsQuiet(time, quiet))
        {
            return;
        }

        list.Add(new ScheduledNotification(
            $"{category}-{time.Hour:00}{time.Minute:00}",
            category,
            TitleFor(category),
            BodyFor(category),
            time.Hour,
            time.Minute));
    }
}

public static class TimezoneClock
{
    public const string DefaultTimezone = "Asia/Kolkata";

    public static TimeZoneInfo Resolve(string? timeZoneId)
    {
        var id = string.IsNullOrWhiteSpace(timeZoneId) ? DefaultTimezone : timeZoneId.Trim();
        if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone))
        {
            return zone;
        }

        try
        {
            if (TimeZoneInfo.TryConvertIanaIdToWindowsId(id, out var windowsId)
                && TimeZoneInfo.TryFindSystemTimeZoneById(windowsId, out zone))
            {
                return zone;
            }
        }
        catch (InvalidTimeZoneException)
        {
            // Fall through to the default zone.
        }

        return TimeZoneInfo.FindSystemTimeZoneById(DefaultTimezone);
    }

    public static DateOnly LocalToday(string? timeZoneId, DateTime utcNow)
    {
        var utc = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, Resolve(timeZoneId));
        return DateOnly.FromDateTime(local);
    }

    public static string Greeting(DateTime utcNow, string? timeZoneId)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), Resolve(timeZoneId));
        return local.Hour switch
        {
            < 12 => "Good morning",
            < 17 => "Good afternoon",
            _ => "Good evening"
        };
    }
}
