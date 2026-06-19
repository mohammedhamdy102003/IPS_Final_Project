using IPS_PROJECT.Data;
using IPS_PROJECT.Hubs;
using IPS_PROJECT.Models;
using IPS_PROJECT.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Prometheus;

//For Pdf Downloading
using QuestPDF.Infrastructure;
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// 1️⃣ Add services
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// 2️⃣ DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);



// 3️⃣ Identity
builder.Services.AddIdentity<USERS, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedEmail = true;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();
//Ai predection services
// إضافة هذه السطور قبل builder.Build()
builder.Services.AddHttpClient<AiPredictionService>();
builder.Services.AddScoped<BatchBuilderService>();
builder.Services.AddScoped<AiPredictionService>();

// Background Service 
builder.Services.AddSignalR();
builder.Services.AddHostedService<DatabaseMonitoringService>();

 

//For Pdf Downloading

builder.Services.AddScoped<PdfReportService>();

builder.Services.AddTransient<IEmailSender, EmailSender>();


var app = builder.Build();

// 4️⃣ HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Prometheus: collects HTTP request metrics automatically
app.UseHttpMetrics();

app.UseAuthentication();
app.UseAuthorization();



app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

//Background Service
app.MapHub<IpsHub>("/ipsHub");

// userdashboard security service 

app.MapHub<SecurityHub>("/securityHub"); // Endpoint للـ Hub


// ===== 5️⃣ Seed Roles, Admin and Test Data =====
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    // ---- 1. Seed Roles and Admin ----
    await SeedData.SeedRolesAndAdminAsync(services);

    var db = services.GetRequiredService<AppDbContext>();
    var userManager = services.GetRequiredService<UserManager<USERS>>();



    // ---- 3. Add Test Events ----
    if (!db.Events.Any())
    {
        db.Events.AddRange(new EVENTS[]
        {
            new EVENTS { SourceIp="192.168.1.10", DestinationIp="10.0.0.5",  AttackType="portscan", Prediction="anomaly", Confidence=95, Status="Blocked" },
            new EVENTS { SourceIp="192.168.1.12", DestinationIp="10.0.0.7",  AttackType="bruteforce", Prediction="anomaly", Confidence=85, Status="Allowed" },
            new EVENTS { SourceIp="192.168.1.15", DestinationIp="10.0.0.8",  AttackType="portscan", Prediction="anomaly", Confidence=90, Status="Blocked" },
            new EVENTS { SourceIp="192.168.1.20", DestinationIp="10.0.0.9",  AttackType="portscan", Prediction="anomaly", Confidence=99, Status="Allowed" },
            new EVENTS { SourceIp="192.168.1.25", DestinationIp="10.0.0.10", AttackType="bruteforce", Prediction="anomaly", Confidence=97, Status="Detected" },
            new EVENTS { SourceIp="192.168.1.30", DestinationIp="10.0.0.11", AttackType="bruteforce", Prediction="anomaly", Confidence=80, Status="Blocked" },
            new EVENTS { SourceIp="192.168.1.35", DestinationIp="10.0.0.12", AttackType="portscan", Prediction="anomaly", Confidence=88, Status="Allowed" },
            new EVENTS { SourceIp="192.168.1.40", DestinationIp="10.0.0.13", AttackType="bruteforce", Prediction="anomaly", Confidence=96, Status="Detected" },
            new EVENTS { SourceIp="192.168.1.45", DestinationIp="10.0.0.14", AttackType="portscan", Prediction="anomaly", Confidence=85, Status="Blocked" },
            new EVENTS { SourceIp="192.168.1.50", DestinationIp="10.0.0.15", AttackType="portscan", Prediction="anomaly", Confidence=82, Status="Blocked" }
        });
        db.SaveChanges();
    }

    // ---- 4. Add Test Threats ----
    if (!db.Threats.Any())
    {
        db.Threats.AddRange(new THREATS[]
        {
            new THREATS { AttackType="DDoS", Severity="High", DetectedTime=DateTime.Now },
            new THREATS { AttackType="Malware", Severity="Medium", DetectedTime=DateTime.Now.AddMinutes(-10) },
            new THREATS { AttackType="Phishing", Severity="Low", DetectedTime=DateTime.Now.AddMinutes(-20) },
            new THREATS { AttackType="Ransomware", Severity="High", DetectedTime=DateTime.Now.AddMinutes(-30) },
            new THREATS { AttackType="SQL Injection", Severity="Medium", DetectedTime=DateTime.Now.AddMinutes(-40) }
        });
        db.SaveChanges();
    }

    // ---- 5. Add Test Alerts ----
    if (!db.Alerts.Any())
    {
        var threats = db.Threats.ToList();
        db.Alerts.AddRange(new ALERTS[]
        {
            new ALERTS { Message="DDoS attack detected!", ThreatId=threats[0].Id },
            new ALERTS { Message="Malware activity found!", ThreatId=threats[1].Id },
            new ALERTS { Message="Phishing email detected!", ThreatId=threats[2].Id },
            new ALERTS { Message="Ransomware attack ongoing!", ThreatId=threats[3].Id },
            new ALERTS { Message="SQL Injection attempt detected!", ThreatId=threats[4].Id }
        });
        db.SaveChanges();
    }
}
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (!context.SystemStatus.Any())
    {
        context.SystemStatus.Add(new SYSTEM_STATUS
        {
            IsSecure = true,
            LastUpdated = DateTime.Now,
        //    FirstAdminCreated = false
        });

        context.SaveChanges();
    }
}


// Prometheus: exposes /metrics endpoint for scraping
app.MapMetrics();

app.MapRazorPages();

app.Run();
