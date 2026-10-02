using Tevscare.Domain.Common;

namespace Tevscare.Domain.Account;

public class AdminAuditEntry : AuditableEntity
{
    public Guid ActorUserId { get; set; }
    public string ActorRole { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Detail { get; set; }
}
