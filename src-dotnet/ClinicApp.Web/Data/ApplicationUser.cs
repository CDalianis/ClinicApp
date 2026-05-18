using Microsoft.AspNetCore.Identity;

namespace ClinicApp.Web.Data;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
}

