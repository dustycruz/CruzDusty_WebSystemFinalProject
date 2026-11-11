using CruzDusty_WebSystemFinalProject.Api.Models.DTOs;

namespace CruzDusty_WebSystemFinalProject.Api.Services.Interfaces;

public interface ISubjectService
{
    Task<IReadOnlyList<SubjectResponseDto>> GetAllAsync();

    Task<SubjectResponseDto> GetByIdAsync(int id);

    Task<SubjectResponseDto> CreateAsync(SubjectCreateDto dto);

    Task<SubjectResponseDto> UpdateAsync(int id, SubjectUpdateDto dto);

    Task DeleteAsync(int id);
}