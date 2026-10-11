namespace AutoLineCRM.SharedKernel.Entities;

public class Contract
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SignedAt { get; set; }
    public decimal Amount { get; set; }

    public string? DigitalSignature { get; set; }
    public bool IsSigned => SignedAt.HasValue;

    public int RequestId { get; set; }
    public Request Request { get; set; } = null!;

    public int ManagerId { get; set; }
    public Manager Manager { get; set; } = null!;

    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;
}