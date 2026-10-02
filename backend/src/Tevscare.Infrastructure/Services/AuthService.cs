using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tevscare.Application.Abstractions;
using Tevscare.Application.Common;
using Tevscare.Application.Contracts;
using Tevscare.Domain.Account;
using Tevscare.Domain.Identity;
using Tevscare.Infrastructure.Identity;
using Tevscare.Infrastructure.Options;
using Tevscare.Infrastructure.Persistence;

namespace Tevscare.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly AppDbContext _db;
    private readonly JwtOptions _jwt;
    private readonly TevscareOptions _options;
    private readonly IEmailSender _email;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> users,
        AppDbContext db,
        IOptions<JwtOptions> jwt,
        IOptions<TevscareOptions> options,
        IEmailSender email,
        ILogger<AuthService> logger)
    {
        _users = users;
        _db = db;
        _jwt = jwt.Value;
        _options = options.Value;
        _email = email;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
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
            Timezone = string.IsNullOrWhiteSpace(request.Timezone) ? "Asia/Kolkata" : request.Timezone.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        var created = await _users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            throw new AppException(string.Join(" ", created.Errors.Select(error => error.Description)));
        }

        await _users.AddToRoleAsync(user, Roles.User);
        _db.Profiles.Add(new UserProfile { UserId = user.Id });
        _db.Budgets.Add(new BudgetSettings { UserId = user.Id, DailyBudgetAmount = 675, Currency = "INR" });
        _db.NotificationSettings.Add(NotificationDefaults.Create(user.Id));
        _db.Entitlements.Add(AccountFactory.FreeEntitlement(user.Id));
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("User registered");
        return await IssueAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _users.FindByEmailAsync(email);
        if (user is null || user.DeletedAtUtc is not null)
        {
            throw new AppException("Email or password is incorrect.", 401);
        }

        if (await _users.IsLockedOutAsync(user))
        {
            throw new AppException("This account is temporarily locked. Try again later.", 423);
        }

        if (!await _users.CheckPasswordAsync(user, request.Password))
        {
            await _users.AccessFailedAsync(user);
            throw new AppException("Email or password is incorrect.", 401);
        }

        await _users.ResetAccessFailedCountAsync(user);
        return await IssueAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        var hash = TokenCodec.Hash(request.RefreshToken);
        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(token => token.TokenHash == hash, cancellationToken);
        if (existing is null || existing.RevokedAtUtc is not null || existing.ExpiresAtUtc <= DateTime.UtcNow)
        {
            throw AppException.Unauthorized("Your session has expired. Sign in again.");
        }

        var user = await _users.FindByIdAsync(existing.UserId.ToString());
        if (user is null || user.DeletedAtUtc is not null)
        {
            throw AppException.Unauthorized("Your session has expired. Sign in again.");
        }

        existing.RevokedAtUtc = DateTime.UtcNow;
        var response = await IssueAsync(user, cancellationToken);
        existing.ReplacedByTokenHash = TokenCodec.Hash(response.RefreshToken);
        await _db.SaveChangesAsync(cancellationToken);
        return response;
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        var hash = TokenCodec.Hash(request.RefreshToken);
        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(token => token.TokenHash == hash, cancellationToken);
        if (existing is not null && existing.RevokedAtUtc is null)
        {
            existing.RevokedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<MessageResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        const string message = "If an account exists for that email, password reset instructions have been created.";
        var user = await _users.FindByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user is null || user.DeletedAtUtc is not null)
        {
            return new MessageResponse(message);
        }

        var token = TokenCodec.NewToken();
        _db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = TokenCodec.Hash(token),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30)
        });
        await _db.SaveChangesAsync(cancellationToken);
        await _email.SendAsync(
            user.Email!,
            "Reset your TEVSCARE password",
            $"Use this reset token within 30 minutes: {token}\n\nPowered by TEVS",
            cancellationToken);

        return new MessageResponse(message, _options.ExposeResetTokens ? token : null);
    }

    public async Task<MessageResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await _users.FindByEmailAsync(request.Email.Trim().ToLowerInvariant());
        var hash = TokenCodec.Hash(request.Token);
        var reset = user is null
            ? null
            : await _db.PasswordResetTokens.FirstOrDefaultAsync(
                token => token.UserId == user.Id && token.TokenHash == hash && token.UsedAtUtc == null,
                cancellationToken);

        if (user is null || reset is null || reset.ExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new AppException("This reset link is invalid or has expired.");
        }

        var passwordResult = await _users.RemovePasswordAsync(user);
        if (!passwordResult.Succeeded)
        {
            throw new AppException("The password could not be reset.");
        }

        passwordResult = await _users.AddPasswordAsync(user, request.NewPassword);
        if (!passwordResult.Succeeded)
        {
            throw new AppException(string.Join(" ", passwordResult.Errors.Select(error => error.Description)));
        }

        reset.UsedAtUtc = DateTime.UtcNow;
        var tokens = await _db.RefreshTokens.Where(token => token.UserId == user.Id && token.RevokedAtUtc == null).ToListAsync(cancellationToken);
        foreach (var token in tokens)
        {
            token.RevokedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new MessageResponse("Your password has been updated. Sign in with the new password.");
    }

    private async Task<AuthResponse> IssueAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await _users.GetRolesAsync(user);
        var (access, accessExpires) = JwtIssuer.CreateAccessToken(user, roles, _jwt);
        var refresh = TokenCodec.NewToken();
        var refreshExpires = DateTime.UtcNow.AddDays(Math.Clamp(_jwt.RefreshTokenDays, 1, 60));
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = TokenCodec.Hash(refresh),
            ExpiresAtUtc = refreshExpires
        });
        await _db.SaveChangesAsync(cancellationToken);

        var profile = await _db.Profiles.AsNoTracking().FirstOrDefaultAsync(item => item.UserId == user.Id, cancellationToken);
        var entitlement = await _db.Entitlements.AsNoTracking()
            .Where(item => item.UserId == user.Id && item.Status == Domain.Enums.EntitlementStatus.Active)
            .OrderByDescending(item => item.StartsAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return new AuthResponse(
            access,
            accessExpires,
            refresh,
            refreshExpires,
            new UserSummary(
                user.Id,
                user.FullName,
                user.Email ?? string.Empty,
                user.Timezone,
                roles.ToList(),
                profile?.OnboardingCompleted ?? false,
                entitlement?.Plan.ToString() ?? "Free"));
    }
}

public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger) => _logger = logger;

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email queued with subject {Subject}. Body is not logged.", subject);
        return Task.CompletedTask;
    }
}
