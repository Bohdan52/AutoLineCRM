using AutoLineCRM.SharedKernel.Entities;
using AutoLineCRM.SharedKernel.Enums;
using AutoLineCRM.UserAuthService.Dtos;

namespace AutoLineCRM.UserAuthService.Services;

public class UserFactory
{
    private readonly Dictionary<UserRole, UserCreator> _creators;

    public UserFactory() : this(new UserCreator[]
    {
        new ClientCreator(), new ManagerCreator(), new AdministratorCreator()
    })
    { }

    public UserFactory(IEnumerable<UserCreator> creators) =>
        _creators = creators.ToDictionary(c => c.Role);

    public User Create(RegisterDto dto, string passwordHash)
    {
        if (!_creators.TryGetValue(dto.Role, out var creator))
            throw new ArgumentException("Невідома роль");
        return creator.Create(dto, passwordHash);
    }
}