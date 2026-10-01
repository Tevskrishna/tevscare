namespace Tevscare.Infrastructure.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "tevscare";
    public string Audience { get; set; } = "tevscare-mobile";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 14;
}

public class TevscareOptions
{
    public const string SectionName = "Tevscare";
    public bool ExposeResetTokens { get; set; }
    public bool SeedDemoData { get; set; }
}
