using IPS_PROJECT.Data;
using IPS_PROJECT.Hubs;
using IPS_PROJECT.Models;
using IPS_PROJECT.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

// For Pdf Downloading
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// 1️⃣ Add services
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// 2️⃣ DbContext - قراءة الـ Connection String (ستعمل محلياً وفي الدوكر)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// 3️⃣ Identity
builder.Services.AddIdentity<USERS, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedEmail = true;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Ai prediction services
builder.Services.AddHttpClient<AiPredictionService>();
builder.Services.AddScoped<AiPredictionService>();

// SignalR & Background Services
builder.Services.AddSignalR();
builder.Services.AddHostedService<DatabaseMonitoringService>();

// Other Services
builder.Services.AddScoped<PdfReportService>();
builder.Services.AddTransient<IEmailSender, EmailSender>();

var app = builder.Build();

// 4️⃣ HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ملاحظة: في الدوكر يفضل أحياناً تعطيل HttpsRedirection لو الـ SSL هيتم معالجته عن طريق Nginx أو Azure Load Balancer
// لكن سأتركها كما هي لتعمل بشكل طبيعي
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<IpsHub>("/ipsHub");
app.MapHub<SecurityHub>("/securityHub");
app.MapRazorPages();

// ===== 5️⃣ Auto-Migration & Seed Data =====
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<AppDbContext>();
        
        // 🚨 أهم سطر للدوكر: بيكريت الداتابيز والجداول أوتوماتيك أول ما الكونتينر يقوم
        db.Database.Migrate();

        // ---- 1. Seed Roles and Admin ----
        await SeedData.SeedRolesAndAdminAsync(services);

        // ---- 3. Add Test Events ----
        if (!db.Events.Any())
        {
            db.Events.AddRange(new EVENTS[]
            {
                new EVENTS { SourceIp="192.168.1.10", DestinationIp="10.0.0.5", TrafficType="HTTP", Prediction="Benign", Confidence=95, Status="Blocked" },
                new EVENTS { SourceIp="192.168.1.12", DestinationIp="10.0.0.7", TrafficType="SSH", Prediction="Attack", Confidence=85, Status="Allowed" },
                new EVENTS { SourceIp="192.168.1.15", DestinationIp="10.0.0.8", TrafficType="FTP", Prediction="Attack", Confidence=90, Status="Blocked" },
                new EVENTS { SourceIp="192.168.1.20", DestinationIp="10.0.0.9", TrafficType="HTTP", Prediction="Benign", Confidence=99, Status="Allowed" },
                new EVENTS { SourceIp="192.168.1.25", DestinationIp="10.0.0.10", TrafficType="HTTPS", Prediction="Benign", Confidence=97, Status="Detected" },
                new EVENTS { SourceIp="192.168.1.30", DestinationIp="10.0.0.11", TrafficType="SMTP", Prediction="Attack", Confidence=80, Status="Blocked" },
                new EVENTS { SourceIp="192.168.1.35", DestinationIp="10.0.0.12", TrafficType="SSH", Prediction="Attack", Confidence=88, Status="Allowed" },
                new EVENTS { SourceIp="192.168.1.40", DestinationIp="10.0.0.13", TrafficType="HTTP", Prediction="Benign", Confidence=96, Status="Detected" },
                new EVENTS { SourceIp="192.168.1.45", DestinationIp="10.0.0.14", TrafficType="FTP", Prediction="Attack", Confidence=85, Status="Blocked" },
                new EVENTS { SourceIp="192.168.1.50", DestinationIp="10.0.0.15", TrafficType="SMTP", Prediction="Attack", Confidence=82, Status="Blocked" }
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
            if(threats.Any())
            {
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

        if (!db.SystemStatus.Any())
        {
            db.SystemStatus.Add(new SYSTEM_STATUS
            {
                IsSecure = true,
                LastUpdated = DateTime.Now
            });
            db.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        // تسجيل الخطأ لو الداتابيز لسه مآقامتش (مفيد جداً في الدوكر)
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "حدث خطأ أثناء تهيئة الداتابيز (Migration/Seeding)");
    }
}

app.Run();
