using ECommerceProject.Core.Entities;
using ECommerceProject.Core.Entities.Identity;
using Microsoft.AspNetCore.Identity;

namespace ECommerceProject.Data.Seeds
{
    public static class DataSeeder
    {
        public static async Task SeedData(AppDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Önce rolleri oluştur
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }
            if (!await roleManager.RoleExistsAsync("User"))
            {
                await roleManager.CreateAsync(new IdentityRole("User"));
            }

            // Admin kullanıcısını oluştur
            if (await userManager.FindByEmailAsync("admin@example.com") == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = "admin",
                    Email = "admin@example.com",
                    FirstName = "Admin",
                    LastName = "User",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Kategori ve ürün verilerini ekle
            if (!context.Categories.Any())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "Elektronik", Description = "Elektronik Ürünler" },
                    new Category { Name = "Giyim", Description = "Giyim Ürünleri" },
                    new Category { Name = "Kitap", Description = "Kitaplar" }
                };

                context.Categories.AddRange(categories);
                context.SaveChanges();

                var products = new List<Product>
                {
                    new Product 
                    { 
                        Name = "Laptop", 
                        Description = "Gaming Laptop", 
                        Price = 15000, 
                        Stock = 10, 
                        CategoryId = categories[0].Id,
                        ImageUrl = "laptop.jpg"
                    },
                    new Product 
                    { 
                        Name = "T-Shirt", 
                        Description = "Pamuklu T-Shirt", 
                        Price = 150, 
                        Stock = 100, 
                        CategoryId = categories[1].Id,
                        ImageUrl = "tshirt.jpg"
                    },
                    new Product 
                    { 
                        Name = "Roman", 
                        Description = "Türk Edebiyatı", 
                        Price = 50, 
                        Stock = 50, 
                        CategoryId = categories[2].Id,
                        ImageUrl = "book.jpg"
                    }
                };

                context.Products.AddRange(products);
                context.SaveChanges();
            }
        }
    }
} 