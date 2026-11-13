using CruzDusty_WebSystemFinalProject.Api.Models.DTOs;

namespace CruzDusty_WebSystemFinalProject.Api.Services.Interfaces;

public interface IStudentService
{
    Task<IReadOnlyList<StudentResponseDto>> GetAllAsync(string? search);
}