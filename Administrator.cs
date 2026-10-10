using AutoLineCRM.SharedKernel.Enums;

namespace AutoLineCRM.SharedKernel.Entities;

public class  Administrator: User
{
    public string FullName { get; set; } = string.Empty;
    public Administrator()
    {
        Role = UserRole.Administrator;
    }
    public override string ToString() => "Administrator";
}