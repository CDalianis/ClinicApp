using ClinicApp.Web.ViewModels.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClinicApp.Web.Controllers;

[Route("users")]
public sealed class UsersController : Controller
{
    private readonly UserManager<Data.ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public UsersController(UserManager<Data.ApplicationUser> userManager, RoleManager<IdentityRole<Guid>> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register()
    {
        await PopulateRolesAsync();
        return View(new RegisterUserViewModel());
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterUserViewModel vm)
    {
        await PopulateRolesAsync();

        if (!ModelState.IsValid) return View(vm);

        if (await _userManager.FindByNameAsync(vm.Username) is not null)
        {
            ModelState.AddModelError(nameof(vm.Username), $"User '{vm.Username}' already exists");
            return View(vm);
        }

        var user = new Data.ApplicationUser
        {
            UserName = vm.Username,
            Email = $"{vm.Username}@local",
            EmailConfirmed = true
        };

        var created = await _userManager.CreateAsync(user, vm.Password);
        if (!created.Succeeded)
        {
            foreach (var err in created.Errors) ModelState.AddModelError(string.Empty, err.Description);
            return View(vm);
        }

        var roleName = NormalizeRequestedRole(vm.RoleName);
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            roleName = "EMPLOYEE";
        }

        await _userManager.AddToRoleAsync(user, roleName);

        TempData["RegisteredUserUuid"] = user.Uuid.ToString();
        return RedirectToAction(nameof(Success));
    }

    [HttpGet("success")]
    [AllowAnonymous]
    public IActionResult Success() => View();

    private string NormalizeRequestedRole(string? roleName)
    {
        // If user is not authenticated/admin, force EMPLOYEE to avoid self-elevation.
        if (!User.Identity?.IsAuthenticated ?? true) return "EMPLOYEE";
        if (!User.IsInRole("ADMIN")) return "EMPLOYEE";
        return string.IsNullOrWhiteSpace(roleName) ? "EMPLOYEE" : roleName.Trim().ToUpperInvariant();
    }

    private async Task PopulateRolesAsync()
    {
        var roles = await Task.FromResult(_roleManager.Roles.OrderBy(r => r.Name).ToList());

        // Unauthenticated users only see EMPLOYEE.
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            roles = roles.Where(r => r.Name == "EMPLOYEE").ToList();
        }
        else if (!User.IsInRole("ADMIN"))
        {
            roles = roles.Where(r => r.Name == "EMPLOYEE").ToList();
        }

        ViewData["Roles"] = roles
            .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
            .ToList();
    }
}

