using Microsoft.EntityFrameworkCore;
using Tevscare.Application.Abstractions;
using Tevscare.Application.Common;
using Tevscare.Application.Contracts;
using Tevscare.Application.Planning;
using Tevscare.Domain.Enums;
using Tevscare.Domain.Tracking;
using Tevscare.Infrastructure.Persistence;

namespace Tevscare.Infrastructure.Services;

public class TrackingExperienceService : IWaterService, IWeightService, IActivityService, ISleepService, ICheckInService, IProgressService
{
    private readonly AppDbContext _db;

    public TrackingExperienceService(AppDbContext db) => _db = db;

    public Task<WaterDayResponse> GetTodayAsync(Guid userId, string? date, CancellationToken cancellationToken) =>
        WaterDay(userId, date, cancellationToken);

    public async Task<WaterDayResponse> AddAsync(Guid userId, LogWaterRequest request, CancellationToken cancellationToken)
    {
        var (user, date) = await Clock(userId, request.LocalDate, cancellationToken);
        _db.WaterEntries.Add(new WaterEntry
        {
            UserId = user.Id,
            LocalDate = date,
            LoggedAtUtc = DateTime.UtcNow,
            AmountMl = request.AmountMl
        });
        await _db.SaveChangesAsync(cancellationToken);
        return await WaterDay(userId, date.ToString("yyyy-MM-dd"), cancellationToken);
    }

    public async Task<WaterDayResponse> DeleteAsync(Guid userId, Guid entryId, CancellationToken cancellationToken)
    {
        var entry = await _db.WaterEntries.FirstOrDefaultAsync(item => item.Id == entryId && item.UserId == userId, cancellationToken)
            ?? throw AppException.NotFound("Water entry was not found.");
        var date = entry.LocalDate;
        _db.WaterEntries.Remove(entry);
        await _db.SaveChangesAsync(cancellationToken);
        return await WaterDay(userId, date.ToString("yyyy-MM-dd"), cancellationToken);
    }

    public async Task<WeightHistoryResponse> GetAsync(Guid userId, int days, CancellationToken cancellationToken)
    {
        var (_, profile) = await UserLoader.RequireAsync(_db, userId, cancellationToken);
        var take = Math.Clamp(days, 7, 365);
        var entries = await _db.WeightEntries.AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.LocalDate)
            .Take(take)
            .ToListAsync(cancellationToken);
        entries.Reverse();
        var points = entries.Select(item => new WeightPoint(item.LocalDate, item.WeightKg)).ToList();
        var current = entries.LastOrDefault()?.WeightKg ?? profile.CurrentWeightKg;
        return new WeightHistoryResponse(
            current,
            profile.TargetWeightKg,
            WeightTrendCalculator.Change(points, 7),
            WeightTrendCalculator.Change(points, 30),
            WeightTrendCalculator.ProgressPercent(current, profile.StartingWeightKg, profile.TargetWeightKg),
            entries.Select(item => new WeightEntryDto(item.Id, item.LocalDate.ToString("yyyy-MM-dd"), item.WeightKg, item.IsMorning, item.Note)).ToList());
    }

    public async Task<WeightEntryDto> LogAsync(Guid userId, LogWeightRequest request, CancellationToken cancellationToken)
    {
        var (_, profile) = await UserLoader.RequireAsync(_db, userId, cancellationToken);
        var date = Parsers.ParseDateOrToday(request.LocalDate, (await _db.Users.Where(item => item.Id == userId).Select(item => item.Timezone).FirstAsync(cancellationToken)), DateTime.UtcNow);
        var entry = await _db.WeightEntries.FirstOrDefaultAsync(item => item.UserId == userId && item.LocalDate == date, cancellationToken);
        if (entry is null)
        {
            entry = new WeightEntry { UserId = userId, LocalDate = date };
            _db.WeightEntries.Add(entry);
        }

        entry.WeightKg = request.WeightKg;
        entry.IsMorning = request.IsMorning;
        entry.Note = request.Note?.Trim();
        entry.LoggedAtUtc = DateTime.UtcNow;
        profile.CurrentWeightKg = request.WeightKg;
        profile.StartingWeightKg ??= request.WeightKg;
        await _db.SaveChangesAsync(cancellationToken);
        return new WeightEntryDto(entry.Id, date.ToString("yyyy-MM-dd"), entry.WeightKg, entry.IsMorning, entry.Note);
    }

    Task<ActivityDto?> IActivityService.GetAsync(Guid userId, string? date, CancellationToken cancellationToken) =>
        GetActivityAsync(userId, date, cancellationToken);

    public async Task<ActivityDto?> GetActivityAsync(Guid userId, string? date, CancellationToken cancellationToken)
    {
        var (user, profile) = await UserLoader.RequireAsync(_db, userId, cancellationToken);
        var local = Parsers.ParseDateOrToday(date, user.Timezone, DateTime.UtcNow);
        var log = await _db.ActivityLogs.AsNoTracking().FirstOrDefaultAsync(item => item.UserId == userId && item.LocalDate == local, cancellationToken);
        return log is null ? null : MapActivity(log, profile.ActivityGoalMinutes);
    }

    public async Task<ActivityDto> LogAsync(Guid userId, LogActivityRequest request, CancellationToken cancellationToken)
    {
        var (user, profile) = await UserLoader.RequireAsync(_db, userId, cancellationToken);
        var local = Parsers.ParseDateOrToday(request.LocalDate, user.Timezone, DateTime.UtcNow);
        var log = await _db.ActivityLogs.FirstOrDefaultAsync(item => item.UserId == userId && item.LocalDate == local, cancellationToken);
        if (log is null)
        {
            log = new ActivityLog { UserId = userId, LocalDate = local };
            _db.ActivityLogs.Add(log);
        }

        log.ActivityType = request.ActivityType.Trim();
        log.DurationMinutes = request.DurationMinutes;
        log.Completed = request.Completed || request.DurationMinutes >= profile.ActivityGoalMinutes;
        log.Notes = request.Notes?.Trim();
        await _db.SaveChangesAsync(cancellationToken);
        return MapActivity(log, profile.ActivityGoalMinutes);
    }

    Task<SleepDto?> ISleepService.GetAsync(Guid userId, string? date, CancellationToken cancellationToken) =>
        GetSleepAsync(userId, date, cancellationToken);

    public async Task<SleepDto?> GetSleepAsync(Guid userId, string? date, CancellationToken cancellationToken)
    {
        var (user, profile) = await UserLoader.RequireAsync(_db, userId, cancellationToken);
        var local = Parsers.ParseDateOrToday(date, user.Timezone, DateTime.UtcNow);
        var log = await _db.SleepLogs.AsNoTracking().FirstOrDefaultAsync(item => item.UserId == userId && item.LocalDate == local, cancellationToken);
        return log is null ? null : MapSleep(log, profile.SleepGoalMinutes);
    }

    public async Task<SleepDto> LogAsync(Guid userId, LogSleepRequest request, CancellationToken cancellationToken)
    {
        var (user, profile) = await UserLoader.RequireAsync(_db, userId, cancellationToken);
        var local = Parsers.ParseDateOrToday(request.LocalDate, user.Timezone, DateTime.UtcNow);
        var log = await _db.SleepLogs.FirstOrDefaultAsync(item => item.UserId == userId && item.LocalDate == local, cancellationToken);
        if (log is null)
        {
            log = new SleepLog { UserId = userId, LocalDate = local };
            _db.SleepLogs.Add(log);
        }

        log.DurationMinutes = request.DurationMinutes;
        log.Quality = request.Quality;
        log.Notes = request.Notes?.Trim();
        await _db.SaveChangesAsync(cancellationToken);
        return MapSleep(log, profile.SleepGoalMinutes);
    }

    Task<CheckInResponse> ICheckInService.GetAsync(Guid userId, string? date, CancellationToken cancellationToken) =>
        CheckIn(userId, date, cancellationToken);

    public async Task<CheckInResponse> SaveAsync(Guid userId, CheckInRequest request, CancellationToken cancellationToken)
    {
        var (user, _) = await UserLoader.RequireAsync(_db, userId, cancellationToken);
        var local = Parsers.ParseDateOrToday(request.LocalDate, user.Timezone, DateTime.UtcNow);
        var checkIn = await _db.CheckIns.FirstOrDefaultAsync(item => item.UserId == userId && item.LocalDate == local, cancellationToken);
        if (checkIn is null)
        {
            checkIn = new DailyCheckIn { UserId = userId, LocalDate = local };
            _db.CheckIns.Add(checkIn);
        }

        checkIn.Mood = request.Mood;
        checkIn.Energy = request.Energy;
        checkIn.Notes = request.Notes?.Trim();
        var existing = await _db.HabitChecks.Where(item => item.UserId == userId && item.LocalDate == local).ToListAsync(cancellationToken);
        _db.HabitChecks.RemoveRange(existing);
        var codes = (request.CompletedHabitCodes ?? []).Where(code => !string.IsNullOrWhiteSpace(code)).Select(code => code.Trim()).Distinct(StringComparer.OrdinalIgnoreCase);
        var known = await _db.HabitDefinitions.Select(item => item.Code).ToListAsync(cancellationToken);
        foreach (var code in codes.Where(code => known.Contains(code, StringComparer.OrdinalIgnoreCase)))
        {
            _db.HabitChecks.Add(new HabitCheck { UserId = userId, HabitCode = code, LocalDate = local, Completed = true });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await CheckIn(userId, local.ToString("yyyy-MM-dd"), cancellationToken);
    }

    Task<ProgressResponse> IProgressService.GetAsync(Guid userId, string range, CancellationToken cancellationToken) =>
        GetProgressAsync(userId, range, cancellationToken);

    public async Task<ProgressResponse> GetProgressAsync(Guid userId, string range, CancellationToken cancellationToken)
    {
        var days = range switch { "15" => 15, "30" => 30, "90" => 90, _ => 7 };
        var (user, profile) = await UserLoader.RequireAsync(_db, userId, cancellationToken);
        var end = TimezoneClock.LocalToday(user.Timezone, DateTime.UtcNow);
        var start = end.AddDays(1 - days);
        var series = await Series(userId, profile.WaterGoalMl, profile.ActivityGoalMinutes, start, end, cancellationToken);
        var weightPoints = series.Where(item => item.Weight is not null).Select(item => new WeightPoint(item.Date, item.Weight!.Value)).ToList();
        var average = series.Count == 0 ? 0 : Math.Round(series.Average(item => item.Adherence), 1);
        return new ProgressResponse(
            days.ToString(),
            series.Where(item => item.Weight is not null).Select(item => Point(item.Date, item.Weight!.Value)).ToList(),
            series.Select(item => Point(item.Date, item.Adherence)).ToList(),
            series.Select(item => Point(item.Date, item.Water)).ToList(),
            series.Select(item => Point(item.Date, item.Meals)).ToList(),
            series.Select(item => Point(item.Date, item.Activity)).ToList(),
            series.Select(item => Point(item.Date, item.Sleep)).ToList(),
            WeightTrendCalculator.Change(weightPoints, days),
            (decimal)average);
    }

    public async Task<IReadOnlyList<CalendarDayDto>> GetCalendarAsync(Guid userId, string? month, CancellationToken cancellationToken)
    {
        var (user, profile) = await UserLoader.RequireAsync(_db, userId, cancellationToken);
        var today = TimezoneClock.LocalToday(user.Timezone, DateTime.UtcNow);
        var cursor = today;
        if (!string.IsNullOrWhiteSpace(month) && DateOnly.TryParse(month + "-01", out var parsed))
        {
            cursor = parsed;
        }

        var start = new DateOnly(cursor.Year, cursor.Month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        var series = await Series(userId, profile.WaterGoalMl, profile.ActivityGoalMinutes, start, end, cancellationToken);
        var assignment = await _db.PlanAssignments.AsNoTracking().Include(item => item.DietPlan)
            .Where(item => item.UserId == userId && item.Status == PlanAssignmentStatus.Active)
            .OrderByDescending(item => item.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        return series.Select(item =>
        {
            int? planDay = null;
            if (assignment is not null)
            {
                var state = PlanDayResolver.Resolve(assignment.StartDate, item.Date, assignment.DietPlan.DurationDays);
                if (!state.NotStarted && !state.Completed)
                {
                    planDay = state.DayNumber;
                }
            }

            return new CalendarDayDto(
                item.Date.ToString("yyyy-MM-dd"),
                planDay,
                item.MealsPlanned > 0,
                item.MealsCompleted,
                item.MealsPlanned,
                item.Water,
                item.Weight,
                item.Activity == 0 ? null : item.Activity,
                item.Sleep == 0 ? null : item.Sleep,
                item.Adherence);
        }).ToList();
    }

    private async Task<WaterDayResponse> WaterDay(Guid userId, string? date, CancellationToken cancellationToken)
    {
        var (user, profile) = await UserLoader.RequireAsync(_db, userId, cancellationToken);
        var local = Parsers.ParseDateOrToday(date, user.Timezone, DateTime.UtcNow);
        var entries = await _db.WaterEntries.AsNoTracking()
            .Where(item => item.UserId == userId && item.LocalDate == local)
            .OrderBy(item => item.LoggedAtUtc)
            .ToListAsync(cancellationToken);
        var progress = WaterCalculator.Progress(profile.WaterGoalMl, entries.Sum(item => item.AmountMl));
        var guidance = progress.ConsumedMl > 4000
            ? "Logged water is above the usual planning range. TEVSCARE does not recommend forcing high volumes."
            : "Your goal is personal. A planning suggestion is about 35 ml per kilogram, kept between 2 and 3.5 litres unless a clinician set a different goal.";
        return new WaterDayResponse(
            local.ToString("yyyy-MM-dd"),
            new WaterSummaryDto(progress.GoalMl, progress.ConsumedMl, progress.RemainingMl, progress.Percent),
            guidance,
            entries.Select(item => new WaterEntryDto(item.Id, item.AmountMl, item.LoggedAtUtc)).ToList());
    }

    private async Task<(Domain.Account.UserProfile Profile, DateOnly Date)> WaitProfile(Guid userId, string? date, CancellationToken cancellationToken)
    {
        var (user, profile) = await UserLoader.RequireAsync(_db, userId, cancellationToken);
        return (profile, Parsers.ParseDateOrToday(date, user.Timezone, DateTime.UtcNow));
    }

    private async Task<(Identity.ApplicationUser User, DateOnly Date)> Clock(Guid userId, string? date, CancellationToken cancellationToken)
    {
        var (user, _) = await UserLoader.RequireAsync(_db, userId, cancellationToken);
        return (user, Parsers.ParseDateOrToday(date, user.Timezone, DateTime.UtcNow));
    }

    private async Task<CheckInResponse> CheckIn(Guid userId, string? date, CancellationToken cancellationToken)
    {
        var (profile, local) = await WaitProfile(userId, date, cancellationToken);
        var checkIn = await _db.CheckIns.AsNoTracking().FirstOrDefaultAsync(item => item.UserId == userId && item.LocalDate == local, cancellationToken);
        var habits = await HabitList(userId, local, cancellationToken);
        var logs = await _db.MealLogs.AsNoTracking().Where(item => item.UserId == userId && item.LocalDate == local).ToListAsync(cancellationToken);
        var water = await _db.WaterEntries.Where(item => item.UserId == userId && item.LocalDate == local).SumAsync(item => (int?)item.AmountMl, cancellationToken) ?? 0;
        var activity = await _db.ActivityLogs.AsNoTracking().FirstOrDefaultAsync(item => item.UserId == userId && item.LocalDate == local, cancellationToken);
        var sleep = await _db.SleepLogs.AnyAsync(item => item.UserId == userId && item.LocalDate == local, cancellationToken);
        var weight = await _db.WeightEntries.AnyAsync(item => item.UserId == userId && item.LocalDate == local, cancellationToken);
        var adherence = AdherenceCalculator.Calculate(
            5,
            logs.Sum(item => AdherenceCalculator.StatusWeight(item.Status.ToString())),
            water >= profile.WaterGoalMl,
            activity is not null && (activity.Completed || activity.DurationMinutes >= profile.ActivityGoalMinutes),
            sleep,
            weight);
        return new CheckInResponse(local.ToString("yyyy-MM-dd"), checkIn?.Mood, checkIn?.Energy, checkIn?.Notes, new AdherenceSummaryDto(adherence.Score, MealMapper.AdherenceExplanation), habits);
    }

    private async Task<List<HabitDto>> HabitList(Guid userId, DateOnly date, CancellationToken cancellationToken)
    {
        var definitions = await _db.HabitDefinitions.AsNoTracking().OrderBy(item => item.SortOrder).ToListAsync(cancellationToken);
        var done = await _db.HabitChecks.AsNoTracking()
            .Where(item => item.UserId == userId && item.LocalDate == date && item.Completed)
            .Select(item => item.HabitCode)
            .ToListAsync(cancellationToken);
        return definitions.Select(item => new HabitDto(item.Code, item.Title, item.Description, done.Contains(item.Code))).ToList();
    }

    private async Task<List<DaySeries>> Series(Guid userId, int waterGoal, int activityGoal, DateOnly start, DateOnly end, CancellationToken cancellationToken)
    {
        var weights = await _db.WeightEntries.AsNoTracking().Where(item => item.UserId == userId && item.LocalDate >= start && item.LocalDate <= end).ToListAsync(cancellationToken);
        var water = await _db.WaterEntries.AsNoTracking().Where(item => item.UserId == userId && item.LocalDate >= start && item.LocalDate <= end).ToListAsync(cancellationToken);
        var meals = await _db.MealLogs.AsNoTracking().Where(item => item.UserId == userId && item.LocalDate >= start && item.LocalDate <= end).ToListAsync(cancellationToken);
        var activity = await _db.ActivityLogs.AsNoTracking().Where(item => item.UserId == userId && item.LocalDate >= start && item.LocalDate <= end).ToListAsync(cancellationToken);
        var sleep = await _db.SleepLogs.AsNoTracking().Where(item => item.UserId == userId && item.LocalDate >= start && item.LocalDate <= end).ToListAsync(cancellationToken);
        var list = new List<DaySeries>();
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            var dayMeals = meals.Where(item => item.LocalDate == date).ToList();
            var dayWater = water.Where(item => item.LocalDate == date).Sum(item => item.AmountMl);
            var dayActivity = activity.FirstOrDefault(item => item.LocalDate == date);
            var daySleep = sleep.FirstOrDefault(item => item.LocalDate == date);
            var dayWeight = weights.FirstOrDefault(item => item.LocalDate == date);
            var adherence = AdherenceCalculator.Calculate(
                5,
                dayMeals.Sum(item => AdherenceCalculator.StatusWeight(item.Status.ToString())),
                dayWater >= waterGoal,
                dayActivity is not null && (dayActivity.Completed || dayActivity.DurationMinutes >= activityGoal),
                daySleep is not null,
                dayWeight is not null);
            list.Add(new DaySeries(
                date,
                dayWeight?.WeightKg,
                adherence.Score,
                dayWater,
                (int)Math.Round(adherence.MealsCompletedWeight * 100m / 5m),
                dayActivity?.DurationMinutes ?? 0,
                daySleep?.DurationMinutes ?? 0,
                dayMeals.Count(item => item.Status == MealLogStatus.Completed),
                5));
        }

        return list;
    }

    private static ProgressPointDto Point(DateOnly date, decimal value) => new(date.ToString("yyyy-MM-dd"), value);
    private static ActivityDto MapActivity(ActivityLog log, int goal) => new(log.Id, log.LocalDate.ToString("yyyy-MM-dd"), log.ActivityType, log.DurationMinutes, log.Completed, log.Notes, goal);
    private static SleepDto MapSleep(SleepLog log, int goal) => new(log.Id, log.LocalDate.ToString("yyyy-MM-dd"), log.DurationMinutes, log.Quality, log.Notes, goal);

    private sealed record DaySeries(DateOnly Date, decimal? Weight, int Adherence, int Water, int Meals, int Activity, int Sleep, int MealsCompleted, int MealsPlanned);
}
