using AutoLineCRM.SharedKernel.Interfaces;

namespace AutoLineCRM.PaymentService.Gateways;

public class CardPaymentGateway : IPaymentGateway
{
    public string ProviderName => "CardProcessor";

    public Task<PaymentResult> ChargeAsync(decimal amount, string currency, string cardToken, CancellationToken ct = default)
    {
        if (amount <= 0)
            return Task.FromResult(new PaymentResult(false, null, "Сума має бути більшою за нуль"));
        if (string.IsNullOrWhiteSpace(cardToken))
            return Task.FromResult(new PaymentResult(false, null, "Не вказано токен картки"));
        return Task.FromResult(new PaymentResult(true, $"CARD-{Guid.NewGuid():N}", null));
    }

    public Task<PaymentResult> RefundAsync(string transactionId, decimal amount, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(transactionId) || amount <= 0)
            return Task.FromResult(new PaymentResult(false, null, "Некоректні дані для повернення"));
        return Task.FromResult(new PaymentResult(true, $"REFUND-{Guid.NewGuid():N}", null));
    }
}