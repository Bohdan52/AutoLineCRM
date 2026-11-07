using AutoLineCRM.SharedKernel.Interfaces;

namespace AutoLineCRM.PaymentService.Gateways;

public class BankTransferPaymentGateway : IPaymentGateway
{
    private static readonly HashSet<string> SupportedCurrencies =
        new(StringComparer.OrdinalIgnoreCase) { "UAH", "USD", "EUR" };

    public string ProviderName => "BankTransfer";

    public Task<PaymentResult> ChargeAsync(decimal amount, string currency, string cardToken, CancellationToken ct = default)
    {
        if (amount <= 0)
            return Task.FromResult(new PaymentResult(false, null, "Сума має бути більшою за нуль"));
        if (!SupportedCurrencies.Contains(currency ?? string.Empty))
            return Task.FromResult(new PaymentResult(false, null, $"Валюта {currency} не підтримується"));
        return Task.FromResult(new PaymentResult(true, $"BANK-{Guid.NewGuid():N}", null));
    }

    public Task<PaymentResult> RefundAsync(string transactionId, decimal amount, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(transactionId) || amount <= 0)
            return Task.FromResult(new PaymentResult(false, null, "Некоректні дані для повернення"));
        return Task.FromResult(new PaymentResult(true, $"BANK-REFUND-{Guid.NewGuid():N}", null));
    }
}