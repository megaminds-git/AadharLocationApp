using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AadharLocation.Api.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace AadharLocation.Api.Services;

public class JwtService(IConfiguration configuration)
{
    private readonly string _secret = configuration["Jwt:Secret"]
        ?? throw new InvalidOperationException("Jwt:Secret is not configured");
    private readonly string _issuer = configuration["Jwt:Issuer"] ?? "AadharLocation";
    private readonly string _audience = configuration["Jwt:Audience"] ?? "AadharLocationClients";
    private readonly int _expiryHours = int.TryParse(configuration["Jwt:ExpiryHours"], out var h) ? h : 8;

    public string GenerateAdminToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role),
        };
        return CreateToken(claims, TimeSpan.FromHours(_expiryHours));
    }

    public string GenerateTrackerToken(Operator op, string deviceKey)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, op.Id.ToString()),
            new Claim(ClaimTypes.Email, op.Email),
            new Claim(ClaimTypes.Name, op.Name),
            new Claim(ClaimTypes.Role, "Tracker"),
            new Claim("device_key", deviceKey),
        };
        return CreateToken(claims, TimeSpan.FromDays(365));
    }

    private string CreateToken(IEnumerable<Claim> claims, TimeSpan expiry)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(expiry),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
