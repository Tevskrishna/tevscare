using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tevscare.Application.Abstractions;
using Tevscare.Application.Contracts;
using Tevscare.Domain.Identity;

namespace Tevscare.Api.Controllers;

[ApiController]
[Authorize(Roles = $"{Roles.Admin},{Roles.Nutritionist}")]
[Route("api/admin")]
public class AdminDirectoryController : ControllerBase
{
    private readonly IAdminDirectoryService _directory;
    private readonly ICurrentUser _current;

    public AdminDirectoryController(IAdminDirectoryService directory, ICurrentUser current)
    {
        _directory = directory;
        _current = current;
    }

    [HttpGet("dashboard")]
    public Task<AdminDashboardResponse> Dashboard(CancellationToken cancellationToken) =>
        _directory.DashboardAsync(cancellationToken);

    [HttpGet("plans")]
    public Task<IReadOnlyList<AdminPlanDto>> Plans(CancellationToken cancellationToken) =>
        _directory.PlansAsync(cancellationToken);

    [HttpGet("guidance")]
    public Task<IReadOnlyList<GuidanceDto>> Guidance(CancellationToken cancellationToken) =>
        _directory.GuidanceAsync(cancellationToken);

    [Authorize(Roles = Roles.Admin)]
    [HttpGet("users")]
    public Task<AdminUserPage> Users([FromQuery] string? query, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        _directory.UsersAsync(query, page, pageSize, cancellationToken);

    [Authorize(Roles = Roles.Admin)]
    [HttpGet("users/{id:guid}")]
    public Task<AdminUserDetail> GetUser(Guid id, CancellationToken cancellationToken) =>
        _directory.UserAsync(id, cancellationToken);

    [Authorize(Roles = Roles.Admin)]
    [HttpPost("users/{id:guid}/lock")]
    public async Task<IActionResult> Lock(Guid id, LockUserRequest request, CancellationToken cancellationToken)
    {
        await _directory.LockAsync(_current.UserId, id, request.Locked, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost("nutritionists")]
    public Task<AdminUserListItem> CreateNutritionist(CreateNutritionistRequest request, CancellationToken cancellationToken) =>
        _directory.CreateNutritionistAsync(_current.UserId, request, cancellationToken);

    [Authorize(Roles = Roles.Admin)]
    [HttpGet("audit")]
    public Task<AdminAuditPage> Audit([FromQuery] int page = 1, [FromQuery] int pageSize = 30, CancellationToken cancellationToken = default) =>
        _directory.AuditAsync(page, pageSize, cancellationToken);
}
