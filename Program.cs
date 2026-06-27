using IPS_PROJECT.Data;
using IPS_PROJECT.Hubs;
using IPS_PROJECT.Models;
using IPS_PROJECT.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity (شلت الـ Extension اللي كان عامل مشكلة)
builder.Services.AddIdentity<USERS, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedEmail = true;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddHttpClient<AiPredictionService>();
builder.Services.AddScoped<BatchBuilderService>();
builder.Services.AddScoped<AiPredictionService>();
builder.Services.AddSignalR();
builder.Services.AddHostedService<DatabaseMonitoringService>();
builder.Services.AddScoped<PdfReportService>();
builder.Services.AddTransient<IEmailSender, EmailSender>();

var app = builder.Build();

// المايجريشن الأوتوماتيكي داخل الكلاستر (الخلاصة)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try 
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Migration Error: {ex.Message}");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseHttpMetrics();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapHub<IpsHub>("/ipsHub");
app.MapHub<SecurityHub>("/securityHub");
app.MapMetrics();
app.MapRazorPages();

app.Run();