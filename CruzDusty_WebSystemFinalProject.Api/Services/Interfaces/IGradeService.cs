using CruzDusty_WebSystemFinalProject.Api.Models.DTOs;

namespace CruzDusty_WebSystemFinalProject.Api.Services.Interfaces;

public interface IGradeService
{
    Task<IReadOnlyList<GradeResponseDto>> GetAllAsync(int? studentId, string? subjectCode, string? semester, string? schoolYear);

    // ownerStudentId: null for faculty; the caller's own StudentId for students.
    Task<GradeResponseDto> GetByIdAsync(int id, int? ownerStudentId);

    Task<GradeResponseDto> CreateAsync(GradeRequestDto dto, int facultyId);

    Task<GradeResponseDto> UpdateAsync(int id, GradeRequestDto dto);

    Task DeleteAsync(int id);

    Task<MyGradesResponseDto> GetStudentGradesAsync(int studentId);

    GradePreviewResponseDto Preview(GradePreviewRequestDto dto);
}