using Tevscare.Domain.Enums;

namespace Tevscare.Application.Planning;

public readonly record struct PlanDayState(int DayNumber, bool NotStarted, bool Completed);

public static class PlanDayResolver
{
    public static PlanDayState Resolve(DateOnly start, DateOnly today, int durationDays)
    {
        var duration = Math.Max(1, durationDays);
        var index = today.DayNumber - start.DayNumber + 1;
        if (index < 1)
        {
            return new PlanDayState(1, true, false);
        }

        if (index > duration)
        {
            return new PlanDayState(duration, false, true);
        }

        return new PlanDayState(index, false, false);
    }
}

public static class DietFilter
{
    public static DietFlags RequiredFlag(DietaryPreference preference) => preference switch
    {
        DietaryPreference.Vegan => DietFlags.Vegan,
        DietaryPreference.Vegetarian => DietFlags.Vegetarian,
        DietaryPreference.Eggetarian => DietFlags.Eggetarian,
        _ => DietFlags.NonVegetarian
    };

    public static bool Allows(DietFlags foodFlags, DietaryPreference preference) =>
        foodFlags.HasFlag(RequiredFlag(preference));

    public static bool BlockedByAllergy(string? allergens, IReadOnlyCollection<string> allergies)
    {
        if (string.IsNullOrWhiteSpace(allergens) || allergies.Count == 0)
        {
            return false;
        }

        var parts = allergens.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Any(part => allergies.Any(a => string.Equals(a.Trim(), part, StringComparison.OrdinalIgnoreCase)));
    }

    public static IReadOnlyList<PlanItem> SelectForDiet(
        IReadOnlyList<PlanItem> items,
        DietaryPreference preference,
        IReadOnlyCollection<string>? allergies = null)
    {
        var allergyList = allergies ?? Array.Empty<string>();
        var result = new List<PlanItem>();
        foreach (var group in items.GroupBy(i => i.AlternativeGroup ?? $"__solo_{i.Id}"))
        {
            var allowed = group
                .Where(i => Allows(i.SuitableFor, preference) && !BlockedByAllergy(i.Allergens, allergyList))
                .ToList();
            if (allowed.Count == 0)
            {
                continue;
            }

            if (group.Key.StartsWith("__solo_", StringComparison.Ordinal))
            {
                result.Add(allowed[0]);
                continue;
            }

            result.AddRange(allowed.OrderByDescending(i => i.IsDefaultAlternative).ThenBy(i => i.SortOrder));
        }

        return result.OrderBy(i => i.SortOrder).ToList();
    }

    public static IReadOnlyList<PlanItem> SelectForShopping(
        IReadOnlyList<PlanItem> items,
        DietaryPreference preference,
        IReadOnlyCollection<string>? allergies = null)
    {
        var visible = SelectForDiet(items, preference, allergies);
        var result = new List<PlanItem>();
        foreach (var group in visible.GroupBy(i => i.AlternativeGroup ?? $"__solo_{i.Id}"))
        {
            if (group.Key.StartsWith("__solo_", StringComparison.Ordinal))
            {
                result.Add(group.First());
                continue;
            }

            result.Add(group.FirstOrDefault(i => i.IsDefaultAlternative) ?? group.First());
        }

        return result;
    }
}

public sealed record PlanItem(
    Guid Id,
    Guid? FoodId,
    string Name,
    decimal Quantity,
    string Unit,
    bool IsOptional,
    string? AlternativeGroup,
    bool IsDefaultAlternative,
    int SortOrder,
    DietFlags SuitableFor,
    decimal? UnitPrice,
    string? Note,
    string GroceryCategory,
    string? Allergens);
