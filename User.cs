using AutoLineCRM.SharedKernel.Enums;

namespace AutoLineCRM.SharedKernel.Entities;

public abstract class User
{
    public int Id { get; set;}
    public string Login { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public UserRole Role { get; protected set; }

    public DateTime CreatedAt { get; set; } = DateTime.UTCNow;
    public bool IsActive { get; set; } = true;

    public abstract string GetRoleName();

    public override string ToString() => $"[{Id}] {Login} ({Role})";
}