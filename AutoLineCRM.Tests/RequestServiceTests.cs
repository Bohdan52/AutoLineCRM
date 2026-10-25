using AutoLineCRM.SalesService.Data;
using AutoLineCRM.SalesService.Dtos;
using AutoLineCRM.SalesService.Services;
using AutoLineCRM.SharedKernel.Entities;
using AutoLineCRM.SharedKernel.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AutoLineCRM.Tests;

public class RequestServiceTests
{
    // Хелпер для створення ізольованої In-Memory бази
    private SalesDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new SalesDbContext(options);
    }

    [Fact]
    public async Task CreateRequestAsync_ShouldCreateNewRequest()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var service = new RequestService(db);
        var dto = new CreateRequestDto(CarId: 1, ClientId: 1);

        // Act
        var result = await service.CreateRequestAsync(dto);

        // Assert
        result.Id.Should().BeGreaterThan(0); // ID згенеровано
        result.Status.Should().Be(RequestStatus.New); // Статус за замовчуванням - "Нова"
        result.CarId.Should().Be(1);
        result.ClientId.Should().Be(1);
        db.Requests.Count().Should().Be(1); // Заявка збережена в базі
    }

    [Fact]
    public async Task GetClientRequestsAsync_ShouldReturnOnlySpecificClientRequests()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        // Додаємо заявки для різних клієнтів
        db.Requests.AddRange(
            new Request { ClientId = 1, CarId = 10, CreatedAt = DateTime.UtcNow },
            new Request { ClientId = 2, CarId = 20, CreatedAt = DateTime.UtcNow },
            new Request { ClientId = 1, CarId = 30, CreatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var service = new RequestService(db);

        // Act
        var result = await service.GetClientRequestsAsync(clientId: 1);

        // Assert
        result.Should().HaveCount(2); // Має знайти лише 2 заявки клієнта №1
        result.All(r => r.ClientId == 1).Should().BeTrue();
    }
}