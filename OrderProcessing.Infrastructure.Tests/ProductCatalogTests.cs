using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderProcessing.Infrastructure.Catalog;
using OrderProcessing.Infrastructure.Persistence;
using OrderProcessing.Infrastructure.Persistence.Models;

namespace OrderProcessing.Infrastructure.Tests;

public class ProductCatalogTests
{
    [Fact]
    public async Task GetPrice_WhenProductExists_ReturnsCorrectPrice()
    {
        // Arrange
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AppDbContext(options);

        await db.Database.EnsureCreatedAsync();

        db.Products.Add(new Product
        {
            Name = "Mechanical Keyboard",
            Price = 120.00m
        });

        await db.SaveChangesAsync();

        var productId = await db.Products
            .Select(p => p.Id)
            .SingleAsync();

        db.ChangeTracker.Clear();

        var catalog = new ProductCatalog(db);

        // Act
        var price = await catalog.GetPriceAsync(productId);

        // Assert
        Assert.Equal(120.00m, price);
    }

    [Fact]
    public async Task GetPrice_WhenProductDoesNotExist_ReturnsNull()
    {
        // Arrange
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AppDbContext(options);

        await db.Database.EnsureCreatedAsync();

        var catalog = new ProductCatalog(db);

        // Act
        var price = await catalog.GetPriceAsync(999);

        // Assert
        Assert.Null(price);
    }
}