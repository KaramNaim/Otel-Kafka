using Microsoft.EntityFrameworkCore;
using OTel.Domain.Models;

namespace OTel.Infrastructure.Context;

public static class AppDbContextExt
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = 1,
                Name = "Mechanical Keyboard",
                Price = 149.99m,
                Stock = 50,
                CreatedOn = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            },
            new Product
            {
                Id = 2,
                Name = "Wireless Mouse",
                Price = 59.99m,
                Stock = 120,
                CreatedOn = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            },
            new Product
            {
                Id = 3,
                Name = "USB-C Hub",
                Price = 39.99m,
                Stock = 200,
                CreatedOn = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            },
            new Product
            {
                Id = 4,
                Name = "4K Monitor",
                Price = 499.99m,
                Stock = 25,
                CreatedOn = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            },
            new Product
            {
                Id = 5,
                Name = "Webcam HD",
                Price = 79.99m,
                Stock = 80,
                CreatedOn = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            }
        );
    }
}
