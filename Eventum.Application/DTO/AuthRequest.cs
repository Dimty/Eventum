namespace Eventum.Application.DTO;

public record LoginRequest(string Login, string Password);
public record RegisterRequest(string Login, string Password, string? Role);
public record AuthResponse(string Token);
