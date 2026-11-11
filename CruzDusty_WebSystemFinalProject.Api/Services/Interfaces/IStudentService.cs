using CruzDusty_WebSystemFinalProject.Api.Models.DTOs;

namespace CruzDusty_WebSystemFinalProject.Api.Services.Interfaces;

public interface IStudentService
{
    Task<IReadOnlyList<StudentResponseDto>> GetAllAsync(string? search);

    Task<StudentResponseDto> GetByIdAsync(int id);

    Task<StudentResponseDto> CreateAsync(StudentCreateDto dto);

    Task<StudentResponseDto> UpdateAsync(int id, StudentUpdateDto dto);

    Task DeleteAsync(int id);
}