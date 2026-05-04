using System.ComponentModel.DataAnnotations;

namespace ClinicApp.Web.ViewModels.Doctors;

public sealed class DoctorEditViewModel
{
    [Required]
    public Guid Uuid { get; set; }

    [Required, StringLength(64)]
    public string LicenseNumber { get; set; } = string.Empty;

    [Required, StringLength(64)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(64)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public long? RegionId { get; set; }
}

