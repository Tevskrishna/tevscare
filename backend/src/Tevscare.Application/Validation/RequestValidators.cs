using FluentValidation;
using Tevscare.Application.Contracts;
using Tevscare.Application.Planning;

namespace Tevscare.Application.Validation;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(100)
            .Matches("[A-Z]").WithMessage("Password must include an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must include a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must include a number.");
        RuleFor(x => x.Timezone).MaximumLength(80);
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
}

public class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    public LogoutRequestValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
}

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator() => RuleFor(x => x.Email).NotEmpty().EmailAddress();
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(100)
            .Matches("[A-Z]").Matches("[a-z]").Matches("[0-9]");
    }
}

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Age).InclusiveBetween(18, 100);
        RuleFor(x => x.HeightCm).InclusiveBetween(120, 230);
        RuleFor(x => x.CurrentWeightKg).InclusiveBetween(30, 250);
        RuleFor(x => x.TargetWeightKg).InclusiveBetween(30, 250);
        RuleFor(x => x.DietaryPreference).NotEmpty();
        RuleFor(x => x.ActivityLevel).NotEmpty();
        RuleFor(x => x.WaterGoalMl).InclusiveBetween(WaterCalculator.MinGoalMl, WaterCalculator.MaxGoalMl);
        RuleFor(x => x.SleepGoalMinutes).InclusiveBetween(240, 600);
        RuleFor(x => x.ActivityGoalMinutes).InclusiveBetween(10, 240);
        RuleFor(x => x.Timezone).NotEmpty().MaximumLength(80);
        RuleForEach(x => x.Allergies).MaximumLength(80);
        RuleForEach(x => x.FoodPreferences).ChildRules(pref =>
        {
            pref.RuleFor(p => p.Name).NotEmpty().MaximumLength(80);
            pref.RuleFor(p => p.Kind).NotEmpty();
        });
    }
}

public class LogWaterRequestValidator : AbstractValidator<LogWaterRequest>
{
    public LogWaterRequestValidator() => RuleFor(x => x.AmountMl).InclusiveBetween(50, 1500);
}

public class LogWeightRequestValidator : AbstractValidator<LogWeightRequest>
{
    public LogWeightRequestValidator()
    {
        RuleFor(x => x.WeightKg).InclusiveBetween(30, 250);
        RuleFor(x => x.Note).MaximumLength(300);
    }
}

public class LogMealRequestValidator : AbstractValidator<LogMealRequest>
{
    public LogMealRequestValidator()
    {
        RuleFor(x => x.MealType).NotEmpty();
        RuleFor(x => x.Status).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Name).NotEmpty().MaximumLength(120);
            item.RuleFor(i => i.Quantity).GreaterThan(0).LessThanOrEqualTo(20);
            item.RuleFor(i => i.Unit).NotEmpty().MaximumLength(40);
        });
    }
}

public class LogActivityRequestValidator : AbstractValidator<LogActivityRequest>
{
    public LogActivityRequestValidator()
    {
        RuleFor(x => x.ActivityType).NotEmpty().MaximumLength(80);
        RuleFor(x => x.DurationMinutes).InclusiveBetween(1, 300);
        RuleFor(x => x.Notes).MaximumLength(300);
    }
}

public class LogSleepRequestValidator : AbstractValidator<LogSleepRequest>
{
    public LogSleepRequestValidator()
    {
        RuleFor(x => x.DurationMinutes).InclusiveBetween(60, 720);
        RuleFor(x => x.Quality).InclusiveBetween(1, 5).When(x => x.Quality.HasValue);
        RuleFor(x => x.Notes).MaximumLength(300);
    }
}

public class CheckInRequestValidator : AbstractValidator<CheckInRequest>
{
    public CheckInRequestValidator()
    {
        RuleFor(x => x.Mood).InclusiveBetween(1, 5).When(x => x.Mood.HasValue);
        RuleFor(x => x.Energy).InclusiveBetween(1, 5).When(x => x.Energy.HasValue);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public class UpsertBudgetRequestValidator : AbstractValidator<UpsertBudgetRequest>
{
    public UpsertBudgetRequestValidator()
    {
        RuleFor(x => x.DailyBudgetAmount).InclusiveBetween(50, 20000);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}

public class UpsertFoodPriceRequestValidator : AbstractValidator<UpsertFoodPriceRequest>
{
    public UpsertFoodPriceRequestValidator()
    {
        RuleFor(x => x.FoodId).NotEmpty();
        RuleFor(x => x.PricePerServing).InclusiveBetween(0, 10000);
    }
}

public class ShoppingToggleRequestValidator : AbstractValidator<ShoppingToggleRequest>
{
    public ShoppingToggleRequestValidator()
    {
        RuleFor(x => x.FoodId).NotEmpty();
        RuleFor(x => x.RangeDays).Must(d => d is 1 or 7 or 15 or 30);
    }
}

public class UpdateNotificationSettingsRequestValidator : AbstractValidator<UpdateNotificationSettingsRequest>
{
    public UpdateNotificationSettingsRequestValidator()
    {
        RuleFor(x => x.QuietStart).NotEmpty();
        RuleFor(x => x.QuietEnd).NotEmpty();
        RuleFor(x => x.Preferences).NotNull();
        RuleForEach(x => x.Preferences).ChildRules(pref =>
        {
            pref.RuleFor(p => p.Category).NotEmpty();
            pref.RuleFor(p => p.IntervalMinutes).InclusiveBetween(30, 240).When(p => p.IntervalMinutes.HasValue);
        });
    }
}

public class AnalyticsEventRequestValidator : AbstractValidator<AnalyticsEventRequest>
{
    private static readonly HashSet<string> Allowed =
    [
        "app_opened",
        "onboarding_completed",
        "meal_viewed",
        "meal_completed",
        "water_logged",
        "weight_logged",
        "plan_completed",
        "notification_opened"
    ];

    public AnalyticsEventRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().Must(Allowed.Contains).WithMessage("Unknown analytics event.");
        RuleFor(x => x.PropertiesJson).MaximumLength(500);
    }
}

public class CreateDietPlanRequestValidator : AbstractValidator<CreateDietPlanRequest>
{
    public CreateDietPlanRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.DurationDays).InclusiveBetween(1, 90);
    }
}

public class UpdateMealRequestValidator : AbstractValidator<UpdateMealRequest>
{
    public UpdateMealRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(160);
        RuleFor(x => x.ScheduledTime).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.DisplayName).NotEmpty().MaximumLength(160);
            item.RuleFor(i => i.Quantity).GreaterThan(0).LessThanOrEqualTo(50);
            item.RuleFor(i => i.Unit).NotEmpty().MaximumLength(40);
        });
    }
}

public class UpsertFoodRequestValidator : AbstractValidator<UpsertFoodRequest>
{
    public UpsertFoodRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.ServingLabel).NotEmpty().MaximumLength(80);
        RuleFor(x => x.ServingQuantity).GreaterThan(0);
        RuleFor(x => x.Calories).InclusiveBetween(0, 2000);
        RuleFor(x => x.ProteinG).InclusiveBetween(0, 200);
        RuleFor(x => x.CarbohydratesG).InclusiveBetween(0, 300);
        RuleFor(x => x.FatG).InclusiveBetween(0, 200);
        RuleFor(x => x.FibreG).InclusiveBetween(0, 100);
        RuleFor(x => x.GroceryCategory).NotEmpty();
        RuleFor(x => x.SuitableFor).NotEmpty();
    }
}

public class AssignPlanRequestValidator : AbstractValidator<AssignPlanRequest>
{
    public AssignPlanRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.DietPlanId).NotEmpty();
        RuleFor(x => x.StartDate).NotEmpty();
    }
}
