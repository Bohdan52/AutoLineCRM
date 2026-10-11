using AutoLineCRM.SharedKernel.Enums;

namespace AutoLineCRM.SharedKernel.Entities;

public class Payment
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime DateTime { get; set; } = DateTime.UtcNow;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public PaymentMethod Method { get; set; }

    public string? TransactionId { get; set; }
    public string? ProviderName { get; set; }

    public int RequestId { get; set; }
    public Request Request { get; set; } = null!;
}