using ClinicApp.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Web.Data;

public sealed class ClinicDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ClinicDbContext(DbContextOptions<ClinicDbContext> options) : base(options) { }

    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Region> Regions => Set<Region>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Region>(b =>
        {
            b.ToTable("regions");
            b.Property(x => x.Name).HasMaxLength(128).IsRequired();
            b.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<Doctor>(b =>
        {
            b.ToTable("doctors");
            b.HasIndex(x => x.Uuid).IsUnique();
            b.HasIndex(x => x.LicenseNumber).IsUnique();
            b.Property(x => x.LicenseNumber).HasMaxLength(64).IsRequired();
            b.Property(x => x.FirstName).HasMaxLength(64).IsRequired();
            b.Property(x => x.LastName).HasMaxLength(64).IsRequired();
            b.HasOne(x => x.Region).WithMany(r => r.Doctors).HasForeignKey(x => x.RegionId);
            b.HasQueryFilter(x => !x.IsDeleted);
        });

        builder.Entity<ApplicationUser>(b =>
        {
            b.ToTable("users");
            b.HasIndex(x => x.Uuid).IsUnique();
        });

        builder.Entity<IdentityRole<Guid>>(b => b.ToTable("roles"));
        builder.Entity<IdentityUserRole<Guid>>(b => b.ToTable("user_roles"));
        builder.Entity<IdentityUserClaim<Guid>>(b => b.ToTable("user_claims"));
        builder.Entity<IdentityRoleClaim<Guid>>(b => b.ToTable("role_claims"));
        builder.Entity<IdentityUserLogin<Guid>>(b => b.ToTable("user_logins"));
        builder.Entity<IdentityUserToken<Guid>>(b => b.ToTable("user_tokens"));

        // Seed minimal reference data (regions) so inserts work out of the box.
        builder.Entity<Region>().HasData(
            new Region { Id = 1, Name = "Athens" },
            new Region { Id = 2, Name = "Thessaloniki" },
            new Region { Id = 3, Name = "Patras" }
        );
    }
}

