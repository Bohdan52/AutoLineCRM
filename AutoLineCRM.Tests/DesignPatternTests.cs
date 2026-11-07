using AutoLineCRM.PaymentService.Gateways;
using AutoLineCRM.PaymentService.Services;
using AutoLineCRM.SalesService.Data;
using AutoLineCRM.SalesService.Dtos;
using AutoLineCRM.SalesService.Services;
using AutoLineCRM.SharedKernel.Entities;
using AutoLineCRM.SharedKernel.Enums;
using AutoLineCRM.SharedKernel.Events;
using AutoLineCRM.SharedKernel.Interfaces;
using AutoLineCRM.UserAuthService.Dtos;
using AutoLineCRM.UserAuthService.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AutoLineCRM.Tests;

public class DesignPatternTests
{
    // ---------- Factory Method ----------

    [Theory]
    [InlineData(UserRole.Client, typeof(Client))]
    [InlineData(UserRole.Manager, typeof(Manager))]
    [InlineData(UserRole.Administrator, typeof(Administrator))]
    public void UserFactory_ShouldCreateUserOfRequestedType(UserRole role, Type expected)
    {
        var dto = new RegisterDto("login", "pass", "a@b.com", "Іван Петренко", "+380501234567", role);

        var user = new UserFactory().Create(dto, "hash");

        user.Should().BeOfType(expected);
        user.Role.Should().Be(role);
        user.Login.Should().Be("login");
        user.Email.Should().Be("a@b.com");
        user.PasswordHash.Should().Be("hash");
    }

    [Fact]
    public void UserFactory_ShouldFillRoleSpecificFields()
    {
        var factory = new UserFactory();

        var client = (Client)factory.Create(
            new RegisterDto("c", "p", "c@b.com", "Клієнт", "123", UserRole.Client), "h");
        var admin = (Administrator)factory.Create(
            new RegisterDto("a", "p", "a@b.com", "Адмін", null, UserRole.Administrator), "h");

        client.FullName.Should().Be("Клієнт");
        client.Phone.Should().Be("123");
        admin.FullName.Should().Be("Адмін");
    }

    [Fact]
    public void UserFactory_WithUnknownRole_ShouldThrow()
    {
        var dto = new RegisterDto("l", "p", "e@b.com", "N", null, (UserRole)99);

        var act = () => new UserFactory().Create(dto, "h");

        act.Should().Throw<ArgumentException>();
    }

    private sealed class ExtraCreator : UserCreator
    {
        public override UserRole Role => UserRole.Manager;
        protected override User CreateUser(RegisterDto dto) => new Manager { FullName = "custom" };
    }

    [Fact]
    public void UserFactory_ShouldUseInjectedCreators()
    {
        var factory = new UserFactory(new UserCreator[] { new ExtraCreator() });
        var dto = new RegisterDto("m", "p", "m@b.com", "x", null, UserRole.Manager);

        var user = (Manager)factory.Create(dto, "h");

        user.FullName.Should().Be("custom");
    }

    // ---------- Strategy ----------

    private static PaymentProcessor CreateProcessor(IEventBus? bus = null) =>
        new(new Dictionary<PaymentMethod, IPaymentGateway>
        {
            [PaymentMethod.Credit] = new CardPaymentGateway(),
            [PaymentMethod.Cash] = new CashPaymentGateway(),
            [PaymentMethod.BankTransfer] = new BankTransferPaymentGateway()
        }, bus);

    private static Payment NewPayment(PaymentMethod method, decimal amount = 1000m) =>
        new() { Id = 1, RequestId = 5, Amount = amount, Method = method, Status = PaymentStatus.Pending };

    [Theory]
    [InlineData(PaymentMethod.Credit, "CardProcessor")]
    [InlineData(PaymentMethod.Cash, "Cash")]
    [InlineData(PaymentMethod.BankTransfer, "BankTransfer")]
    public async Task PaymentProcessor_ShouldPickGatewayByMethod(PaymentMethod method, string provider)
    {
        var payment = NewPayment(method);

        var result = await CreateProcessor().ProcessAsync(payment, "UAH", "tok");

        result.Success.Should().BeTrue();
        payment.ProviderName.Should().Be(provider);
        payment.Status.Should().Be(PaymentStatus.Completed);
        payment.TransactionId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PaymentProcessor_CardWithoutToken_ShouldFail()
    {
        var payment = NewPayment(PaymentMethod.Credit);

        var result = await CreateProcessor().ProcessAsync(payment, "UAH", "");

        result.Success.Should().BeFalse();
        payment.Status.Should().Be(PaymentStatus.Failed);
    }

    [Fact]
    public async Task PaymentProcessor_BankTransferWithUnsupportedCurrency_ShouldFail()
    {
        var payment = NewPayment(PaymentMethod.BankTransfer);

        var result = await CreateProcessor().ProcessAsync(payment, "XXX", "");

        result.Success.Should().BeFalse();
        payment.Status.Should().Be(PaymentStatus.Failed);
    }

    [Fact]
    public async Task PaymentProcessor_ZeroAmount_ShouldFail()
    {
        var result = await CreateProcessor().ProcessAsync(NewPayment(PaymentMethod.Cash, 0m), "UAH", "");

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task PaymentProcessor_UnsupportedMethod_ShouldThrow()
    {
        var processor = CreateProcessor();

        processor.Supports(PaymentMethod.PayPal).Should().BeFalse();
        var act = () => processor.ProcessAsync(NewPayment(PaymentMethod.PayPal), "UAH", "");

        await act.Should().ThrowAsync<NotSupportedException>();
    }

    [Fact]
    public async Task PaymentProcessor_Refund_ShouldMarkPaymentRefunded()
    {
        var processor = CreateProcessor();
        var payment = NewPayment(PaymentMethod.Credit);
        await processor.ProcessAsync(payment, "UAH", "tok");

        var result = await processor.RefundAsync(payment);

        result.Success.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Refunded);
    }

    [Fact]
    public async Task PaymentProcessor_RefundOfPendingPayment_ShouldThrow()
    {
        var act = () => CreateProcessor().RefundAsync(NewPayment(PaymentMethod.Cash));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Gateways_RefundWithInvalidData_ShouldFail()
    {
        IPaymentGateway[] gateways =
            { new CardPaymentGateway(), new CashPaymentGateway(), new BankTransferPaymentGateway() };

        foreach (var gateway in gateways)
        {
            var result = await gateway.RefundAsync("", 10m);
            result.Success.Should().BeFalse();
        }
    }

    // ---------- Observer ----------

    [Fact]
    public async Task EventBus_ShouldNotifyAllSubscribers()
    {
        var bus = new InMemoryEventBus();
        var calls = new List<string>();
        bus.Subscribe<RequestCreatedEvent>((e, _) => { calls.Add("first"); return Task.CompletedTask; });
        bus.Subscribe<RequestCreatedEvent>((e, _) => { calls.Add("second"); return Task.CompletedTask; });

        await bus.PublishAsync(new RequestCreatedEvent(1, 2, 3));

        calls.Should().Equal("first", "second");
    }

    [Fact]
    public async Task EventBus_ShouldDeliverOnlyEventsOfSubscribedType()
    {
        var bus = new InMemoryEventBus();
        var called = false;
        bus.Subscribe<RequestCreatedEvent>((e, _) => { called = true; return Task.CompletedTask; });

        await bus.PublishAsync(new PaymentProcessedEvent(1, 1, 10m, true));

        called.Should().BeFalse();
    }

    [Fact]
    public async Task EventBus_WithoutSubscribers_ShouldNotThrow()
    {
        var act = () => new InMemoryEventBus().PublishAsync(new RequestCreatedEvent(1, 2, 3));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EventBus_FailingHandler_ShouldNotBreakOthers()
    {
        var errors = new List<Exception>();
        var bus = new InMemoryEventBus(errors.Add);
        var secondCalled = false;
        bus.Subscribe<RequestCreatedEvent>((e, _) => throw new InvalidOperationException("boom"));
        bus.Subscribe<RequestCreatedEvent>((e, _) => { secondCalled = true; return Task.CompletedTask; });

        await bus.PublishAsync(new RequestCreatedEvent(1, 2, 3));

        secondCalled.Should().BeTrue();
        errors.Should().ContainSingle().Which.Message.Should().Be("boom");
    }

    [Fact]
    public async Task EventBus_CancelledToken_ShouldPropagateCancellation()
    {
        var bus = new InMemoryEventBus();
        using var cts = new CancellationTokenSource();
        bus.Subscribe<RequestCreatedEvent>((e, ct) => { cts.Cancel(); ct.ThrowIfCancellationRequested(); return Task.CompletedTask; });

        var act = () => bus.PublishAsync(new RequestCreatedEvent(1, 2, 3), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task EventBus_NullArguments_ShouldThrow()
    {
        var bus = new InMemoryEventBus();

        var subscribe = () => bus.Subscribe<RequestCreatedEvent>(null!);
        var publish = () => bus.PublishAsync<RequestCreatedEvent>(null!);

        subscribe.Should().Throw<ArgumentNullException>();
        await publish.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RequestService_ShouldPublishRequestCreatedEvent()
    {
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using var db = new SalesDbContext(options);
        var bus = new InMemoryEventBus();
        RequestCreatedEvent? received = null;
        bus.Subscribe<RequestCreatedEvent>((e, _) => { received = e; return Task.CompletedTask; });
        var service = new RequestService(db, bus);

        var request = await service.CreateRequestAsync(new CreateRequestDto(CarId: 7, ClientId: 3));

        received.Should().NotBeNull();
        received!.RequestId.Should().Be(request.Id);
        received.ClientId.Should().Be(3);
    }

    [Fact]
    public async Task PaymentProcessor_ShouldPublishPaymentProcessedEvent()
    {
        var bus = new InMemoryEventBus();
        PaymentProcessedEvent? received = null;
        bus.Subscribe<PaymentProcessedEvent>((e, _) => { received = e; return Task.CompletedTask; });

        await CreateProcessor(bus).ProcessAsync(NewPayment(PaymentMethod.Cash, 500m), "UAH", "");

        received.Should().NotBeNull();
        received!.Success.Should().BeTrue();
        received.Amount.Should().Be(500m);
        received.RequestId.Should().Be(5);
    }
}