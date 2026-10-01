using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Tevscare.Application.Common;
using Tevscare.Application.Contracts;
using Tevscare.Application.Planning;
using Tevscare.Domain.Account;
using Tevscare.Domain.Enums;
using Tevscare.Domain.Identity;
using Tevscare.Domain.Planning;
using Tevscare.Infrastructure.Identity;
using Tevscare.Infrastructure.Options;

namespace Tevscare.Infrastructure.Services;

public static class TokenCodec
{
    public static string NewToken()
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        return raw.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static string Hash(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}

public static class Parsers
{
    public static TEnum ParseEnum<TEnum>(string? value, string field) where TEnum : struct, Enum
    {
        if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse<TEnum>(value.Replace("-", string.Empty).Replace(" ", string.Empty), true, out var parsed))
        {
            return parsed;
        }

        throw new AppException($"{field} is not valid.");
    }

    public static DateOnly ParseDateOrToday(string? value, string? timezone, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return TimezoneClock.LocalToday(timezone, utcNow);
        }

        if (DateOnly.TryParse(value, out var date))
        {
            return date;
        }

        throw new AppException("Date must use the yyyy-MM-dd format.");
    }

    public static TimeOnly ParseTime(string? value, string field)
    {
        if (!string.IsNullOrWhiteSpace(value) && TimeOnly.TryParse(value, out var time))
        {
            return time;
        }

        throw new AppException($"{field} must use HH:mm.");
    }

    public static TimeOnly ParseTimeOr(string? value, TimeOnly fallback) =>
        !string.IsNullOrWhiteSpace(value) && TimeOnly.TryParse(value, out var time) ? time : fallback;

    public static string Clock(TimeOnly time) => time.ToString("HH:mm");

    public static string Grocery(GroceryCategory category) => category switch
    {
        GroceryCategory.NutsAndSeeds => "Nuts & Seeds",
        GroceryCategory.Vegetables => "Vegetables",
        GroceryCategory.Fruits => "Fruits",
        GroceryCategory.Grains => "Grains",
        GroceryCategory.Pulses => "Pulses",
        GroceryCategory.Dairy => "Dairy",
        GroceryCategory.Protein => "Protein",
        _ => "Other"
    };

    public static GroceryCategory ParseGrocery(string value)
    {
        var normalized = value.Replace("&", string.Empty).Replace(" ", string.Empty);
        if (normalized.Equals("NutsSeeds", StringComparison.OrdinalIgnoreCase) || normalized.Equals("NutsAndSeeds", StringComparison.OrdinalIgnoreCase))
        {
            return GroceryCategory.NutsAndSeeds;
        }

        return ParseEnum<GroceryCategory>(normalized, "Grocery category");
    }
}

public static class NotificationDefaults
{
    public static UserNotificationSettings Create(Guid userId)
    {
        var settings = new UserNotificationSettings
        {
            UserId = userId,
            QuietHoursEnabled = true,
            QuietStart = new TimeOnly(22, 0),
            QuietEnd = new TimeOnly(7, 0)
        };

        settings.Preferences =
        [
            Pref(settings, NotificationCategory.MorningWeight, new TimeOnly(7, 0)),
            Pref(settings, NotificationCategory.Breakfast, new TimeOnly(8, 0)),
            new NotificationPreference
            {
                Settings = settings,
                Category = NotificationCategory.Water,
                Enabled = true,
                IntervalMinutes = 60,
                WindowStart = new TimeOnly(8, 0),
                WindowEnd = new TimeOnly(20, 0)
            },
            Pref(settings, NotificationCategory.Lunch, new TimeOnly(13, 0)),
            Pref(settings, NotificationCategory.Activity, new TimeOnly(17, 30)),
            Pref(settings, NotificationCategory.Dinner, new TimeOnly(19, 30)),
            Pref(settings, NotificationCategory.Sleep, new TimeOnly(21, 30))
        ];

        return settings;
    }

    public static QuietHours Quiet(UserNotificationSettings settings) =>
        new(settings.QuietHoursEnabled, settings.QuietStart, settings.QuietEnd);

    public static IReadOnlyList<NotificationPreferenceInput> Inputs(UserNotificationSettings settings) =>
        settings.Preferences.Select(p => new NotificationPreferenceInput(
            p.Category.ToString(),
            p.Enabled,
            p.LocalTime,
            p.IntervalMinutes,
            p.WindowStart,
            p.WindowEnd)).ToList();

    private static NotificationPreference Pref(UserNotificationSettings settings, NotificationCategory category, TimeOnly time) =>
        new()
        {
            Settings = settings,
            Category = category,
            Enabled = true,
            LocalTime = time
        };
}

public static class MealMapper
{
    public const string AdherenceExplanation =
        "This score shows how much of today's plan was logged. It is a tracking indicator, not a health grade.";

    public const string PriceNote =
        "Prices are estimates you can change for your store. TEVSCARE does not treat grocery prices as globally fixed.";

    public static MealCardDto ToCard(
        Meal meal,
        DietaryPreference preference,
        IReadOnlyCollection<string> allergies,
        IReadOnlyDictionary<Guid, decimal> prices,
        string? logStatus,
        bool applyDietFilter = true)
    {
        var planItems = meal.Items
            .OrderBy(i => i.SortOrder)
            .Select(item =>
            {
                decimal? price = null;
                if (item.FoodId is Guid foodId)
                {
                    price = prices.TryGetValue(foodId, out var custom) ? custom : item.Food?.ReferencePriceInr;
                }

                return new PlanItem(
                    item.Id,
                    item.FoodId,
                    item.DisplayName,
                    item.Quantity,
                    item.Unit,
                    item.IsOptional,
                    item.AlternativeGroup,
                    item.IsDefaultAlternative,
                    item.SortOrder,
                    item.Food?.SuitableFor ?? DietFlags.All,
                    price,
                    item.Note,
                    Parsers.Grocery(item.Food?.GroceryCategory ?? GroceryCategory.Other),
                    item.Food?.Allergens);
            })
            .ToList();

        var visible = applyDietFilter
            ? DietFilter.SelectForDiet(planItems, preference, allergies)
            : planItems;
        var priced = applyDietFilter
            ? DietFilter.SelectForShopping(planItems, preference, allergies)
            : planItems.Where(i => i.AlternativeGroup is null || i.IsDefaultAlternative).ToList();

        decimal cost = 0;
        var missing = false;
        foreach (var item in priced)
        {
            if (item.UnitPrice is null)
            {
                missing = true;
                continue;
            }

            cost += item.Quantity * item.UnitPrice.Value;
        }

        var items = visible.Select(item =>
        {
            var source = meal.Items.First(i => i.Id == item.Id);
            decimal? line = item.UnitPrice is null ? null : decimal.Round(item.Quantity * item.UnitPrice.Value, 2);
            return new MealItemDto(
                item.Id,
                item.FoodId,
                item.Name,
                source.Food?.LocalName,
                item.Quantity,
                item.Unit,
                item.IsOptional,
                item.AlternativeGroup,
                item.IsDefaultAlternative,
                item.Note,
                line,
                source.Food is null ? null : decimal.Round(source.Food.Calories * item.Quantity, 1),
                source.Food is null ? null : decimal.Round(source.Food.ProteinG * item.Quantity, 1));
        }).ToList();

        return new MealCardDto(
            meal.Id,
            meal.MealType.ToString(),
            meal.Title,
            Parsers.Clock(meal.ScheduledTime),
            meal.Notes,
            decimal.Round(cost, 2),
            missing,
            logStatus,
            items);
    }

    public static IReadOnlyList<PlanItem> ShoppingItems(
        Meal meal,
        DietaryPreference preference,
        IReadOnlyCollection<string> allergies,
        IReadOnlyDictionary<Guid, decimal> prices)
    {
        var planItems = meal.Items.Select(item =>
        {
            decimal? price = null;
            if (item.FoodId is Guid foodId)
            {
                price = prices.TryGetValue(foodId, out var custom) ? custom : item.Food?.ReferencePriceInr;
            }

            return new PlanItem(
                item.Id,
                item.FoodId,
                item.Food?.Name ?? item.DisplayName,
                item.Quantity,
                item.Unit,
                item.IsOptional,
                item.AlternativeGroup,
                item.IsDefaultAlternative,
                item.SortOrder,
                item.Food?.SuitableFor ?? DietFlags.All,
                price,
                item.Note,
                Parsers.Grocery(item.Food?.GroceryCategory ?? GroceryCategory.Other),
                item.Food?.Allergens);
        }).ToList();

        return DietFilter.SelectForShopping(planItems, preference, allergies)
            .Where(i => i.FoodId is not null)
            .ToList();
    }
}

public static class JwtIssuer
{
    public static (string Token, DateTime Expires) CreateAccessToken(ApplicationUser user, IList<string> roles, JwtOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.SigningKey) || options.SigningKey.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must be configured and at least 32 characters.");
        }

        var expires = DateTime.UtcNow.AddMinutes(Math.Clamp(options.AccessTokenMinutes, 5, 120));
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}

public static class AccountFactory
{
    public static Entitlement FreeEntitlement(Guid userId) => new()
    {
        UserId = userId,
        Plan = EntitlementPlan.Free,
        Status = EntitlementStatus.Active,
        StartsAtUtc = DateTime.UtcNow,
        Source = "system"
    };

    public static IReadOnlyList<string> FeaturesFor(EntitlementPlan plan) => plan switch
    {
        EntitlementPlan.Premium => ["plans", "tracking", "budget", "shopping", "progress", "reminders", "history-export-later"],
        EntitlementPlan.Nutritionist => ["plans", "tracking", "budget", "shopping", "progress", "reminders", "provider-tools"],
        _ => ["plans", "tracking", "budget", "shopping", "progress", "reminders"]
    };
}
