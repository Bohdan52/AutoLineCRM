namespace AutoLineCRM.SharedKernel.Enums;

public interface IPaymentGateway
{
    string ProviderName { get; }

    Task<PaymentResult> ChargeAsync(decimal amount, string currency, string cardToken, CancellationToken ct = default);

    Task<PaymentResult> RefundAsync(string transactionId, decimal amount, CancellationToken ct = default);
}

public record PaymentResult(bool Success, string? TransactionId, string? ErrorMessage);