using AutoLineCRM.CatalogService.Data;
using AutoLineCRM.CatalogService.Services;
using AutoLineCRM.SharedKernel.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AutoLineCRM.Tests;

public class CarServiceTests
{
    // Хелпер для створення ізольованої In-Memory бази для кожного тесту
    private CatalogDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new CatalogDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_ShouldAddCarToDatabase()
    {
        // Arrange (Підготовка)
        using var db = GetInMemoryDbContext();
        var service = new CarService(db);
        var newCar = new Car
        {
            Brand = "Audi",
            Model = "A6",
            VIN = "TEST123456789",
            Price = 50000
        };

        // Act (Дія)
        var result = await service.CreateAsync(newCar);

        // Assert (Перевірка)
        result.Id.Should().BeGreaterThan(0); // ID має згенеруватися
        db.Cars.Count().Should().Be(1);      // Машина має з'явитися в базі
    }

    [Fact]
    public async Task SearchAsync_ShouldFilterByBrand()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        db.Cars.AddRange(
            new Car { Brand = "Toyota", Model = "Camry", VIN = "111" },
            new Car { Brand = "BMW", Model = "X5", VIN = "222" },
            new Car { Brand = "Toyota", Model = "Corolla", VIN = "333" }
        );
        await db.SaveChangesAsync();
        var service = new CarService(db);

        // Act
        var result = await service.SearchAsync(brand: "Toyota", model: null, minPrice: null, maxPrice: null, status: null);

        // Assert
        result.Should().HaveCount(2); // Має знайти 2 Тойоти
        result.All(c => c.Brand == "Toyota").Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveCarFromDb()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var car = new Car { Brand = "Ford", Model = "Focus", VIN = "444" };
        db.Cars.Add(car);
        await db.SaveChangesAsync();
        var service = new CarService(db);

        // Act
        var isDeleted = await service.DeleteAsync(car.Id);

        // Assert
        isDeleted.Should().BeTrue();
        db.Cars.Should().BeEmpty();
    }
}