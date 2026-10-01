using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tevscare.Application.Abstractions;
using Tevscare.Application.Common;
using Tevscare.Application.Contracts;
using Tevscare.Application.Planning;
using Tevscare.Domain.Account;
using Tevscare.Domain.Enums;
using Tevscare.Domain.Identity;
using Tevscare.Domain.Planning;
using Tevscare.Infrastructure.Identity;
using Tevscare.Infrastructure.Persistence;

namespace Tevscare.Infrastructure.Services;

internal static class UserLoader
{
    public static async Task<(ApplicationUser User, UserProfile Profile)> RequireAsync(AppDbContext db, Guid userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null || user.DeletedAtUtc is not null)
        {
            throw AppException.Unauthorized();
        }

        var profile = await db.Profiles
            .Include(item => item.Allergies)
            .Include(item => item.FoodPreferences)
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        if (profile is null)
        {
            profile = new UserProfile { UserId = userId };
            db.Profiles.Add(profile);
            await db.SaveChangesAsync(cancellationToken);
        }

        return (user, profile);
    }
}

public class ProfileService : IProfileService
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public ProfileService(AppDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<ProfileResponse> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var (user, profile) = await UserLoader.RequireAsync(_db, userId, cancellationToken);
        return await MapAsync(user, profile, cancellationToken);
    }

    public async Task<ProfileResponse> UpdateAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var (user, profile) = await UserLoader.RequireAsync(_db, userId, cancellationToken);
        TimezoneClock.Resolve(request.Timezone);

        user.FullName = request.FullName.Trim();
        user.Timezone = request.Timezone.Trim();
        profile.Age = request.Age;
        profile.HeightCm = request.HeightCm;
        profile.CurrentWeightKg = request.CurrentWeightKg;
        profile.StartingWeightKg ??= request.CurrentWeightKg;
        profile.TargetWeightKg = request.TargetWeightKg;
        profile.DietaryPreference = Parsers.ParseEnum<DietaryPreference>(request.DietaryPreference, "Dietary preference");
        profile.ActivityLevel = Parsers.ParseEnum<ActivityLevel>(request.ActivityLevel, "Activity level");
        profile.WaterGoalMl = request.WaterGoalMl;
        profile.SleepGoalMinutes = request.SleepGoalMinutes;
        profile.ActivityGoalMinutes = request.ActivityGoalMinutes;
        profile.WakeTime = Parsers.ParseTimeOr(request.WakeTime, profile.WakeTime);
        profile.BreakfastTime = Parsers.ParseTimeOr(request.BreakfastTime, profile.BreakfastTime);
        profile.LunchTime = Parsers.ParseTimeOr(request.LunchTime, profile.LunchTime);
        profile.DinnerTime = Parsers.ParseTimeOr(request.DinnerTime, profile.DinnerTime);
        profile.SleepTime = Parsers.ParseTimeOr(request.SleepTime, profile.SleepTime);

        _db.Allergies.RemoveRange(profile.Allergies.ToList());
        _db.FoodPreferences.RemoveRange(profile.FoodPreferences.ToList());
        profile.Allergies.Clear();
        profile.FoodPreferences.Clear();
        foreach (var name in (request.Allergies ?? []).Select(item => item.Trim()).Where(item => item.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var allergy = new UserAllergy { UserId = userId, Name = name };
            _db.Allergies.Add(allergy);
            profile.Allergies.Add(allergy);
        }

        foreach (var item in (request.FoodPreferences ?? []).Where(item => !string.IsNullOrWhiteSpace(item.Name)).GroupBy(item => item.Name.Trim(), StringComparer.OrdinalIgnoreCase).Select(group => group.First()))
        {
            var preference = new UserFoodPreference
            {
                UserId = userId,
                Name = item.Name.Trim(),
                Kind = Parsers.ParseEnum<FoodPreferenceKind>(item.Kind, "Food preference")
            };
            _db.FoodPreferences.Add(preference);
            profile.FoodPreferences.Add(preference);
        }

        var wasComplete = profile.OnboardingCompleted;
        profile.OnboardingCompleted = true;
        if (!wasComplete)
        {
            var hasPlan = await _db.PlanAssignments.AnyAsync(
                item => item.UserId == userId && item.Status == PlanAssignmentStatus.Active,
                cancellationToken);
            if (!hasPlan)
            {
                var plan = await _db.DietPlans.FirstOrDefaultAsync(item => item.IsPublished, cancellationToken)
                    ?? throw new AppException("No published diet plan is available yet.");
                _db.PlanAssignments.Add(new UserPlanAssignment
                {
                    UserId = userId,
                    DietPlanId = plan.Id,
                    StartDate = TimezoneClock.LocalToday(user.Timezone, DateTime.UtcNow),
                    Status = PlanAssignmentStatus.Active
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await MapAsync(user, profile, cancellationToken);
    }

    public async Task DeleteAccountAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _users.FindByIdAsync(userId.ToString()) ?? throw AppException.NotFound("Account was not found.");
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var logIds = _db.MealLogs.Where(item => item.UserId == userId).Select(item => item.Id);
        await _db.MealLogItems.Where(item => logIds.Contains(item.MealLogId)).ExecuteDeleteAsync(cancellationToken);
        await _db.MealLogs.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await _db.WaterEntries.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await _db.WeightEntries.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await _db.ActivityLogs.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await _db.SleepLogs.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await _db.CheckIns.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await _db.HabitChecks.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await _db.AdherenceSnapshots.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await _db.FoodPrices.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await _db.ShoppingStates.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await _db.PlanAssignments.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await _db.RefreshTokens.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await _db.PasswordResetTokens.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await _db.Allergies.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await _db.FoodPreferences.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        var settingsIds = _db.NotificationSettings.Where(item => item.UserId == userId).Select(item => item.Id);
        await _db.NotificationPreferences.Where(item => settingsIds.Contains(item.SettingsId)).ExecuteDeleteAsync(cancellationToken);
        await _db.NotificationSettings.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await _db.Budgets.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await _db.Entitlements.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await _db.ProductEvents.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await _db.Profiles.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        _db.ChangeTracker.Clear();
        var delete = await _users.DeleteAsync(user);
        if (!delete.Succeeded)
        {
            throw new AppException("The account could not be deleted.");
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<ProfileResponse> MapAsync(ApplicationUser user, UserProfile profile, CancellationToken cancellationToken)
    {
        var entitlement = await _db.Entitlements.AsNoTracking()
            .Where(item => item.UserId == user.Id && item.Status == EntitlementStatus.Active)
            .OrderByDescending(item => item.StartsAtUtc)
            .Select(item => item.Plan.ToString())
            .FirstOrDefaultAsync(cancellationToken) ?? "Free";

        var suggested = WaterCalculator.SuggestGoalMl(profile.CurrentWeightKg ?? 0);
        return new ProfileResponse(
            user.Id,
            user.FullName,
            user.Email ?? string.Empty,
            user.Timezone,
            profile.Age,
            profile.HeightCm,
            profile.CurrentWeightKg,
            profile.TargetWeightKg,
            profile.StartingWeightKg,
            profile.DietaryPreference.ToString(),
            profile.ActivityLevel.ToString(),
            profile.WaterGoalMl,
            suggested,
            WaterCalculator.Progress(profile.WaterGoalMl, 0).OutsideSuggestedRange,
            profile.SleepGoalMinutes,
            profile.ActivityGoalMinutes,
            Parsers.Clock(profile.WakeTime),
            Parsers.Clock(profile.BreakfastTime),
            Parsers.Clock(profile.LunchTime),
            Parsers.Clock(profile.DinnerTime),
            Parsers.Clock(profile.SleepTime),
            profile.OnboardingCompleted,
            profile.Allergies.Select(item => item.Name).OrderBy(name => name).ToList(),
            profile.FoodPreferences.Select(item => new FoodPreferenceDto(item.Name, item.Kind.ToString())).ToList(),
            entitlement,
            ProductDisclaimer.Text);
    }
}
