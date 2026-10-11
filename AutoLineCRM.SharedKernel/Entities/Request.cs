using AutoLineCRM.SharedKernel.Enums;

namespace AutoLineCRM.SharedKernel.Entities;

public class  Request
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.New;
    public string Notes { get; set; } = string.Empty;   

    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public int CarId { get; set; }
    public Car Car { get; set; } = null!;

    public int? ManagerID { get; set; }
    public Manager Manager { get; set; }

    public Contract? Contract { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public override string ToString() => $"[{Id}] Заявка клієнта #{ClientId} на авто #{CarId} — {Status}";
}