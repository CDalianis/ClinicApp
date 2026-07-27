using ClinicApp.Web.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services));

// Add services to the container.
builder.Services.AddControllersWithViews();

var mysqlHost = builder.Configuration["MYSQL_HOST"] ?? "localhost";
var mysqlPort = builder.Configuration["MYSQL_PORT"] ?? "3306";
var mysqlDb = builder.Configuration["MYSQL_DB"] ?? "clinicapp";
var mysqlUser = builder.Configuration["MYSQL_USER"] ?? "clinic";
var mysqlPassword = builder.Configuration["MYSQL_PASSWORD"] ?? "12345";

var connectionString =
    $"Server={mysqlHost};Port={mysqlPort};Database={mysqlDb};User={mysqlUser};Password={mysqlPassword};TreatTinyAsBoolean=true;";

builder.Services.AddDbContext<ClinicDbContext>(options =>
    options.UseMySQL(connectionString));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole<Guid>>(opt =>
    {
        opt.Password.RequiredLength = 8;
        opt.Password.RequireDigit = true;
        opt.Password.RequireNonAlphanumeric = false;
        opt.Password.RequireUppercase = true;
        opt.Password.RequireLowercase = true;
    })
    .AddEntityFrameworkStores<ClinicDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.LoginPath = "/login";
    opt.LogoutPath = "/logout";
    opt.AccessDeniedPath = "/access-denied";
});

builder.Services.AddAuthorization(options =>
{
    foreach (var cap in SeedData.Capabilities.All)
    {
        options.AddPolicy(cap, p => p.RequireClaim(SeedData.CapabilityClaimType, cap));
    }
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
    await db.Database.MigrateAsync();
}

await SeedData.EnsureSeededAsync(app.Services);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSerilogRequestLogging();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
