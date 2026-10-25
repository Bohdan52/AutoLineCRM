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
        var result = await _storage.GetAsync<string>("authToken");

        if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.Value))
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        return CreateAuthenticationState(result.Value);
    }

    public void NotifyUserAuthentication(string token)
    {
        var authState = CreateAuthenticationState(token);
        NotifyAuthenticationStateChanged(Task.FromResult(authState));
    }

    private AuthenticationState CreateAuthenticationState(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        var claims = new List<Claim>();

        foreach (var claim in jwt.Claims)
        {
            // Якщо назва клейму містить слово "role" (незалежно від регістру) - робимо його стандартним Role
            if (claim.Type.Equals("role", StringComparison.OrdinalIgnoreCase) ||
                claim.Type == ClaimTypes.Role ||
                claim.Type.EndsWith("/role"))
            {
                claims.Add(new Claim(ClaimTypes.Role, claim.Value));
            }
            else
            {
                claims.Add(new Claim(claim.Type, claim.Value));
            }
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