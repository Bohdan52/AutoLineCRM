using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoLineCRM.SharedKernel.Entities;
using AutoLineCRM.SharedKernel.Enums;
using AutoLineCRM.UserAuthService.Data;
using AutoLineCRM.UserAuthService.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AutoLineCRM.UserAuthService.Services;

public class AuthService
{
    private readonly AuthDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AuthDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Login == dto.Login))
            throw new InvalidOperationException("Логін вже зайнятий");

        var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        User user = dto.Role switch
        {
            UserRole.Client => new Client { Login = dto.Login, PasswordHash = hash, Email = dto.Email, FullName = dto.FullName, Phone = dto.Phone ?? "" },
            UserRole.Manager => new Manager { Login = dto.Login, PasswordHash = hash, Email = dto.Email, FullName = dto.FullName, Phone = dto.Phone ?? "" },
            UserRole.Administrator => new Administrator { Login = dto.Login, PasswordHash = hash, Email = dto.Email, FullName = dto.FullName },
            _ => throw new ArgumentException("Невідома роль")
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return new AuthResponseDto(GenerateToken(user), user.Login, user.Role.ToString());
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Login == dto.Login)
            ?? throw new UnauthorizedAccessException("Невірний логін або пароль");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Невірний логін або пароль");

        return new AuthResponseDto(GenerateToken(user), user.Login, user.Role.ToString());
    }

    private string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}