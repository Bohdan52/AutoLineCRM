using AutoLineCRM.CatalogService.Controllers;
using AutoLineCRM.CatalogService.Data;
using AutoLineCRM.CatalogService.Services;
using AutoLineCRM.SharedKernel.Entities;
using AutoLineCRM.SharedKernel.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AutoLineCRM.Tests;

// Тести каталогу автомобілів: CarService та CarsController
public class CarServiceTests
{
    private static CatalogDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    // Тестовий каталог: 5 автомобілів різних марок, цін і статусів
    private static async Task SeedAsync(CatalogDbContext db)
    {
        db.Cars.AddRange(
            new Car { Brand = "Toyota", Model = "Camry",   VIN = "S1", Price = 25000, Status = CarStatus.Available },
            new Car { Brand = "Toyota", Model = "Corolla", VIN = "S2", Price = 18000, Status = CarStatus.Sold },
            new Car { Brand = "BMW",    Model = "X5",      VIN = "S3", Price = 60000, Status = CarStatus.Available },
            new Car { Brand = "Audi",   Model = "A6",      VIN = "S4", Price = 45000, Status = CarStatus.Reserved },
            new Car { Brand = "Audi",   Model = "Q5",      VIN = "S5", Price = 52000, Status = CarStatus.Available });
        await db.SaveChangesAsync();
    }

    private static CarsController CreateCarsController(out CatalogDbContext db)
    {
        db = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        return new CarsController(new CarService(db));
    }

    // ===== CarService: базові сценарії =====

    [Fact]
    public async Task CreateAsync_ShouldAddCarToDatabase()
    {
        using var db = CreateDb();
        var service = new CarService(db);

        var result = await service.CreateAsync(new Car { Brand = "Audi", Model = "A6", VIN = "TEST123456789", Price = 50000 });

        result.Id.Should().BeGreaterThan(0);
        db.Cars.Count().Should().Be(1);
    }

    [Fact]
    public async Task SearchAsync_ShouldFilterByBrand()
    {
        using var db = CreateDb();
        db.Cars.AddRange(
            new Car { Brand = "Toyota", Model = "Camry", VIN = "111" },
            new Car { Brand = "BMW", Model = "X5", VIN = "222" },
            new Car { Brand = "Toyota", Model = "Corolla", VIN = "333" });
        await db.SaveChangesAsync();
        var service = new CarService(db);

        var result = await service.SearchAsync("Toyota", null, null, null, null);

        result.Should().HaveCount(2);
        result.All(c => c.Brand == "Toyota").Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveCarFromDb()
    {
        using var db = CreateDb();
        var car = new Car { Brand = "Ford", Model = "Focus", VIN = "444" };
        db.Cars.Add(car);
        await db.SaveChangesAsync();
        var service = new CarService(db);

        var isDeleted = await service.DeleteAsync(car.Id);

        isDeleted.Should().BeTrue();
        db.Cars.Should().BeEmpty();
    }

    // ===== CarService: пошук, пагінація, оновлення, валідація =====

    [Fact]
    public async Task SearchAsync_ShouldFilterByModel()
    {
        using var db = CreateDb();
        await SeedAsync(db);
        var service = new CarService(db);

        var result = await service.SearchAsync(null, "Cam", null, null, null);

        result.Should().ContainSingle().Which.Model.Should().Be("Camry");
    }

    [Fact]
    public async Task SearchAsync_ShouldFilterByPriceRange()
    {
        using var db = CreateDb();
        await SeedAsync(db);
        var service = new CarService(db);

        var result = await service.SearchAsync(null, null, minPrice: 20000, maxPrice: 50000, status: null);

        result.Select(c => c.Model).Should().BeEquivalentTo(new[] { "Camry", "A6" });
    }

    [Fact]
    public async Task SearchAsync_ShouldFilterByStatus()
    {
        using var db = CreateDb();
        await SeedAsync(db);
        var service = new CarService(db);

        var result = await service.SearchAsync(null, null, null, null, CarStatus.Sold);

        result.Should().ContainSingle().Which.Model.Should().Be("Corolla");
    }

    [Fact]
    public async Task SearchAsync_ShouldCombineFilters()
    {
        using var db = CreateDb();
        await SeedAsync(db);
        var service = new CarService(db);

        var result = await service.SearchAsync("Audi", null, null, null, CarStatus.Available);

        result.Should().ContainSingle().Which.Model.Should().Be("Q5");
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnEmpty_WhenNothingMatches()
    {
        using var db = CreateDb();
        await SeedAsync(db);
        var service = new CarService(db);

        var result = await service.SearchAsync("Tesla", null, null, null, null);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_ShouldSortByBrandThenModel()
    {
        using var db = CreateDb();
        await SeedAsync(db);
        var service = new CarService(db);

        var result = await service.SearchAsync(null, null, null, null, null);

        result.Select(c => $"{c.Brand} {c.Model}").Should().Equal(
            "Audi A6", "Audi Q5", "BMW X5", "Toyota Camry", "Toyota Corolla");
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnRequestedPage()
    {
        using var db = CreateDb();
        await SeedAsync(db);
        var service = new CarService(db);

        var page2 = await service.SearchAsync(null, null, null, null, null, page: 2, size: 2);
        var page3 = await service.SearchAsync(null, null, null, null, null, page: 3, size: 2);

        page2.Select(c => c.Model).Should().Equal("X5", "Camry");
        page3.Select(c => c.Model).Should().Equal("Corolla");
    }

    [Fact]
    public async Task SearchAsync_ShouldNormalizeInvalidPaging()
    {
        // Дефект D5: page = 0 та size = 0 мають замінюватися на допустимі значення
        using var db = CreateDb();
        await SeedAsync(db);
        var service = new CarService(db);

        var result = await service.SearchAsync(null, null, null, null, null, page: 0, size: 0);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenCarDoesNotExist()
    {
        using var db = CreateDb();
        var service = new CarService(db);

        var result = await service.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldIncludeImages()
    {
        using var db = CreateDb();
        var car = new Car { Brand = "Audi", Model = "A4", VIN = "IMG1" };
        car.Images.Add(new CarImage { Url = "a.jpg", IsMain = true });
        car.Images.Add(new CarImage { Url = "b.jpg" });
        db.Cars.Add(car);
        await db.SaveChangesAsync();
        var service = new CarService(db);

        var result = await service.GetByIdAsync(car.Id);

        result.Should().NotBeNull();
        result!.Images.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_ShouldApplyDefaults()
    {
        using var db = CreateDb();
        var service = new CarService(db);

        var result = await service.CreateAsync(new Car { Brand = "Kia", Model = "Rio", VIN = "DEF1" });

        result.Status.Should().Be(CarStatus.Available);
        result.AddedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenVinAlreadyExists()
    {
        // Дефект D1: у каталозі не може бути двох автомобілів з однаковим VIN
        using var db = CreateDb();
        var service = new CarService(db);
        await service.CreateAsync(new Car { Brand = "Audi", Model = "A6", VIN = "DUP1" });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new Car { Brand = "BMW", Model = "X5", VIN = "DUP1" }));
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenVinIsEmpty()
    {
        // Дефект D1: VIN є обов'язковим
        using var db = CreateDb();
        var service = new CarService(db);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(new Car { Brand = "Audi", Model = "A6", VIN = "" }));
    }

    [Fact]
    public async Task UpdateAsync_ShouldChangeFields_AndKeepVin()
    {
        using var db = CreateDb();
        var car = new Car { Brand = "Ford", Model = "Focus", VIN = "UPD1", Price = 10000, Year = 2015 };
        db.Cars.Add(car);
        await db.SaveChangesAsync();
        var service = new CarService(db);

        var ok = await service.UpdateAsync(car.Id, new Car
        {
            Brand = "Ford", Model = "Mondeo", VIN = "CHANGED", Price = 12000, Year = 2018,
            Status = CarStatus.Reserved
        });

        ok.Should().BeTrue();
        var stored = await db.Cars.FindAsync(car.Id);
        stored!.Model.Should().Be("Mondeo");
        stored.Price.Should().Be(12000);
        stored.Status.Should().Be(CarStatus.Reserved);
        stored.VIN.Should().Be("UPD1"); // VIN не змінюється при оновленні
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnFalse_WhenCarDoesNotExist()
    {
        using var db = CreateDb();
        var service = new CarService(db);

        var ok = await service.UpdateAsync(999, new Car { Brand = "X", Model = "Y", VIN = "Z" });

        ok.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenCarDoesNotExist()
    {
        using var db = CreateDb();
        var service = new CarService(db);

        var ok = await service.DeleteAsync(999);

        ok.Should().BeFalse();
    }

    // ===== CarsController: відповіді HTTP =====

    [Fact]
    public async Task Cars_Search_ShouldReturnOkWithList()
    {
        var controller = CreateCarsController(out var db);
        db.Cars.Add(new Car { Brand = "Audi", Model = "A6", VIN = "C1" });
        await db.SaveChangesAsync();

        var result = await controller.Search(null, null, null, null, null);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<List<Car>>().Which.Should().HaveCount(1);
    }

    [Fact]
    public async Task Cars_GetById_ShouldReturnOk_WhenCarExists()
    {
        var controller = CreateCarsController(out var db);
        var car = new Car { Brand = "Audi", Model = "A6", VIN = "C2" };
        db.Cars.Add(car);
        await db.SaveChangesAsync();

        var result = await controller.GetById(car.Id);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Cars_GetById_ShouldReturnNotFound_WhenCarDoesNotExist()
    {
        var controller = CreateCarsController(out _);

        var result = await controller.GetById(999);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Cars_Create_ShouldReturnCreatedAtAction()
    {
        var controller = CreateCarsController(out _);

        var result = await controller.Create(new Car { Brand = "Kia", Model = "Rio", VIN = "C3" });

        result.Should().BeOfType<CreatedAtActionResult>()
              .Which.ActionName.Should().Be("GetById");
    }

    [Fact]
    public async Task Cars_Create_ShouldReturnConflict_WhenVinAlreadyExists()
    {
        var controller = CreateCarsController(out _);
        await controller.Create(new Car { Brand = "Kia", Model = "Rio", VIN = "DUP" });

        var result = await controller.Create(new Car { Brand = "Audi", Model = "A4", VIN = "DUP" });

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Cars_Create_ShouldReturnBadRequest_WhenVinIsEmpty()
    {
        var controller = CreateCarsController(out _);

        var result = await controller.Create(new Car { Brand = "Kia", Model = "Rio", VIN = "" });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Cars_Update_ShouldReturnNoContent_WhenCarExists()
    {
        var controller = CreateCarsController(out var db);
        var car = new Car { Brand = "Ford", Model = "Focus", VIN = "C4" };
        db.Cars.Add(car);
        await db.SaveChangesAsync();

        var result = await controller.Update(car.Id, new Car { Brand = "Ford", Model = "Mondeo" });

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Cars_Update_ShouldReturnNotFound_WhenCarDoesNotExist()
    {
        var controller = CreateCarsController(out _);

        var result = await controller.Update(999, new Car { Brand = "X", Model = "Y" });

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Cars_Delete_ShouldReturnNoContent_WhenCarExists()
    {
        var controller = CreateCarsController(out var db);
        var car = new Car { Brand = "Ford", Model = "Focus", VIN = "C5" };
        db.Cars.Add(car);
        await db.SaveChangesAsync();

        var result = await controller.Delete(car.Id);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Cars_Delete_ShouldReturnNotFound_WhenCarDoesNotExist()
    {
        var controller = CreateCarsController(out _);

        var result = await controller.Delete(999);

        result.Should().BeOfType<NotFoundResult>();
    }
}
