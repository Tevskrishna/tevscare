using Tevscare.Application.Planning;
using Tevscare.Domain.Enums;

namespace Tevscare.UnitTests;

public class CalculatorTests
{
    [Fact]
    public void PlanDay_resolves_today_inside_the_plan()
    {
        var start = new DateOnly(2026, 10, 1);
        var state = PlanDayResolver.Resolve(start, new DateOnly(2026, 10, 3), 15);
        Assert.Equal(3, state.DayNumber);
        Assert.False(state.NotStarted);
        Assert.False(state.Completed);
    }

    [Fact]
    public void PlanDay_marks_the_plan_complete_after_the_last_day()
    {
        var state = PlanDayResolver.Resolve(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 20), 15);
        Assert.True(state.Completed);
        Assert.Equal(15, state.DayNumber);
    }

    [Fact]
    public void DietFilter_keeps_eggs_for_eggetarian_and_peanuts_for_vegan()
    {
        var items = new List<PlanItem>
        {
            Item("almonds", DietFlags.All, null, false),
            Item("eggs", DietFlags.EggetarianAndAbove, "breakfast-protein", true, "egg"),
            Item("peanuts", DietFlags.All, "breakfast-protein", true, "peanuts"),
            Item("sprouts", DietFlags.All, "breakfast-protein", false)
        };

        var eggetarian = DietFilter.SelectForShopping(items, DietaryPreference.Eggetarian);
        Assert.Contains(eggetarian, item => item.Name == "eggs");
        Assert.DoesNotContain(eggetarian, item => item.Name == "peanuts");

        var vegan = DietFilter.SelectForShopping(items, DietaryPreference.Vegan);
        Assert.Contains(vegan, item => item.Name == "peanuts");
        Assert.DoesNotContain(vegan, item => item.Name == "eggs");
    }

    [Fact]
    public void DietFilter_removes_a_matching_allergen()
    {
        var items = new List<PlanItem> { Item("peanuts", DietFlags.All, "breakfast-protein", true, "peanuts"), Item("sprouts", DietFlags.All, "breakfast-protein", false) };
        var selected = DietFilter.SelectForShopping(items, DietaryPreference.Vegan, ["peanuts"]);
        Assert.Equal("sprouts", Assert.Single(selected).Name);
    }

    [Theory]
    [InlineData(70, 2450)]
    [InlineData(40, 2000)]
    [InlineData(120, 3500)]
    public void Water_suggestion_stays_inside_the_planning_range(int weight, int expected)
    {
        Assert.Equal(expected, WaterCalculator.SuggestGoalMl(weight));
    }

    [Fact]
    public void Water_progress_reports_remaining_and_percent()
    {
        var progress = WaterCalculator.Progress(2000, 500);
        Assert.Equal(1500, progress.RemainingMl);
        Assert.Equal(25, progress.Percent);
    }

    [Fact]
    public void Adherence_uses_the_published_weights()
    {
        var result = AdherenceCalculator.Calculate(5, 5, true, true, true, true);
        Assert.Equal(100, result.Score);
        var half = AdherenceCalculator.Calculate(5, 2.5m, false, false, false, false);
        Assert.Equal(20, half.Score);
    }

    [Fact]
    public void Budget_projects_the_plan_and_the_month()
    {
        var result = BudgetCalculator.Calculate(675, "INR",
        [
            ("Breakfast", 65, 65),
            ("Lunch", 120, 0)
        ], 15);

        Assert.Equal(185, result.PlannedToday);
        Assert.Equal(65, result.Spent);
        Assert.Equal(610, result.Remaining);
        Assert.Equal(2775, result.ProjectedPeriod);
        Assert.Equal(5550, result.ProjectedMonthly);
    }

    [Fact]
    public void Shopping_list_sums_quantities_and_known_prices()
    {
        var food = Guid.NewGuid();
        var built = ShoppingListCalculator.Build(
        [
            new ShoppingInputLine(food, "Idly", "Grains", 2, "piece", 10),
            new ShoppingInputLine(food, "Idly", "Grains", 2, "piece", 10)
        ], new Dictionary<Guid, bool> { [food] = true });

        var line = Assert.Single(built.Lines);
        Assert.Equal(4, line.Quantity);
        Assert.Equal(40, line.EstimatedCost);
        Assert.True(line.Purchased);
        Assert.Equal(40, built.TotalKnownCost);
        Assert.False(built.HasMissingPrices);
    }

    [Fact]
    public void Weight_change_is_signed_and_progress_does_not_go_below_zero()
    {
        var points = new List<WeightPoint>
        {
            new(new DateOnly(2026, 9, 1), 72),
            new(new DateOnly(2026, 10, 1), 70)
        };
        Assert.Equal(-2, WeightTrendCalculator.Change(points, 30));
        Assert.Equal(0, WeightTrendCalculator.ProgressPercent(74, 72, 65));
        Assert.Equal(50, WeightTrendCalculator.ProgressPercent(70, 72, 68));
    }

    [Fact]
    public void Notification_schedule_skips_quiet_hours_and_repeats_water()
    {
        var schedule = NotificationScheduleBuilder.Build(
        [
            new("MorningWeight", true, new TimeOnly(6, 30), null, null, null),
            new("Water", true, null, 60, new TimeOnly(8, 0), new TimeOnly(10, 0)),
            new("Sleep", true, new TimeOnly(21, 30), null, null, null)
        ], new QuietHours(true, new TimeOnly(22, 0), new TimeOnly(7, 0)));

        Assert.DoesNotContain(schedule, item => item.Category == "MorningWeight");
        Assert.Equal(3, schedule.Count(item => item.Category == "Water"));
        Assert.Contains(schedule, item => item.Category == "Sleep" && item.Body.Contains("winding down"));
    }

    [Fact]
    public void Breakfast_rotation_matches_the_source_plan()
    {
        var rotation = new[]
        {
            "2 idly", "2 dosa", "Ragi java", "2 pesar dosa", "2 idly", "2 dosa", "Ragi java",
            "2 idly", "2 pesar dosa", "2 dosa", "Ragi java", "2 idly", "2 pesar dosa", "2 dosa", "Ragi java"
        };
        Assert.Equal(15, rotation.Length);
        Assert.Equal("2 idly", rotation[0]);
        Assert.Equal("Ragi java", rotation[14]);
        Assert.Equal(4, rotation.Count(item => item == "2 idly"));
        Assert.Equal(4, rotation.Count(item => item == "2 dosa"));
        Assert.Equal(3, rotation.Count(item => item == "2 pesar dosa"));
        Assert.Equal(4, rotation.Count(item => item.StartsWith("Ragi")));
    }

    private static PlanItem Item(string name, DietFlags flags, string? group, bool isDefault, string? allergens = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), name, 1, "serving", false, group, isDefault, 0, flags, 10, null, "Other", allergens);
}
