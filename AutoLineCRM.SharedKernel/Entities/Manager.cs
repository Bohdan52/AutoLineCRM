using AutoLineCRM.SharedKernel.Enums;

namespace AutoLineCRM.SharedKernel.Entities;

public class Manager: User
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public decimal CommissionRate { get; set; } = 0.05m;
    public int SalesCount { get; set; }

    public ICollection<Request> ProcessedRequests { get; set; } = new List<Request>();
    public ICollection<Contract> CreatedContracts { get; set; } = new List<Contract>();

    public override string GetRoleName() => "Manager";

    public Manager ()
    {
        Role = UserRole.Manager;
    }
    
}