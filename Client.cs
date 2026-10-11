using AutoLineCRM.SharedKernel.Enums;

namespace AutoLineCRM.SharedKernel.Entities;

public class Client : User
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PassportData { get; set; } = string.Empty;

    public ICollection<Request> Requests { get; set; } = new List<Request>();
    public ICollection<Review> Reviews{ get; set; } = new List<Review>();
    public ICollection<Contract> Contracts{ get; set; } = new List<Contract>();

    public abstract string GetRoleName() => "Client";

    public Client()
    {
        Role = UserRole.Client;
    }
 
}