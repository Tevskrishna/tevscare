namespace Tevscare.Domain.Enums;

public enum DietaryPreference
{
    Vegetarian = 0,
    Eggetarian = 1,
    NonVegetarian = 2,
    Vegan = 3
}

public enum ActivityLevel
{
    Sedentary = 0,
    Light = 1,
    Moderate = 2,
    Active = 3,
    VeryActive = 4
}

public enum MealType
{
    Breakfast = 0,
    MidMorning = 1,
    Lunch = 2,
    AfternoonSnack = 3,
    Dinner = 4
}

public enum MealLogStatus
{
    Completed = 0,
    Partial = 1,
    Skipped = 2
}

public enum GuidanceCategory
{
    General = 0,
    Hydration = 1,
    MealHabit = 2,
    Activity = 3,
    Sleep = 4,
    Restriction = 5
}

public enum NotificationCategory
{
    MorningWeight = 0,
    Breakfast = 1,
    Water = 2,
    Lunch = 3,
    Activity = 4,
    Dinner = 5,
    Sleep = 6,
    AfternoonSnack = 7,
    CheckIn = 8
}

public enum EntitlementPlan
{
    Free = 0,
    Premium = 1,
    Nutritionist = 2,
    Trial = 3
}

public enum EntitlementStatus
{
    Active = 0,
    Expired = 1,
    Cancelled = 2
}

public enum PlanAssignmentStatus
{
    Active = 0,
    Completed = 1,
    Paused = 2
}

public enum GroceryCategory
{
    Vegetables = 0,
    Fruits = 1,
    Grains = 2,
    Pulses = 3,
    Dairy = 4,
    Protein = 5,
    NutsAndSeeds = 6,
    Other = 7
}

[Flags]
public enum DietFlags
{
    None = 0,
    Vegan = 1,
    Vegetarian = 2,
    Eggetarian = 4,
    NonVegetarian = 8,
    All = Vegan | Vegetarian | Eggetarian | NonVegetarian,
    VegetarianAndAbove = Vegetarian | Eggetarian | NonVegetarian,
    EggetarianAndAbove = Eggetarian | NonVegetarian
}

public enum FoodPreferenceKind
{
    Like = 0,
    Dislike = 1,
    Avoid = 2
}
