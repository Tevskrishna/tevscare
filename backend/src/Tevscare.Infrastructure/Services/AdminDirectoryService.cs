using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tevscare.Application.Abstractions;
using Tevscare.Application.Common;
using Tevscare.Application.Contracts;
using Tevscare.Domain.Account;
using Tevscare.Domain.Enums;
using Tevscare.Domain.Identity;
using Tevscare.Infrastructure.Identity;
using Tevscare.Infrastructure.Persistence;

namespace Tevscare.Infrastructure.Services;

public class AdminDirectoryService : IAdminDirectoryService
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public AdminDirectoryService(AppDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<AdminDashboardResponse> DashboardAsync(CancellationToken cancellationToken)
    {
        var since = DateTime.UtcNow.AddDays(-7);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var weekStart = today.AddDays(-6);
        var users = _db.Users.AsNoTracking().Where(user => user.DeletedAtUtc == null);
        var now = DateTimeOffset.UtcNow;
        var totalUsers = await users.CountAsync(cancellationToken);
        var lockouts = await users.Select(user => user.LockoutEnd).ToListAsync(cancellationToken);
        var lockedUsers = lockouts.Count(end => end.HasValue && end.Value > now);

        return new AdminDashboardResponse(
            totalUsers,
            totalUsers - lockedUsers,
            await users.CountAsync(user => user.CreatedAtUtc >= since, cancellationToken),
            await _db.DietPlans.CountAsync(plan => plan.IsPublished, cancellationToken),
            await _db.DietPlans.CountAsync(plan => !plan.IsPublished, cancellationToken),
            await _db.PlanAssignments.CountAsync(assignment => assignment.Status == PlanAssignmentStatus.Active, cancellationToken),
            await _db.CheckIns.CountAsync(checkIn => checkIn.LocalDate >= weekStart, cancellationToken),
            await _db.WeightEntries.CountAsync(entry => entry.LocalDate >= weekStart, cancellationToken),
            await _db.WaterEntries.CountAsync(entry => entry.LocalDate >= weekStart, cancellationToken),
            "ok",
            "ok");
    }

    public async Task<AdminUserPage> UsersAsync(string? query, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var users = _db.Users.AsNoTracking().Where(user => user.DeletedAtUtc == null);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            users = users.Where(user => user.FullName.Contains(term) || (user.Email != null && user.Email.Contains(term)));
        }

        var total = await users.CountAsync(cancellationToken);
        var slice = await users.OrderByDescending(user => user.CreatedAtUtc).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var items = await MapUsersAsync(slice, cancellationToken);
        return new AdminUserPage(page, pageSize, total, items);
    }

    public async Task<AdminUserDetail> UserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == userId && item.DeletedAtUtc == null, cancellationToken)
            ?? throw AppException.NotFound("User was not found.");
        var profile = await _db.Profiles.AsNoTracking().FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        var entitlement = await LatestEntitlementAsync([userId], cancellationToken);
        var assignment = await _db.PlanAssignments.AsNoTracking()
            .Include(item => item.DietPlan)
            .Where(item => item.UserId == userId && item.Status == PlanAssignmentStatus.Active)
            .OrderByDescending(item => item.StartDate)
            .FirstOrDefaultAsync(cancellationToken);
        var weight = await _db.WeightEntries.AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.LocalDate)
            .Take(30)
            .ToListAsync(cancellationToken);
        var roles = await _users.GetRolesAsync(user);
        var current = entitlement.GetValueOrDefault(userId);

        return new AdminUserDetail(
            user.Id,
            user.FullName,
            user.Email ?? string.Empty,
            user.Timezone,
            roles.ToList(),
            IsLocked(user),
            profile?.OnboardingCompleted ?? false,
            profile?.PreferredLanguage ?? "en",
            profile?.DietaryPreference.ToString() ?? DietaryPreference.Eggetarian.ToString(),
            profile?.CurrentWeightKg,
            profile?.TargetWeightKg,
            current?.Plan.ToString() ?? EntitlementPlan.Free.ToString(),
            current?.Status.ToString() ?? EntitlementStatus.Active.ToString(),
            assignment?.DietPlan.Name,
            assignment?.StartDate.ToString("yyyy-MM-dd"),
            await _db.MealLogs.CountAsync(item => item.UserId == userId, cancellationToken),
            await _db.WaterEntries.CountAsync(item => item.UserId == userId, cancellationToken),
            await _db.WeightEntries.CountAsync(item => item.UserId == userId, cancellationToken),
            await _db.ActivityLogs.CountAsync(item => item.UserId == userId, cancellationToken),
            await _db.SleepLogs.CountAsync(item => item.UserId == userId, cancellationToken),
            await _db.CheckIns.CountAsync(item => item.UserId == userId, cancellationToken),
            weight.Select(item => new AdminWeightPoint(item.LocalDate.ToString("yyyy-MM-dd"), item.WeightKg)).ToList());
    }

    public async Task LockAsync(Guid actorId, Guid userId, bool locked, CancellationToken cancellationToken)
    {
        var user = await _users.FindByIdAsync(userId.ToString()) ?? throw AppException.NotFound("User was not found.");
        if (user.DeletedAtUtc is not null)
        {
            throw AppException.NotFound("User was not found.");
        }

        if (user.Id == actorId && locked)
        {
            throw new AppException("You cannot lock your own account.");
        }

        await _users.SetLockoutEnabledAsync(user, true);
        await _users.SetLockoutEndDateAsync(user, locked ? DateTimeOffset.UtcNow.AddYears(100) : null);
        await RecordAsync(actorId, locked ? "UserLocked" : "UserUnlocked", "User", user.Id.ToString(), user.Email, cancellationToken);
    }

    public async Task<AdminUserListItem> CreateNutritionistAsync(Guid actorId, CreateNutritionistRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _users.FindByEmailAsync(email) is not null)
        {
            throw new AppException("An account with this email already exists.", 409);
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = request.FullName.Trim(),
            Timezone = "Asia/Kolkata",
            CreatedAtUtc = DateTime.UtcNow
        };
        var created = await _users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            throw new AppException(string.Join(" ", created.Errors.Select(error => error.Description)));
        }

        await _users.AddToRoleAsync(user, Roles.Nutritionist);
        _db.Profiles.Add(new UserProfile { UserId = user.Id });
        _db.Budgets.Add(new BudgetSettings { UserId = user.Id, DailyBudgetAmount = 675, Currency = "INR" });
        _db.NotificationSettings.Add(NotificationDefaults.Create(user.Id));
        _db.Entitlements.Add(AccountFactory.FreeEntitlement(user.Id));
        await _db.SaveChangesAsync(cancellationToken);
        await RecordAsync(actorId, "NutritionistCreated", "User", user.Id.ToString(), email, cancellationToken);
        var mapped = await MapUsersAsync([user], cancellationToken);
        return mapped[0];
    }

    public async Task<IReadOnlyList<AdminPlanDto>> PlansAsync(CancellationToken cancellationToken)
    {
        var plans = await _db.DietPlans.AsNoTracking()
            .Select(plan => new { plan.Id, plan.Name, plan.Description, plan.DurationDays, plan.IsPublished, DayCount = plan.Days.Count })
            .OrderBy(plan => plan.Name)
            .ToListAsync(cancellationToken);
        var counts = await _db.PlanAssignments.AsNoTracking()
            .Where(assignment => assignment.Status == PlanAssignmentStatus.Active)
            .GroupBy(assignment => assignment.DietPlanId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Key, group => group.Count, cancellationToken);
        return plans.Select(plan => new AdminPlanDto(
            plan.Id,
            plan.Name,
            plan.Description,
            plan.DurationDays,
            plan.IsPublished,
            plan.DayCount,
            counts.GetValueOrDefault(plan.Id))).ToList();
    }

    public async Task<IReadOnlyList<GuidanceDto>> GuidanceAsync(CancellationToken cancellationToken)
    {
        return await _db.Guidance.AsNoTracking()
            .OrderBy(item => item.SortOrder)
            .Select(item => new GuidanceDto(item.Id, item.Category.ToString(), item.Title, item.Body, item.Label, item.ConditionKey))
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminAuditPage> AuditAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var query = _db.AdminAudit.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(item => item.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new AdminAuditDto(item.Id, item.CreatedAtUtc, item.ActorRole, item.Action, item.EntityName, item.EntityId, item.Detail))
            .ToListAsync(cancellationToken);
        return new AdminAuditPage(page, pageSize, total, items);
    }

    public async Task RecordAsync(Guid actorId, string action, string entityName, string? entityId, string? detail, CancellationToken cancellationToken)
    {
        var user = await _users.FindByIdAsync(actorId.ToString());
        var roles = user is null ? "unknown" : string.Join(",", await _users.GetRolesAsync(user));
        _db.AdminAudit.Add(new AdminAuditEntry
        {
            ActorUserId = actorId,
            ActorRole = roles.Length > 80 ? roles[..80] : roles,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Detail = string.IsNullOrWhiteSpace(detail) ? null : detail.Trim().Length > 500 ? detail.Trim()[..500] : detail.Trim()
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<AdminUserListItem>> MapUsersAsync(IReadOnlyList<ApplicationUser> users, CancellationToken cancellationToken)
    {
        var ids = users.Select(user => user.Id).ToList();
        var profiles = await _db.Profiles.AsNoTracking().Where(profile => ids.Contains(profile.UserId)).ToDictionaryAsync(profile => profile.UserId, cancellationToken);
        var entitlements = await LatestEntitlementAsync(ids, cancellationToken);
        var roleRows = await (
            from link in _db.UserRoles
            join role in _db.Roles on link.RoleId equals role.Id
            where ids.Contains(link.UserId)
            select new { link.UserId, role.Name }).ToListAsync(cancellationToken);
        var roles = roleRows.GroupBy(row => row.UserId).ToDictionary(group => group.Key, group => (IReadOnlyList<string>)group.Select(row => row.Name ?? string.Empty).ToList());

        return users.Select(user =>
        {
            var entitlement = entitlements.GetValueOrDefault(user.Id);
            return new AdminUserListItem(
                user.Id,
                user.FullName,
                user.Email ?? string.Empty,
                roles.GetValueOrDefault(user.Id) ?? [],
                user.CreatedAtUtc,
                IsLocked(user),
                profiles.GetValueOrDefault(user.Id)?.OnboardingCompleted ?? false,
                entitlement?.Plan.ToString() ?? EntitlementPlan.Free.ToString());
        }).ToList();
    }

    private async Task<Dictionary<Guid, Entitlement>> LatestEntitlementAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        var rows = await _db.Entitlements.AsNoTracking()
            .Where(item => ids.Contains(item.UserId))
            .OrderByDescending(item => item.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        return rows.GroupBy(item => item.UserId).ToDictionary(group => group.Key, group => group.First());
    }

    private static bool IsLocked(ApplicationUser user) =>
        user.LockoutEnd is not null && user.LockoutEnd > DateTimeOffset.UtcNow;
}
