using CruzDusty_WebSystemFinalProject.Api.Models.Entities;

namespace CruzDusty_WebSystemFinalProject.Api.Services.Interfaces;

public sealed record TokenResult(string Token, DateTime ExpiresAt);

public interface ITokenService
{
    TokenResult CreateToken(User user);
}