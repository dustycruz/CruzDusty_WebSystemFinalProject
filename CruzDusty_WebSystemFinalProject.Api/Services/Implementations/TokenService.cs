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

    public TokenResult CreateToken(ApplicationUser user, string role, string fullName)
    {
        var claims = new List<Claim>
        {
            new Claim(AppClaimTypes.UserId, user.Id),
            new Claim(AppClaimTypes.Username, user.UserName ?? string.Empty),
            new Claim(AppClaimTypes.FullName, fullName),
            new Claim(AppClaimTypes.Role, role),
            new Claim("jti", Guid.NewGuid().ToString())
        };

        // These claims are written by the server from the Identity account mapping and are
        // signed inside the token. The API trusts them, never a StudentId sent by the browser.
        if (role == AppRoles.Student && user.StudentId.HasValue)
        {
            claims.Add(new Claim(AppClaimTypes.StudentId, user.StudentId.Value.ToString()));
        }

        if (role == AppRoles.Faculty && user.FacultyId.HasValue)
        {
            claims.Add(new Claim(AppClaimTypes.FacultyId, user.FacultyId.Value.ToString()));
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

        return new TokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}