using AutoLineCRM.SalesService.Dtos;
using AutoLineCRM.SharedKernel.Entities;
using AutoLineCRM.SharedKernel.Enums;
using AutoLineCRM.SharedKernel.Events;
using AutoLineCRM.SharedKernel.Interfaces;
using AutoLineCRM.UserAuthService.Dtos;
using FluentAssertions;
using Xunit;

namespace AutoLineCRM.Tests;

// Тести доменної моделі: сутності, DTO та події SharedKernel
public class DomainEntityTests
{
    // Створює значення для властивості за її типом
    private static object? MakeValue(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type == typeof(string)) return "x";
        if (type == typeof(bool)) return true;
        if (type == typeof(int)) return 1;
        if (type == typeof(decimal)) return 1m;
        if (type == typeof(DateTime)) return DateTime.UtcNow;
        if (type.IsEnum) return Enum.GetValues(type).GetValue(0);
        if (type.IsGenericType)
            return Activator.CreateInstance(typeof(List<>).MakeGenericType(type.GetGenericArguments()));
        if (type.IsInterface || type.IsAbstract) return null;
        return Activator.CreateInstance(type);
    }

    // ===== Сутності =====

    [Fact]
    public void Client_ShouldHaveClientRole()
    {
        var user = new Client();
        user.Role.Should().Be(UserRole.Client);
        user.GetRoleName().Should().Be("Client");
    }

    [Fact]
    public void Manager_ShouldHaveManagerRole_AndDefaultCommission()
    {
        var user = new Manager();
        user.Role.Should().Be(UserRole.Manager);
        user.GetRoleName().Should().Be("Manager");
        user.CommissionRate.Should().Be(0.05m);
    }

    [Fact]
    public void Administrator_ShouldHaveAdministratorRole()
    {
        var user = new Administrator();
        user.Role.Should().Be(UserRole.Administrator);
        user.GetRoleName().Should().Be("Administrator");
    }

    [Fact]
    public void User_ShouldBeActiveByDefault_AndFormatToString()
    {
        var user = new Client { Login = "bob" };
        user.IsActive.Should().BeTrue();
        user.ToString().Should().Be("[0] bob (Client)");
    }

    [Fact]
    public void Car_ShouldBeAvailableByDefault()
    {
        var car = new Car();
        car.Status.Should().Be(CarStatus.Available);
        car.Images.Should().BeEmpty();
    }

    [Fact]
    public void Request_ShouldStartWithNewStatus_AndNoUpdateDate()
    {
        var request = new Request();
        request.Status.Should().Be(RequestStatus.New);
        request.UpdatedAt.Should().BeNull();
        request.Contract.Should().BeNull();
        request.Payments.Should().BeEmpty();
    }

    [Fact]
    public void Contract_ShouldBeSigned_OnlyWhenSignedAtIsSet()
    {
        var contract = new Contract();
        contract.IsSigned.Should().BeFalse();

        contract.SignedAt = DateTime.UtcNow;
        contract.IsSigned.Should().BeTrue();
    }

    [Fact]
    public void AllEntities_ShouldAllowSettingAndReadingEveryProperty()
    {
        var entityTypes = typeof(Car).Assembly.GetTypes()
            .Where(t => t.Namespace == "AutoLineCRM.SharedKernel.Entities" && t.IsClass && !t.IsAbstract)
            .ToList();
        entityTypes.Should().NotBeEmpty();

        foreach (var type in entityTypes)
        {
            var instance = Activator.CreateInstance(type)!;
            foreach (var property in type.GetProperties())
            {
                if (property.GetSetMethod() is not null)
                {
                    var value = MakeValue(property.PropertyType);
                    if (value is not null) property.SetValue(instance, value);
                }
                property.GetValue(instance);
            }
            instance.ToString().Should().NotBeNull();
            if (instance is User user) user.GetRoleName().Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public void Car_ToString_ShouldContainBrandModelAndStatus()
    {
        var car = new Car { Id = 3, Brand = "Audi", Model = "A6", Year = 2020, Price = 100, Status = CarStatus.Sold };

        car.ToString().Should().Contain("Audi").And.Contain("A6").And.Contain("Sold");
    }

    [Fact]
    public void Request_ToString_ShouldContainClientCarAndStatus()
    {
        var request = new Request { Id = 7, ClientId = 2, CarId = 9 };

        request.ToString().Should().Contain("#2").And.Contain("#9").And.Contain("New");
    }

    // ===== DTO та події (записи) =====

    [Fact]
    public void SalesDtos_ShouldSupportRecordSemantics()
    {
        var create = new CreateRequestDto(1, 2);
        var createCopy = create with { };
        (create == createCopy).Should().BeTrue();
        (create != (create with { CarId = 5 })).Should().BeTrue();
        create.Equals((object)createCopy).Should().BeTrue();
        create.GetHashCode().Should().Be(createCopy.GetHashCode());
        create.ToString().Should().Contain("CarId");
        var (carId, clientId) = create;
        carId.Should().Be(1);
        clientId.Should().Be(2);

        var update = new UpdateStatusDto(3);
        var updateCopy = update with { };
        (update == updateCopy).Should().BeTrue();
        (update != (update with { Status = 4 })).Should().BeTrue();
        update.Equals((object)updateCopy).Should().BeTrue();
        update.GetHashCode().Should().Be(updateCopy.GetHashCode());
        update.ToString().Should().Contain("Status");
        var (status, _) = (update.Status, 0);
        status.Should().Be(3);
    }

    [Fact]
    public void AuthDtos_ShouldSupportRecordSemantics()
    {
        var register = new RegisterDto("l", "p", "e@e.com", "Full", "123", UserRole.Client);
        var registerCopy = register with { };
        (register == registerCopy).Should().BeTrue();
        (register != (register with { Login = "z" })).Should().BeTrue();
        register.Equals((object)registerCopy).Should().BeTrue();
        register.GetHashCode().Should().Be(registerCopy.GetHashCode());
        register.ToString().Should().Contain("Login");
        var (login, password, email, fullName, phone, role) = register;
        (login, password, email, fullName, phone, role).Should().Be(("l", "p", "e@e.com", "Full", "123", UserRole.Client));

        var loginDto = new LoginDto("l", "p");
        var loginCopy = loginDto with { };
        (loginDto == loginCopy).Should().BeTrue();
        (loginDto != (loginDto with { Password = "z" })).Should().BeTrue();
        loginDto.Equals((object)loginCopy).Should().BeTrue();
        loginDto.GetHashCode().Should().Be(loginCopy.GetHashCode());
        loginDto.ToString().Should().Contain("Login");
        var (l2, p2) = loginDto;
        (l2, p2).Should().Be(("l", "p"));

        var response = new AuthResponseDto("t", "l", "Client");
        var responseCopy = response with { };
        (response == responseCopy).Should().BeTrue();
        (response != (response with { Token = "z" })).Should().BeTrue();
        response.Equals((object)responseCopy).Should().BeTrue();
        response.GetHashCode().Should().Be(responseCopy.GetHashCode());
        response.ToString().Should().Contain("Token");
        var (token, login3, role3) = response;
        (token, login3, role3).Should().Be(("t", "l", "Client"));
    }

    [Fact]
    public void ContractSignedEvent_ShouldSupportRecordSemantics()
    {
        var ev = new ContractSignedEvent(10, 20, 30);
        ev.EventId.Should().NotBe(Guid.Empty);
        ev.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        var copy = ev with { };
        (ev == copy).Should().BeTrue();
        (ev != (ev with { ContractId = 11 })).Should().BeTrue();
        ev.Equals((object)copy).Should().BeTrue();
        ev.GetHashCode().Should().Be(copy.GetHashCode());
        ev.ToString().Should().Contain("ContractId");
        var (_, _, contractId, requestId, clientId) = ev;
        (contractId, requestId, clientId).Should().Be((10, 20, 30));
        ((IDomainEvent)ev).EventId.Should().Be(ev.EventId);
    }

    [Fact]
    public void PaymentProcessedEvent_ShouldSupportRecordSemantics()
    {
        var ev = new PaymentProcessedEvent(1, 2, 99.5m, true);
        ev.EventId.Should().NotBe(Guid.Empty);
        var copy = ev with { };
        (ev == copy).Should().BeTrue();
        (ev != (ev with { Success = false })).Should().BeTrue();
        ev.Equals((object)copy).Should().BeTrue();
        ev.GetHashCode().Should().Be(copy.GetHashCode());
        ev.ToString().Should().Contain("Amount");
        var (_, _, paymentId, requestId, amount, success) = ev;
        (paymentId, requestId, amount, success).Should().Be((1, 2, 99.5m, true));
    }

    [Fact]
    public void RequestCreatedEvent_ShouldSupportRecordSemantics()
    {
        var ev = new RequestCreatedEvent(5, 6, 7);
        ev.EventId.Should().NotBe(Guid.Empty);
        var copy = ev with { };
        (ev == copy).Should().BeTrue();
        (ev != (ev with { RequestId = 8 })).Should().BeTrue();
        ev.Equals((object)copy).Should().BeTrue();
        ev.GetHashCode().Should().Be(copy.GetHashCode());
        ev.ToString().Should().Contain("RequestId");
        var (_, _, requestId, clientId) = ev;
        (requestId, clientId).Should().Be((5, 6));
    }

    [Fact]
    public void PaymentResult_ShouldSupportRecordSemantics()
    {
        var result = new PaymentResult(true, "tx1", null);
        var copy = result with { };
        (result == copy).Should().BeTrue();
        (result != (result with { Success = false })).Should().BeTrue();
        result.Equals((object)copy).Should().BeTrue();
        result.GetHashCode().Should().Be(copy.GetHashCode());
        result.ToString().Should().Contain("TransactionId");
        var (success, transactionId, errorMessage) = result;
        (success, transactionId, errorMessage).Should().Be((true, "tx1", (string?)null));
    }
}
