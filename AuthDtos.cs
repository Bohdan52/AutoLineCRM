using AutoLineCRM.SharedKernel.Enums;

namespace AutoLineCRM.UserAuthService.Dtos;

public record RegisterDto(string Login, string Password, string Email, string FullName, string? Phone, UserRole Role);
public record LoginDto(string Login, string Password);
public record AuthResponseDto(string Token, string Login, string Role);