using AutoLineCRM.SharedKernel.Enums;

namespace AutoLineCRM.SharedKernel.Entities;

public class Administrator: User
{
    public string FullName { get; set; } = string.Empty;
    public override string GetRoleName() => "Administrator";
    public Administrator()
    {
        Role = UserRole.Administrator;
    }
}