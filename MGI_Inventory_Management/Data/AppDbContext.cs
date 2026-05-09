using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MGI_Inventory_Management.Models;

namespace MGI_Inventory_Management.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<AddProduct> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<ContactMessage> ContactMessages { get; set; }
        public DbSet<ProductMaster> ProductMasters { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<PurchaseItem> PurchaseItems { get; set; }
        public DbSet<CategoryLog> CategoryLogs { get; set; }
        public DbSet<ProductMasterLog> ProductMasterLogs { get; set; }
        public DbSet<OrderActivityLog> OrderActivityLogs { get; set; }
        public DbSet<PurchaseActivityLog> PurchaseActivityLogs { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AddProduct>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId);

            modelBuilder.Entity<ProductMaster>()
                .HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId);

            modelBuilder.Entity<OrderItem>()
                .HasOne(i => i.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(i => i.OrderId);

            modelBuilder.Entity<OrderItem>()
                .HasOne(i => i.Category)
                .WithMany()
                .HasForeignKey(i => i.CategoryId);

            // ✅ Purchase → PurchaseItems relationship
            modelBuilder.Entity<PurchaseItem>()
                .HasOne(i => i.Purchase)
                .WithMany(p => p.Items)
                .HasForeignKey(i => i.PurchaseId);

            // ✅ PurchaseItem → Category relationship
            modelBuilder.Entity<PurchaseItem>()
                .HasOne(i => i.Category)
                .WithMany()
                .HasForeignKey(i => i.CategoryId);
        }
    }
}