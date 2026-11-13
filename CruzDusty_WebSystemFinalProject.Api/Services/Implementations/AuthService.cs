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

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;

    public AuthService(UserManager<ApplicationUser> userManager, ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        var normalizedUsername = _userManager.NormalizeName(request.Username.Trim());

        var user = await _userManager.Users
            .Include(u => u.Student)
            .Include(u => u.Faculty)
            .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUsername);

        // Same message for "no such user" and "wrong password" so usernames cannot be probed.
        if (user is null)
        {
            throw ApiException.Unauthorized(InvalidCredentialsMessage);
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            throw ApiException.Unauthorized(
                "This account is temporarily locked after too many failed sign-in attempts. Try again in a few minutes.");
        }

        // Identity verifies the hashed password; plaintext passwords are never stored.
        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            await _userManager.AccessFailedAsync(user);
            throw ApiException.Unauthorized(InvalidCredentialsMessage);
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        string role;
        string fullName;

        // Role-specific mapping is enforced: a Faculty login must point to a faculty record,
        // a Student login to a student record, and never to both.
        if (roles.Contains(AppRoles.Faculty))
        {
            if (user.Faculty is null || user.StudentId is not null)
            {
                throw ApiException.Forbidden("This faculty account is not linked to a faculty record.");
            }

            if (!user.Faculty.IsActive)
            {
                throw ApiException.Forbidden("This faculty record is inactive.");
            }

            role = AppRoles.Faculty;
            fullName = user.Faculty.FullName;
        }
        else if (roles.Contains(AppRoles.Student))
        {
            if (user.Student is null || user.FacultyId is not null)
            {
                throw ApiException.Forbidden("This student account is not linked to a student record.");
            }

            if (!user.Student.IsActive)
            {
                throw ApiException.Forbidden("This student record is inactive.");
            }

            role = AppRoles.Student;
            fullName = user.Student.FullName;
        }
        else
        {
            throw ApiException.Forbidden("This account has no role assigned.");
        }

        var token = _tokenService.CreateToken(user, role, fullName);

        return new LoginResponseDto
        {
            Token = token.Token,
            Role = role,
            FullName = fullName,
            StudentId = role == AppRoles.Student ? user.StudentId : null,
            FacultyId = role == AppRoles.Faculty ? user.FacultyId : null,
            ExpiresAt = token.ExpiresAt
        };
    }
}