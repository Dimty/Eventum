using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Eventum.Application.Common;
using Eventum.Domain.Models;
using Eventum.Infrastructure.OptionsPattern;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Eventum.Infrastructure.Services;

// Infrastructure/Services/JwtTokenGenerator.cs
public class JwtTokenGenerator : ITokenGenerator
{
    private readonly JwtSettings _settings;
    
    public JwtTokenGenerator(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }
    
    public string GenerateToken(User user)
    {
        // Техническая логика создания JWT
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.ExpirationMinutes),
            signingCredentials: credentials
        );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}