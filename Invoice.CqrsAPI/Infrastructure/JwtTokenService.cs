using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Invoice.DTOs;
using Microsoft.IdentityModel.Tokens;

namespace Invoice.CqrsAPI.Infrastructure;

public interface IJwtTokenService
{
    (string Token, DateTime Expiration) CreateToken(UserDto user);
}

/// <summary>Same token format as Invoice.CoreAPI's LoginController, so both APIs accept each other's tokens.</summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime Expiration) CreateToken(UserDto user)
    {
        var key = _configuration["Jwt:Key"];
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        var expiryMinutes = _configuration.GetValue<int>("Jwt:ExpiryMinutes");

        if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("Jwt:Key is not configured.");
        if (string.IsNullOrWhiteSpace(issuer)) throw new InvalidOperationException("Jwt:Issuer is not configured.");
        if (string.IsNullOrWhiteSpace(audience)) throw new InvalidOperationException("Jwt:Audience is not configured.");
        if (expiryMinutes <= 0) throw new InvalidOperationException("Jwt:ExpiryMinutes must be greater than zero.");

        var expiration = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.GivenName, user.FirstName ?? string.Empty),
            new(ClaimTypes.Surname, user.LastName ?? string.Empty),
            new("displayName", user.DisplayName ?? string.Empty)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiration,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.WriteToken(handler.CreateToken(descriptor));

        return (token, expiration);
    }
}
