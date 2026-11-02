using AutoLineCRM.SharedKernel.Entities;
using AutoLineCRM.SharedKernel.Enums;
using AutoLineCRM.UserAuthService.Controllers;
using AutoLineCRM.UserAuthService.Data;
using AutoLineCRM.UserAuthService.Dtos;
using AutoLineCRM.UserAuthService.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace AutoLineCRM.Tests;

// Тести автентифікації: AuthService та AuthController
public class AuthServiceTests
{
    private static AuthDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static IConfiguration CreateConfig() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "SuperSecretTestKeyForUnitTests1234567890",
            ["Jwt:Issuer"] = "TestIssuer",
            ["Jwt:Audience"] = "TestAudience"
        }).Build();

    private static RegisterDto ClientDto(string login, string email) =>
        new(login, "password123", email, "Test User", "+380501112233", UserRole.Client);

    private static AuthController CreateAuthController()
    {
        var db = new AuthDbContext(new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "SuperSecretTestKeyForUnitTests1234567890",
            ["Jwt:Issuer"] = "TestIssuer",
            ["Jwt:Audience"] = "TestAudience"
        }).Build();
        return new AuthController(new AuthService(db, config));
    }

    // ===== AuthService: базові сценарії =====

    [Fact]
    public async Task RegisterAsync_ShouldStorePasswordAsBCryptHash()
    {
        using var db = CreateDb();
        var service = new AuthService(db, CreateConfig());

        var result = await service.RegisterAsync(new RegisterDto(
            "testuser", "password123", "test@test.com", "Test User", "+380123456789", UserRole.Client));

        result.Token.Should().NotBeNullOrEmpty();
        result.Login.Should().Be("testuser");
        var stored = await db.Users.FirstAsync(u => u.Login == "testuser");
        stored.PasswordHash.Should().NotBe("password123");
        BCrypt.Net.BCrypt.Verify("password123", stored.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrow_WhenLoginAlreadyExists()
    {
        using var db = CreateDb();
        db.Users.Add(new Client { Login = "existinguser", Email = "old@test.com", PasswordHash = "hash" });
        await db.SaveChangesAsync();
        var service = new AuthService(db, CreateConfig());
        var dto = new RegisterDto("existinguser", "pass", "new@test.com", "Name", null, UserRole.Client);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(dto));
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnTokenAndRole_ForAdministrator()
    {
        using var db = CreateDb();
        db.Users.Add(new Administrator
        {
            Login = "admin",
            Email = "admin@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correctPassword")
        });
        await db.SaveChangesAsync();
        var service = new AuthService(db, CreateConfig());

        var result = await service.LoginAsync(new LoginDto("admin", "correctPassword"));

        result.Token.Should().NotBeNullOrEmpty();
        result.Role.Should().Be("Administrator");
    }

    [Fact]
    public async Task LoginAsync_ShouldThrow_WhenPasswordIsWrong()
    {
        using var db = CreateDb();
        db.Users.Add(new Client
        {
            Login = "user1",
            Email = "user1@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correctPassword")
        });
        await db.SaveChangesAsync();
        var service = new AuthService(db, CreateConfig());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.LoginAsync(new LoginDto("user1", "wrongPassword")));
    }

    // ===== AuthService: профіль, токен, ролі, унікальність =====

    [Fact]
    public async Task RegisterAsync_ShouldStoreClientProfile()
    {
        using var db = CreateDb();
        var service = new AuthService(db, CreateConfig());

        await service.RegisterAsync(ClientDto("anna", "anna@test.com"));

        var client = await db.Users.OfType<Client>().FirstAsync(c => c.Login == "anna");
        client.FullName.Should().Be("Test User");
        client.Phone.Should().Be("+380501112233");
        client.Email.Should().Be("anna@test.com");
        client.Role.Should().Be(UserRole.Client);
    }

    [Fact]
    public async Task RegisterAsync_ShouldUseEmptyPhone_WhenPhoneIsNull()
    {
        using var db = CreateDb();
        var service = new AuthService(db, CreateConfig());

        await service.RegisterAsync(new RegisterDto("noph", "password123", "noph@test.com", "No Phone", null, UserRole.Client));

        (await db.Users.OfType<Client>().FirstAsync(c => c.Login == "noph")).Phone.Should().BeEmpty();
    }

    [Fact]
    public async Task RegisterAsync_ShouldGenerateDifferentHashes_ForSamePassword()
    {
        using var db = CreateDb();
        var service = new AuthService(db, CreateConfig());

        await service.RegisterAsync(ClientDto("u1", "u1@test.com"));
        await service.RegisterAsync(ClientDto("u2", "u2@test.com"));

        var hashes = await db.Users.Select(u => u.PasswordHash).ToListAsync();
        hashes.Distinct().Should().HaveCount(2); // сіль робить хеші унікальними
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowArgumentException_ForUnknownRole()
    {
        using var db = CreateDb();
        var service = new AuthService(db, CreateConfig());
        var dto = new RegisterDto("x", "password123", "x@test.com", "X", null, (UserRole)99);

        await Assert.ThrowsAsync<ArgumentException>(() => service.RegisterAsync(dto));
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrow_WhenEmailAlreadyExists()
    {
        // Дефект D2: електронна пошта має бути унікальною (InMemory не перевіряє індекс)
        using var db = CreateDb();
        var service = new AuthService(db, CreateConfig());
        await service.RegisterAsync(ClientDto("first", "same@test.com"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RegisterAsync(ClientDto("second", "same@test.com")));
    }

    [Fact]
    public async Task LoginAsync_ShouldThrow_WhenLoginDoesNotExist()
    {
        using var db = CreateDb();
        var service = new AuthService(db, CreateConfig());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.LoginAsync(new LoginDto("ghost", "whatever")));
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnManagerRole()
    {
        using var db = CreateDb();
        db.Users.Add(new Manager
        {
            Login = "mgr", Email = "mgr@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("secret123")
        });
        await db.SaveChangesAsync();
        var service = new AuthService(db, CreateConfig());

        var result = await service.LoginAsync(new LoginDto("mgr", "secret123"));

        result.Role.Should().Be("Manager");
        result.Login.Should().Be("mgr");
    }

    [Fact]
    public async Task LoginAsync_ShouldIssueTokenWithConfiguredIssuerAudienceAndRole()
    {
        using var db = CreateDb();
        var service = new AuthService(db, CreateConfig());
        await service.RegisterAsync(ClientDto("tok", "tok@test.com"));

        var result = await service.LoginAsync(new LoginDto("tok", "password123"));
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        jwt.Issuer.Should().Be("TestIssuer");
        jwt.Audiences.Should().Contain("TestAudience");
        jwt.ValidTo.Should().BeAfter(DateTime.UtcNow);
        jwt.Claims.Should().Contain(c => c.Value == "Client");
        jwt.Claims.Should().Contain(c => c.Value == "tok");
    }

    [Fact]
    public async Task Controller_Login_ShouldReturnUnauthorized_ForWrongPassword()
    {
        using var db = CreateDb();
        var service = new AuthService(db, CreateConfig());
        await service.RegisterAsync(ClientDto("ctl", "ctl@test.com"));
        var controller = new AuthController(service);

        var result = await controller.Login(new LoginDto("ctl", "wrong"));

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Controller_Register_ShouldReturnConflict_WhenLoginTaken()
    {
        using var db = CreateDb();
        var service = new AuthService(db, CreateConfig());
        var controller = new AuthController(service);
        await controller.Register(ClientDto("dup", "dup1@test.com"));

        var result = await controller.Register(ClientDto("dup", "dup2@test.com"));

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Theory]
    [InlineData(UserRole.Manager)]
    [InlineData(UserRole.Administrator)]
    public async Task Controller_Register_ShouldForbidPrivilegedRoles(UserRole role)
    {
        // Дефект D4: через відкриту реєстрацію не можна створити менеджера чи адміністратора
        using var db = CreateDb();
        var controller = new AuthController(new AuthService(db, CreateConfig()));
        var dto = new RegisterDto("evil", "password123", "evil@test.com", "Evil", null, role);

        var result = await controller.Register(dto);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
        db.Users.Should().BeEmpty();
    }

    [Theory]
    [InlineData(UserRole.Manager, "Manager")]
    [InlineData(UserRole.Administrator, "Administrator")]
    public async Task AuthService_Register_ShouldCreateUserOfRequestedRole(UserRole role, string expectedRole)
    {
        using var db = new AuthDbContext(new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "SuperSecretTestKeyForUnitTests1234567890",
            ["Jwt:Issuer"] = "TestIssuer",
            ["Jwt:Audience"] = "TestAudience"
        }).Build();
        var service = new AuthService(db, config);

        var result = await service.RegisterAsync(
            new RegisterDto("staff", "password123", "staff@test.com", "Staff", "+380", role));

        result.Role.Should().Be(expectedRole);
        (await db.Users.SingleAsync()).GetRoleName().Should().Be(expectedRole);
    }

    // ===== AuthController: відповіді HTTP =====

    [Fact]
    public async Task Auth_Register_ShouldReturnOk_ForNewClient()
    {
        var controller = CreateAuthController();

        var result = await controller.Register(
            new RegisterDto("newuser", "password123", "new@test.com", "New User", null, UserRole.Client));

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Auth_Login_ShouldReturnOk_WhenCredentialsAreValid()
    {
        var controller = CreateAuthController();
        await controller.Register(
            new RegisterDto("okuser", "password123", "ok@test.com", "Ok User", null, UserRole.Client));

        var result = await controller.Login(new LoginDto("okuser", "password123"));

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void Auth_Me_ShouldReturnClaimsOfCurrentUser()
    {
        var controller = CreateAuthController();
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "5"),
            new Claim(ClaimTypes.Name, "bob"),
            new Claim(ClaimTypes.Role, "Client")
        }, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        var result = controller.Me();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value!;
        value.GetType().GetProperty("Id")!.GetValue(value).Should().Be("5");
        value.GetType().GetProperty("Login")!.GetValue(value).Should().Be("bob");
        value.GetType().GetProperty("Role")!.GetValue(value).Should().Be("Client");
    }
}
