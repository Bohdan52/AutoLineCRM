using AutoLineCRM.SharedKernel.Interfaces;

namespace AutoLineCRM.PaymentService.Gateways;

public class CashPaymentGateway : IPaymentGateway
{
    public string ProviderName => "Cash";

    public Task<PaymentResult> ChargeAsync(decimal amount, string currency, string cardToken, CancellationToken ct = default)
    {
        if (amount <= 0)
            return Task.FromResult(new PaymentResult(false, null, "Сума має бути більшою за нуль"));
        return Task.FromResult(new PaymentResult(true, $"CASH-{Guid.NewGuid():N}", null));
    }

    public Task<PaymentResult> RefundAsync(string transactionId, decimal amount, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(transactionId) || amount <= 0)
            return Task.FromResult(new PaymentResult(false, null, "Некоректні дані для повернення"));
        return Task.FromResult(new PaymentResult(true, $"CASH-REFUND-{Guid.NewGuid():N}", null));
    }
}