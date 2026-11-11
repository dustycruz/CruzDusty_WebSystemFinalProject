using CruzDusty_WebSystemFinalProject.Api.Models.DTOs;

namespace CruzDusty_WebSystemFinalProject.Api.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
}