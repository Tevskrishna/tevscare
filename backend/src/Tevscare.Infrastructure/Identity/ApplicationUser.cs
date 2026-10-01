using Microsoft.AspNetCore.Identity;

namespace Tevscare.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public string Timezone { get; set; } = "Asia/Kolkata";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}
