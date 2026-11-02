using AutoLineCRM.SalesService.Controllers;
using AutoLineCRM.SalesService.Data;
using AutoLineCRM.SalesService.Dtos;
using AutoLineCRM.SalesService.Services;
using AutoLineCRM.SharedKernel.Entities;
using AutoLineCRM.SharedKernel.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AutoLineCRM.Tests;

// Тести заявок: RequestService та RequestsController
public class RequestServiceTests
{
    private static SalesDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<SalesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static RequestsController CreateRequestsController(out SalesDbContext db)
    {
        db = new SalesDbContext(new DbContextOptionsBuilder<SalesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        return new RequestsController(new RequestService(db));
    }

    // ===== RequestService: базові сценарії =====

    [Fact]
    public async Task CreateRequestAsync_ShouldCreateRequestWithNewStatus()
    {
        using var db = CreateDb();
        var service = new RequestService(db);

        var result = await service.CreateRequestAsync(new CreateRequestDto(CarId: 1, ClientId: 1));

        result.Id.Should().BeGreaterThan(0);
        result.Status.Should().Be(RequestStatus.New);
        result.CarId.Should().Be(1);
        result.ClientId.Should().Be(1);
        db.Requests.Count().Should().Be(1);
    }

    [Fact]
    public async Task GetClientRequestsAsync_ShouldReturnOnlySpecificClientRequests()
    {
        using var db = CreateDb();
        db.Requests.AddRange(
            new Request { ClientId = 1, CarId = 10 },
            new Request { ClientId = 2, CarId = 20 },
            new Request { ClientId = 1, CarId = 30 });
        await db.SaveChangesAsync();
        var service = new RequestService(db);

        var result = await service.GetClientRequestsAsync(clientId: 1);

        result.Should().HaveCount(2);
        result.All(r => r.ClientId == 1).Should().BeTrue();
    }

    // ===== RequestService: сортування, статуси, помилки =====

    [Fact]
    public async Task CreateRequestAsync_ShouldAssignDistinctIds()
    {
        using var db = CreateDb();
        var service = new RequestService(db);

        var first = await service.CreateRequestAsync(new CreateRequestDto(1, 1));
        var second = await service.CreateRequestAsync(new CreateRequestDto(2, 1));

        first.Id.Should().NotBe(second.Id);
        db.Requests.Count().Should().Be(2);
    }

    [Fact]
    public async Task GetClientRequestsAsync_ShouldReturnEmpty_ForUnknownClient()
    {
        using var db = CreateDb();
        db.Requests.Add(new Request { ClientId = 1, CarId = 10 });
        await db.SaveChangesAsync();
        var service = new RequestService(db);

        var result = await service.GetClientRequestsAsync(clientId: 42);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetClientRequestsAsync_ShouldSortNewestFirst()
    {
        using var db = CreateDb();
        db.Requests.AddRange(
            new Request { ClientId = 1, CarId = 10, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Request { ClientId = 1, CarId = 20, CreatedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Request { ClientId = 1, CarId = 30, CreatedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc) });
        await db.SaveChangesAsync();
        var service = new RequestService(db);

        var result = await service.GetClientRequestsAsync(1);

        result.Select(r => r.CarId).Should().Equal(20, 30, 10);
    }

    [Fact]
    public async Task GetAllRequestsAsync_ShouldReturnAllClients_NewestFirst()
    {
        using var db = CreateDb();
        db.Requests.AddRange(
            new Request { ClientId = 1, CarId = 10, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Request { ClientId = 2, CarId = 20, CreatedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc) });
        await db.SaveChangesAsync();
        var service = new RequestService(db);

        var result = await service.GetAllRequestsAsync();

        result.Select(r => r.CarId).Should().Equal(20, 10);
    }

    [Fact]
    public async Task UpdateRequestStatusAsync_ShouldChangeStatus()
    {
        using var db = CreateDb();
        var request = new Request { ClientId = 1, CarId = 10 };
        db.Requests.Add(request);
        await db.SaveChangesAsync();
        var service = new RequestService(db);

        var ok = await service.UpdateRequestStatusAsync(request.Id, RequestStatus.Confirmed);

        ok.Should().BeTrue();
        (await db.Requests.FindAsync(request.Id))!.Status.Should().Be(RequestStatus.Confirmed);
    }

    [Fact]
    public async Task UpdateRequestStatusAsync_ShouldReturnFalse_WhenRequestDoesNotExist()
    {
        using var db = CreateDb();
        var service = new RequestService(db);

        var ok = await service.UpdateRequestStatusAsync(999, RequestStatus.Paid);

        ok.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateRequestStatusAsync_ShouldSetUpdatedAt()
    {
        // Дефект D3: дата останньої зміни має заповнюватися
        using var db = CreateDb();
        var request = new Request { ClientId = 1, CarId = 10 };
        db.Requests.Add(request);
        await db.SaveChangesAsync();
        var service = new RequestService(db);

        await service.UpdateRequestStatusAsync(request.Id, RequestStatus.InProcessing);

        var stored = await db.Requests.FindAsync(request.Id);
        stored!.UpdatedAt.Should().NotBeNull();
        stored.UpdatedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateRequestStatusAsync_ShouldThrow_WhenStatusIsNotDefined()
    {
        // Дефект D3: контролер приводить int до enum без перевірки
        using var db = CreateDb();
        var request = new Request { ClientId = 1, CarId = 10 };
        db.Requests.Add(request);
        await db.SaveChangesAsync();
        var service = new RequestService(db);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateRequestStatusAsync(request.Id, (RequestStatus)99));
    }

    // ===== RequestsController: відповіді HTTP =====

    [Fact]
    public async Task Requests_Create_ShouldReturnOk()
    {
        var controller = CreateRequestsController(out var db);

        var result = await controller.Create(new CreateRequestDto(1, 1));

        result.Should().BeOfType<OkObjectResult>();
        db.Requests.Count().Should().Be(1);
    }

    [Fact]
    public async Task Requests_GetByClient_ShouldReturnOnlyThatClientsRequests()
    {
        var controller = CreateRequestsController(out var db);
        db.Requests.AddRange(
            new Request { ClientId = 1, CarId = 10 },
            new Request { ClientId = 2, CarId = 20 });
        await db.SaveChangesAsync();

        var result = await controller.GetByClient(1);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<List<Request>>().Which.Should().HaveCount(1);
    }

    [Fact]
    public async Task Requests_GetAll_ShouldReturnAllRequests()
    {
        var controller = CreateRequestsController(out var db);
        db.Requests.AddRange(
            new Request { ClientId = 1, CarId = 10 },
            new Request { ClientId = 2, CarId = 20 });
        await db.SaveChangesAsync();

        var result = await controller.GetAll();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<List<Request>>().Which.Should().HaveCount(2);
    }

    [Fact]
    public async Task Requests_UpdateStatus_ShouldReturnOk_WhenRequestExists()
    {
        var controller = CreateRequestsController(out var db);
        var request = new Request { ClientId = 1, CarId = 10 };
        db.Requests.Add(request);
        await db.SaveChangesAsync();

        var result = await controller.UpdateStatus(request.Id, new UpdateStatusDto((int)RequestStatus.Paid));

        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Requests_UpdateStatus_ShouldReturnNotFound_WhenRequestDoesNotExist()
    {
        var controller = CreateRequestsController(out _);

        var result = await controller.UpdateStatus(999, new UpdateStatusDto((int)RequestStatus.Paid));

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Requests_UpdateStatus_ShouldReturnBadRequest_WhenStatusIsUnknown()
    {
        var controller = CreateRequestsController(out var db);
        var request = new Request { ClientId = 1, CarId = 10 };
        db.Requests.Add(request);
        await db.SaveChangesAsync();

        var result = await controller.UpdateStatus(request.Id, new UpdateStatusDto(99));

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
