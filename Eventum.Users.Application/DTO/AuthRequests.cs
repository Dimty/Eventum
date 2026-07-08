namespace Eventum.Users.Application.DTO;

public sealed record LoginRequest(string Login, string Password);

public sealed record RegisterRequest(string Login, string Password, string? Role);

public sealed record AuthResponse(string Token);
