namespace ClinicApp.Web.Models;

public sealed class Region
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
}

