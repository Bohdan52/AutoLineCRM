using AutoLineCRM.SharedKernel.Entities;
using AutoLineCRM.SharedKernel.Enums;
using AutoLineCRM.UserAuthService.Data;
using AutoLineCRM.UserAuthService.Dtos;
using AutoLineCRM.UserAuthService.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AutoLineCRM.Tests;

public class AuthServiceTests
{
    // Хелпер для створення ізольованої In-Memory бази
    private AuthDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AuthDbContext(options);
    }

    // Хелпер для створення фейкової конфігурації (потрібної для JWT)
    private IConfiguration GetFakeConfiguration()
    {
        var inMemorySettings = new Dictionary<string, string> {
            {"Jwt:Key", "SuperSecretTestKeyForUnitTests1234567890"},
            {"Jwt:Issuer", "TestIssuer"},
            {"Jwt:Audience", "TestAudience"}
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateUserAndHashPassword()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var config = GetFakeConfiguration();
        var service = new AuthService(db, config);

        var dto = new RegisterDto(
            Login: "testuser",
            Password: "password123",
            Email: "test@test.com",
            FullName: "Test User",
            Phone: "+380123456789",
            Role: UserRole.Client);

        // Act
        var result = await service.RegisterAsync(dto);

        // Assert
        result.Token.Should().NotBeNullOrEmpty(); // Токен має згенеруватися
        result.Login.Should().Be("testuser");

        var userInDb = await db.Users.FirstOrDefaultAsync(u => u.Login == "testuser");
        userInDb.Should().NotBeNull();

        // Пароль не має зберігатися у відкритому вигляді
        userInDb!.PasswordHash.Should().NotBe("password123");
        // Перевіряємо, що хеш валідний
        BCrypt.Net.BCrypt.Verify("password123", userInDb.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowException_WhenLoginAlreadyExists()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var config = GetFakeConfiguration();
        var service = new AuthService(db, config);

        // Спочатку додаємо користувача вручну
        db.Users.Add(new Client { Login = "existinguser", Email = "old@test.com", PasswordHash = "hash" });
        await db.SaveChangesAsync();

        var dto = new RegisterDto("existinguser", "pass", "new@test.com", "Name", null, UserRole.Client);

        // Act & Assert
        // Очікуємо, що метод викине виняток, бо логін зайнятий
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(dto));
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnToken_WhenCredentialsAreValid()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var config = GetFakeConfiguration();
        var service = new AuthService(db, config);

        // Додаємо користувача з реальним хешем пароля
        var hash = BCrypt.Net.BCrypt.HashPassword("correctPassword");
        db.Users.Add(new Administrator { Login = "admin", Email = "admin@test.com", PasswordHash = hash });
        await db.SaveChangesAsync();

        var dto = new LoginDto("admin", "correctPassword");

        // Act
        var result = await service.LoginAsync(dto);

        // Assert
        result.Token.Should().NotBeNullOrEmpty();
        result.Role.Should().Be("Administrator");
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowException_WhenPasswordIsWrong()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var config = GetFakeConfiguration();
        var service = new AuthService(db, config);

        var hash = BCrypt.Net.BCrypt.HashPassword("correctPassword");
        db.Users.Add(new Client { Login = "user1", Email = "user1@test.com", PasswordHash = hash });
        await db.SaveChangesAsync();

        var dto = new LoginDto("user1", "wrongPassword"); // Вводимо неправильний пароль

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.LoginAsync(dto));
    }
}