using System.ComponentModel.DataAnnotations;

namespace ClinicApp.Web.ViewModels.Users;

public sealed class RegisterUserViewModel
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? RoleName { get; set; }
}

