using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace ClinicApp.Web.Data;

public static class SeedData
{
    public static class Capabilities
    {
        public const string ViewDoctors = "VIEW_DOCTORS";
        public const string InsertDoctor = "INSERT_DOCTOR";
        public const string EditDoctor = "EDIT_DOCTOR";
        public const string DeleteDoctor = "DELETE_DOCTOR";

        public static readonly string[] All =
        [
            ViewDoctors,
            InsertDoctor,
            EditDoctor,
            DeleteDoctor
        ];
    }

    public const string CapabilityClaimType = "capability";

    public static async Task EnsureSeededAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var adminRole = await EnsureRoleAsync(roleManager, "ADMIN");
        var employeeRole = await EnsureRoleAsync(roleManager, "EMPLOYEE");

        await EnsureRoleCapabilitiesAsync(roleManager, adminRole, Capabilities.All);
        await EnsureRoleCapabilitiesAsync(roleManager, employeeRole,
        [
            Capabilities.ViewDoctors,
            Capabilities.InsertDoctor,
            Capabilities.EditDoctor
        ]);

        // Create a default admin for local/dev (username: admin, password: Admin123!)
        var adminUser = await userManager.FindByNameAsync("admin");
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@local",
                EmailConfirmed = true
            };

            var created = await userManager.CreateAsync(adminUser, "Admin123!");
            if (created.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, adminRole.Name!);
            }
        }
    }

    private static async Task<IdentityRole<Guid>> EnsureRoleAsync(RoleManager<IdentityRole<Guid>> roleManager, string roleName)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role is not null) return role;

        role = new IdentityRole<Guid>(roleName);
        var created = await roleManager.CreateAsync(role);
        if (!created.Succeeded)
        {
            var msg = string.Join("; ", created.Errors.Select(e => $"{e.Code}: {e.Description}"));
            throw new InvalidOperationException($"Failed creating role '{roleName}': {msg}");
        }
        return role;
    }

    private static async Task EnsureRoleCapabilitiesAsync(
        RoleManager<IdentityRole<Guid>> roleManager,
        IdentityRole<Guid> role,
        IEnumerable<string> capabilities)
    {
        var existing = await roleManager.GetClaimsAsync(role);
        foreach (var cap in capabilities)
        {
            if (existing.Any(c => c.Type == CapabilityClaimType && c.Value == cap)) continue;
            await roleManager.AddClaimAsync(role, new Claim(CapabilityClaimType, cap));
        }
    }
}

