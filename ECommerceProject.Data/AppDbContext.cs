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
        public DbSet<SellerReview> SellerReviews { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<WishlistItem> WishlistItems { get; set; }

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

            modelBuilder.Entity<SellerReview>(entity =>
            {
                entity.HasOne(e => e.Seller)
                      .WithMany()
                      .HasForeignKey(e => e.SellerId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.Rating)
                      .IsRequired()
                      .HasAnnotation("Range", new[] { 1, 5 });

                entity.Property(e => e.Comment)
                      .HasMaxLength(500);
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.TotalAmount)
                      .HasPrecision(18, 2);

                entity.Property(e => e.Status)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(e => e.PaymentStatus)
                      .IsRequired()
                      .HasMaxLength(50);
            });

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasOne(e => e.Order)
                      .WithMany(e => e.OrderItems)
                      .HasForeignKey(e => e.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                      .WithMany()
                      .HasForeignKey(e => e.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Seller)
                      .WithMany()
                      .HasForeignKey(e => e.SellerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.UnitPrice)
                      .HasPrecision(18, 2);

                entity.Property(e => e.Status)
                      .IsRequired()
                      .HasMaxLength(50);
            });

            modelBuilder.Entity<Cart>(entity =>
            {
                entity.HasOne(e => e.User)
                      .WithOne()
                      .HasForeignKey<Cart>(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.HasOne(e => e.Cart)
                      .WithMany(e => e.Items)
                      .HasForeignKey(e => e.CartId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                      .WithMany()
                      .HasForeignKey(e => e.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.UnitPrice)
                      .HasPrecision(18, 2);
            });

            modelBuilder.Entity<WishlistItem>(entity =>
            {
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                      .WithMany()
                      .HasForeignKey(e => e.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Bir kullanıcı aynı ürünü birden fazla kez favoriye ekleyemesin
                entity.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();
            });
        }
    }
} 