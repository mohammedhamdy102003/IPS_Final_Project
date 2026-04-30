using IPS_PROJECT.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IPS_PROJECT.Data
{
    public class AppDbContext : IdentityDbContext<USERS>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<EVENTS> Events { get; set; } = null!;
        public DbSet<THREATS> Threats { get; set; } = null!;
        public DbSet<ALERTS> Alerts { get; set; } = null!;

        public DbSet<SYSTEM_STATUS> SystemStatus { get; set; }
        // AlertNotifications  table
        public DbSet<AlertNotification> AlertNotifications { get; set; } = null!;

        public DbSet<AdminRequest> AdminRequests { get; set; }





        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); 

            // THREATS ↔ ALERTS One-to-One
            modelBuilder.Entity<THREATS>()
                .HasOne(t => t.Alert)
                .WithOne(a => a.Threat)
                .HasForeignKey<ALERTS>(a => a.ThreatId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
