using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CruzDusty_WebSystemFinalProject.Api.Models;
using CruzDusty_WebSystemFinalProject.Api.Models.Entities;
using CruzDusty_WebSystemFinalProject.Api.Services.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CruzDusty_WebSystemFinalProject.Api.Services.Implementations;

public class TokenService : ITokenService
{
    private readonly JwtSettings _settings;

    public TokenService(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
    }

    public TokenResult CreateToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(AppClaimTypes.UserId, user.UserId.ToString()),
            new Claim(AppClaimTypes.Username, user.Username),
            new Claim(AppClaimTypes.FullName, user.FullName),
            new Claim(AppClaimTypes.Role, user.Role),
            new Claim("jti", Guid.NewGuid().ToString())
        };

        // The StudentId claim is what GET /api/grades/me trusts.
        // It is written by the server at login and signed inside the token.
        if (user.Role == AppRoles.Student && user.Student is not null)
        {
            claims.Add(new Claim(AppClaimTypes.StudentId, user.Student.StudentId.ToString()));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(_settings.ExpiresMinutes);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new TokenResult(tokenString, expiresAt);
    }
}