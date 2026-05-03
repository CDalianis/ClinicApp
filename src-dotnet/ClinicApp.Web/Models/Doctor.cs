using System.ComponentModel.DataAnnotations;

namespace ClinicApp.Web.Models;

public sealed class Doctor : BaseEntity
{
    public long Id { get; set; }

    [Required]
    public Guid Uuid { get; set; } = Guid.NewGuid();

    [Required, StringLength(64)]
    public string LicenseNumber { get; set; } = string.Empty;

    [Required, StringLength(64)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(64)]
    public string LastName { get; set; } = string.Empty;

    public long RegionId { get; set; }
    public Region? Region { get; set; }
}

