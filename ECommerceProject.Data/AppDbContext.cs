using ECommerceProject.Core.Entities;
using ECommerceProject.Core.Entities.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ECommerceProject.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Seller> Sellers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Category ve Product arasındaki ilişkiyi tanımlayalım
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId);

            modelBuilder.Entity<Seller>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.StoreName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.ContactEmail).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ContactPhone).HasMaxLength(20);
                entity.Property(e => e.Address).HasMaxLength(200);
                entity.Property(e => e.Rating).HasPrecision(3, 2);

                // User ile ilişki
                entity.HasOne(e => e.User)
                      .WithOne()
                      .HasForeignKey<Seller>(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Products ile ilişki
                entity.HasMany(e => e.Products)
                      .WithOne(e => e.Seller)
                      .HasForeignKey(e => e.SellerId)
                      .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
} 