using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tevscare.Application.Abstractions;
using Tevscare.Infrastructure.Identity;
using Tevscare.Infrastructure.Options;
using Tevscare.Infrastructure.Persistence;
using Tevscare.Infrastructure.Services;

namespace Tevscare.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<TevscareOptions>(configuration.GetSection(TevscareOptions.SectionName));

        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:Default is not configured. Copy .env.example to .env and start PostgreSQL.");
        }

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            if (string.Equals(configuration["Tevscare:DatabaseProvider"], "Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlite(connectionString);
            }
            else
            {
                options.UseNpgsql(connectionString);
            }

            options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
        });
        services.AddScoped<AuditInterceptor>();

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<PlanExperienceService>();
        services.AddScoped<IDashboardService>(sp => sp.GetRequiredService<PlanExperienceService>());
        services.AddScoped<IDietPlanService>(sp => sp.GetRequiredService<PlanExperienceService>());
        services.AddScoped<IMealLogService>(sp => sp.GetRequiredService<PlanExperienceService>());
        services.AddScoped<IBudgetService>(sp => sp.GetRequiredService<PlanExperienceService>());
        services.AddScoped<IShoppingListService>(sp => sp.GetRequiredService<PlanExperienceService>());
        services.AddScoped<TrackingExperienceService>();
        services.AddScoped<IWaterService>(sp => sp.GetRequiredService<TrackingExperienceService>());
        services.AddScoped<IWeightService>(sp => sp.GetRequiredService<TrackingExperienceService>());
        services.AddScoped<IActivityService>(sp => sp.GetRequiredService<TrackingExperienceService>());
        services.AddScoped<ISleepService>(sp => sp.GetRequiredService<TrackingExperienceService>());
        services.AddScoped<ICheckInService>(sp => sp.GetRequiredService<TrackingExperienceService>());
        services.AddScoped<IProgressService>(sp => sp.GetRequiredService<TrackingExperienceService>());
        services.AddScoped<ContentService>();
        services.AddScoped<IFoodService>(sp => sp.GetRequiredService<ContentService>());
        services.AddScoped<INotificationPreferenceService>(sp => sp.GetRequiredService<ContentService>());
        services.AddScoped<IGuidanceService>(sp => sp.GetRequiredService<ContentService>());
        services.AddScoped<IEntitlementService>(sp => sp.GetRequiredService<ContentService>());
        services.AddScoped<IAnalyticsService>(sp => sp.GetRequiredService<ContentService>());
        services.AddScoped<IAdminContentService>(sp => sp.GetRequiredService<ContentService>());
        services.AddScoped<IAdminDirectoryService, AdminDirectoryService>();
        services.AddScoped<IEmailSender, LoggingEmailSender>();
        services.AddSingleton<IPushNotificationSender, NoRemotePushSender>();
        return services;
    }
}
