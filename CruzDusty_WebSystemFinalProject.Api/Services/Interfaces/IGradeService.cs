using CruzDusty_WebSystemFinalProject.Api.Models.DTOs;

namespace CruzDusty_WebSystemFinalProject.Api.Services.Interfaces;

public interface IGradeService
{
    Task<IReadOnlyList<GradeResponseDto>> GetAllAsync(int? studentId, int? subjectId, string? schoolYear, string? semester);

    Task<GradeResponseDto> GetByIdAsync(int id);

    Task<GradeResponseDto> CreateAsync(GradeCreateDto dto);

    Task<GradeResponseDto> UpdateAsync(int id, GradeUpdateDto dto);

    Task DeleteAsync(int id);

    Task<MyGradesResponseDto> GetMyGradesAsync(int studentId);

    GradePreviewResponseDto Preview(GradePreviewRequestDto dto);
}