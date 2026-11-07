using AutoLineCRM.SharedKernel.Entities;
using AutoLineCRM.SharedKernel.Enums;
using AutoLineCRM.SharedKernel.Events;
using AutoLineCRM.SharedKernel.Interfaces;

namespace AutoLineCRM.PaymentService.Services;

public class PaymentProcessor
{
    private readonly IReadOnlyDictionary<PaymentMethod, IPaymentGateway> _gateways;
    private readonly IEventBus? _eventBus;

    public PaymentProcessor(IReadOnlyDictionary<PaymentMethod, IPaymentGateway> gateways, IEventBus? eventBus = null)
    {
        _gateways = gateways;
        _eventBus = eventBus;
    }

    public bool Supports(PaymentMethod method) => _gateways.ContainsKey(method);

    public async Task<PaymentResult> ProcessAsync(Payment payment, string currency, string cardToken, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(payment);
        var gateway = GetGateway(payment.Method);
        var result = await gateway.ChargeAsync(payment.Amount, currency, cardToken, ct);

        payment.ProviderName = gateway.ProviderName;
        payment.TransactionId = result.TransactionId;
        payment.Status = result.Success ? PaymentStatus.Completed : PaymentStatus.Failed;

        if (_eventBus is not null)
            await _eventBus.PublishAsync(
                new PaymentProcessedEvent(payment.Id, payment.RequestId, payment.Amount, result.Success), ct);

        return result;
    }

    public async Task<PaymentResult> RefundAsync(Payment payment, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(payment);
        if (payment.Status != PaymentStatus.Completed || string.IsNullOrEmpty(payment.TransactionId))
            throw new InvalidOperationException("Повернути можна лише завершений платіж");

        var gateway = GetGateway(payment.Method);
        var result = await gateway.RefundAsync(payment.TransactionId, payment.Amount, ct);
        if (result.Success) payment.Status = PaymentStatus.Refunded;
        return result;
    }

    private IPaymentGateway GetGateway(PaymentMethod method) =>
        _gateways.TryGetValue(method, out var gateway)
            ? gateway
            : throw new NotSupportedException($"Спосіб оплати {method} не підтримується");
}