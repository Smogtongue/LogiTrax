using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LogiTrax.Models
{
    public class LogiTraxContext : IdentityDbContext<ApplicationUser>
    {
        public LogiTraxContext(DbContextOptions<LogiTraxContext> options)
            : base(options)
        {
        }

        public DbSet<InventoryItem> InventoryItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Add indexes on Orders
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.Status);
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.CustomerName);

            // Add index on OrderItems for the InventoryItemId column
            modelBuilder.Entity<OrderItem>()
                .HasIndex(oi => oi.InventoryItemId);
            
            // Other model configurations...
        }
    }
}
