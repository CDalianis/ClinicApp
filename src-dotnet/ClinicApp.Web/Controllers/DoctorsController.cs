using ClinicApp.Web.Data;
using ClinicApp.Web.Infrastructure;
using ClinicApp.Web.Models;
using ClinicApp.Web.ViewModels.Doctors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace ClinicApp.Web.Controllers;

[Route("doctors")]
public sealed class DoctorsController : Controller
{
    private readonly ClinicDbContext _db;
    private readonly Serilog.ILogger _log;

    public DoctorsController(ClinicDbContext db)
    {
        _db = db;
        _log = Log.ForContext<DoctorsController>();
    }

    [HttpGet("")]
    [Authorize(Policy = SeedData.Capabilities.ViewDoctors)]
    public async Task<IActionResult> Index(int page = 0, int size = 5)
    {
        if (size is < 1 or > 100) size = 5;
        if (page < 0) page = 0;

        var query = _db.Doctors
            .AsNoTracking()
            .Include(d => d.Region)
            .OrderBy(d => d.LastName)
            .ThenBy(d => d.FirstName);

        var total = await query.CountAsync();
        var items = await query
            .Skip(page * size)
            .Take(size)
            .ToListAsync();

        var model = new PagedResult<Doctor>
        {
            Items = items,
            PageIndex = page,
            PageSize = size,
            TotalCount = total
        };

        return View(model);
    }

    [HttpGet("insert")]
    [Authorize(Policy = SeedData.Capabilities.InsertDoctor)]
    public async Task<IActionResult> Insert()
    {
        await PopulateRegionsAsync();
        return View(new DoctorInsertViewModel());
    }

    [HttpPost("insert")]
    [Authorize(Policy = SeedData.Capabilities.InsertDoctor)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Insert(DoctorInsertViewModel vm)
    {
        await PopulateRegionsAsync();

        if (!ModelState.IsValid) return View(vm);

        var regionExists = await _db.Regions.AnyAsync(r => r.Id == vm.RegionId);
        if (!regionExists)
        {
            ModelState.AddModelError(nameof(vm.RegionId), "Invalid region");
            return View(vm);
        }

        if (await _db.Doctors.IgnoreQueryFilters().AnyAsync(d => d.LicenseNumber == vm.LicenseNumber))
        {
            ModelState.AddModelError(nameof(vm.LicenseNumber), $"Doctor with license number '{vm.LicenseNumber}' already exists");
            return View(vm);
        }

        var doctor = new Doctor
        {
            LicenseNumber = vm.LicenseNumber.Trim(),
            FirstName = vm.FirstName.Trim(),
            LastName = vm.LastName.Trim(),
            RegionId = vm.RegionId!.Value
        };

        _db.Doctors.Add(doctor);
        await _db.SaveChangesAsync();

        _log.Information("Doctor with licenseNumber={LicenseNumber} saved successfully", doctor.LicenseNumber);

        TempData["DoctorUuid"] = doctor.Uuid.ToString();
        return RedirectToAction(nameof(Success));
    }

    [HttpGet("success")]
    [Authorize(Policy = SeedData.Capabilities.InsertDoctor)]
    public IActionResult Success() => View();

    [HttpGet("edit/{uuid:guid}")]
    [Authorize(Policy = SeedData.Capabilities.EditDoctor)]
    public async Task<IActionResult> Edit(Guid uuid)
    {
        await PopulateRegionsAsync();

        var doctor = await _db.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Uuid == uuid);

        if (doctor is null)
        {
            ViewData["ErrorMessage"] = $"Doctor with uuid={uuid} not found";
            return View(new DoctorEditViewModel { Uuid = uuid });
        }

        return View(new DoctorEditViewModel
        {
            Uuid = doctor.Uuid,
            LicenseNumber = doctor.LicenseNumber,
            FirstName = doctor.FirstName,
            LastName = doctor.LastName,
            RegionId = doctor.RegionId
        });
    }

    [HttpPost("edit")]
    [Authorize(Policy = SeedData.Capabilities.EditDoctor)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(DoctorEditViewModel vm)
    {
        await PopulateRegionsAsync();

        if (!ModelState.IsValid) return View(vm);

        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.Uuid == vm.Uuid);
        if (doctor is null)
        {
            ViewData["ErrorMessage"] = $"Doctor with uuid={vm.Uuid} not found";
            return View(vm);
        }

        if (await _db.Doctors.AnyAsync(d => d.LicenseNumber == vm.LicenseNumber && d.Uuid != vm.Uuid))
        {
            ModelState.AddModelError(nameof(vm.LicenseNumber), $"Doctor with license number '{vm.LicenseNumber}' already exists");
            return View(vm);
        }

        var regionExists = await _db.Regions.AnyAsync(r => r.Id == vm.RegionId);
        if (!regionExists)
        {
            ModelState.AddModelError(nameof(vm.RegionId), "Invalid region");
            return View(vm);
        }

        doctor.LicenseNumber = vm.LicenseNumber.Trim();
        doctor.FirstName = vm.FirstName.Trim();
        doctor.LastName = vm.LastName.Trim();
        doctor.RegionId = vm.RegionId!.Value;
        doctor.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        _log.Information("Doctor with uuid={Uuid} updated successfully", vm.Uuid);

        TempData["DoctorUuid"] = vm.Uuid.ToString();
        return RedirectToAction(nameof(UpdateSuccess));
    }

    [HttpGet("update-success")]
    [Authorize(Policy = SeedData.Capabilities.EditDoctor)]
    public IActionResult UpdateSuccess() => View();

    [HttpPost("delete/{uuid:guid}")]
    [Authorize(Policy = SeedData.Capabilities.DeleteDoctor)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid uuid)
    {
        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.Uuid == uuid);
        if (doctor is null)
        {
            TempData["ErrorMessage"] = $"Doctor with uuid={uuid} not found";
            return RedirectToAction(nameof(Index));
        }

        doctor.SoftDelete();
        await _db.SaveChangesAsync();
        _log.Information("Doctor with uuid={Uuid} deleted successfully", uuid);

        TempData["DoctorUuid"] = uuid.ToString();
        return RedirectToAction(nameof(DeleteSuccess));
    }

    [HttpGet("delete-success")]
    [Authorize(Policy = SeedData.Capabilities.DeleteDoctor)]
    public IActionResult DeleteSuccess() => View();

    private async Task PopulateRegionsAsync()
    {
        var regions = await _db.Regions.AsNoTracking().OrderBy(r => r.Name).ToListAsync();
        ViewData["Regions"] = regions.Select(r => new SelectListItem(r.Name, r.Id.ToString())).ToList();
    }
}

