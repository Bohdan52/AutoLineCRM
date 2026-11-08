using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using D20Tek.Blazor.BrowserStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace AutoLineCRM.BlazorClient;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _storage;

    public CustomAuthStateProvider(ILocalStorageService storage) => _storage = storage;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var anonymous = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        var result = await _storage.GetAsync<string>("authToken");

        if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.Value))
            return anonymous;

        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Value);
            if (jwt.ValidTo < DateTime.UtcNow)
            {
                await _storage.RemoveAsync("authToken");
                return anonymous;
            }
        }
        catch
        {
            await _storage.RemoveAsync("authToken");
            return anonymous;
        }

        return CreateAuthenticationState(result.Value);
    }

    public void NotifyUserAuthentication(string token)
    {
        var authState = CreateAuthenticationState(token);
        NotifyAuthenticationStateChanged(Task.FromResult(authState));
    }
    public void NotifyUserLogout()
    {
        var anonymous = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        NotifyAuthenticationStateChanged(Task.FromResult(anonymous));
    }

    private AuthenticationState CreateAuthenticationState(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        var claims = new List<Claim>();

        foreach (var claim in jwt.Claims)
        {
            var type = claim.Type;

            if (type.Equals("role", StringComparison.OrdinalIgnoreCase) || type == ClaimTypes.Role || type.EndsWith("/role"))
                type = ClaimTypes.Role;
            else if (type == "nameid" || type.EndsWith("/nameidentifier"))
                type = ClaimTypes.NameIdentifier;
            else if (type == "unique_name" || type.EndsWith("/name"))
                type = ClaimTypes.Name;

            claims.Add(new Claim(type, claim.Value));
        }

        var identity = new ClaimsIdentity(claims, "jwt", ClaimTypes.Name, ClaimTypes.Role);
        var user = new ClaimsPrincipal(identity);

        // Виводимо в консоль для перевірки
        Console.WriteLine("--- CLAIMS IN TOKEN ---");
        foreach (var c in user.Claims)
        {
            Console.WriteLine($"Type: {c.Type} | Value: {c.Value}");
        }
        Console.WriteLine($"IsInRole(Administrator): {user.IsInRole("Administrator")}");
        Console.WriteLine("-----------------------");

        return new AuthenticationState(user);
    }
}