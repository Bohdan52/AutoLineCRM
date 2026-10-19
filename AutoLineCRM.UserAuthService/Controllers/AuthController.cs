using AutoLineCRM.UserAuthService.Dtos;
using AutoLineCRM.UserAuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoLineCRM.UserAuthService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;
    public AuthController(AuthService auth) => _auth = auth;

    [HttpPost("register")]                                                  
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        try { return Ok(await _auth.RegisterAsync(dto)); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        try { return Ok(await _auth.LoginAsync(dto)); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { error = ex.Message }); }
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        return Ok(new
        {
            Id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            Login = User.Identity?.Name,
            Role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
        });
    }
}