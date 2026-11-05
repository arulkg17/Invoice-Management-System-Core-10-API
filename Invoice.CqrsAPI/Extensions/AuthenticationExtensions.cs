using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Invoice.CqrsAPI.Extensions;

public static class AuthenticationExtensions
{
    /// <summary>Same JWT settings as Invoice.CoreAPI, so a token from either API works on both.</summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var key = configuration["Jwt:Key"];
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];

        if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("Jwt:Key is not configured.");
        if (string.IsNullOrWhiteSpace(issuer)) throw new InvalidOperationException("Jwt:Issuer is not configured.");
        if (string.IsNullOrWhiteSpace(audience)) throw new InvalidOperationException("Jwt:Audience is not configured.");

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();

        return services;
    }
}
