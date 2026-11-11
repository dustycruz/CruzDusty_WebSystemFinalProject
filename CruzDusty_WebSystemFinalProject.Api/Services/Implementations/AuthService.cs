using CruzDusty_WebSystemFinalProject.Api.Data;
using CruzDusty_WebSystemFinalProject.Api.Exceptions;
using CruzDusty_WebSystemFinalProject.Api.Models;
using CruzDusty_WebSystemFinalProject.Api.Models.DTOs;
using CruzDusty_WebSystemFinalProject.Api.Models.Entities;
using CruzDusty_WebSystemFinalProject.Api.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CruzDusty_WebSystemFinalProject.Api.Services.Implementations;

public class AuthService : IAuthService
{
    private const string InvalidCredentialsMessage = "Invalid username or password.";

    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthService(AppDbContext db, IPasswordHasher<User> passwordHasher, ITokenService tokenService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        var username = request.Username.Trim();

        var user = await _db.Users
            .Include(u => u.Student)
            .FirstOrDefaultAsync(u => u.Username == username);

        // Same message for "no such user" and "wrong password" so usernames cannot be probed.
        if (user is null || !user.IsActive)
        {
            throw ApiException.Unauthorized(InvalidCredentialsMessage);
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            throw ApiException.Unauthorized(InvalidCredentialsMessage);
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        if (user.Role == AppRoles.Student && user.Student is null)
        {
            throw ApiException.Forbidden(
                "This account is not linked to a student record. Please contact your faculty adviser.");
        }

        var token = _tokenService.CreateToken(user);

        return new LoginResponseDto
        {
            Token = token.Token,
            Role = user.Role,
            FullName = user.FullName,
            StudentId = user.Student?.StudentId,
            ExpiresAt = token.ExpiresAt
        };
    }
}