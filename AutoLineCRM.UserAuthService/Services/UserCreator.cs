using AutoLineCRM.SharedKernel.Entities;
using AutoLineCRM.SharedKernel.Enums;
using AutoLineCRM.UserAuthService.Dtos;

namespace AutoLineCRM.UserAuthService.Services;

public abstract class UserCreator
{
    public abstract UserRole Role { get; }
    protected abstract User CreateUser(RegisterDto dto);

    public User Create(RegisterDto dto, string passwordHash)
    {
        var user = CreateUser(dto);
        user.Login = dto.Login;
        user.Email = dto.Email;
        user.PasswordHash = passwordHash;
        return user;
    }
}

public class ClientCreator : UserCreator
{
    public override UserRole Role => UserRole.Client;
    protected override User CreateUser(RegisterDto dto) =>
        new Client { FullName = dto.FullName, Phone = dto.Phone ?? "" };
}

public class ManagerCreator : UserCreator
{
    public override UserRole Role => UserRole.Manager;
    protected override User CreateUser(RegisterDto dto) =>
        new Manager { FullName = dto.FullName, Phone = dto.Phone ?? "" };
}

public class AdministratorCreator : UserCreator
{
    public override UserRole Role => UserRole.Administrator;
    protected override User CreateUser(RegisterDto dto) =>
        new Administrator { FullName = dto.FullName };
}