namespace Tevscare.Domain.Identity;

public static class Roles
{
    public const string User = "USER";
    public const string Nutritionist = "NUTRITIONIST";
    public const string Admin = "ADMIN";

    public static readonly string[] All = [User, Nutritionist, Admin];
}

public static class ProductDisclaimer
{
    public const string Text =
        "TEVSCARE provides meal-planning and habit-tracking information. It is not a substitute for medical diagnosis or treatment. Dietary restrictions should be confirmed with a qualified healthcare professional.";
}
