using CruzDusty_WebSystemFinalProject.Api.Data;
using CruzDusty_WebSystemFinalProject.Api.Exceptions;
using CruzDusty_WebSystemFinalProject.Api.Models.DTOs;
using CruzDusty_WebSystemFinalProject.Api.Models.Entities;
using CruzDusty_WebSystemFinalProject.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CruzDusty_WebSystemFinalProject.Api.Services.Implementations;

public class SubjectService : ISubjectService
{
    private readonly AppDbContext _db;

    public SubjectService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SubjectResponseDto>> GetAllAsync()
    {
        var subjects = await _db.Subjects
            .AsNoTracking()
            .OrderBy(s => s.SubjectCode)
            .ToListAsync();

        return subjects.Select(ToDto).ToList();
    }

    public async Task<SubjectResponseDto> GetByIdAsync(int id)
    {
        var subject = await _db.Subjects
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SubjectId == id);

        if (subject is null)
        {
            throw ApiException.NotFound($"Subject with id {id} was not found.");
        }

        return ToDto(subject);
    }

    public async Task<SubjectResponseDto> CreateAsync(SubjectCreateDto dto)
    {
        var code = NormalizeCode(dto.SubjectCode);
        var name = dto.SubjectName.Trim();

        if (code.Length == 0 || name.Length == 0)
        {
            throw ApiException.BadRequest("Subject code and subject name cannot be blank.");
        }

        if (await _db.Subjects.AnyAsync(s => s.SubjectCode == code))
        {
            throw ApiException.Conflict($"Subject code '{code}' already exists.");
        }

        var subject = new Subject
        {
            SubjectCode = code,
            SubjectName = name,
            Units = dto.Units,
            Description = NullIfBlank(dto.Description),
            CreatedAt = DateTime.UtcNow
        };

        _db.Subjects.Add(subject);
        await _db.SaveChangesAsync();

        return ToDto(subject);
    }

    public async Task<SubjectResponseDto> UpdateAsync(int id, SubjectUpdateDto dto)
    {
        var subject = await _db.Subjects.FirstOrDefaultAsync(s => s.SubjectId == id);

        if (subject is null)
        {
            throw ApiException.NotFound($"Subject with id {id} was not found.");
        }

        var code = NormalizeCode(dto.SubjectCode);
        var name = dto.SubjectName.Trim();

        if (code.Length == 0 || name.Length == 0)
        {
            throw ApiException.BadRequest("Subject code and subject name cannot be blank.");
        }

        var codeTaken = await _db.Subjects
            .AnyAsync(s => s.SubjectCode == code && s.SubjectId != id);

        if (codeTaken)
        {
            throw ApiException.Conflict($"Subject code '{code}' already exists.");
        }

        subject.SubjectCode = code;
        subject.SubjectName = name;
        subject.Units = dto.Units;
        subject.Description = NullIfBlank(dto.Description);
        subject.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return ToDto(subject);
    }

    public async Task DeleteAsync(int id)
    {
        var subject = await _db.Subjects.FirstOrDefaultAsync(s => s.SubjectId == id);

        if (subject is null)
        {
            throw ApiException.NotFound($"Subject with id {id} was not found.");
        }

        var gradeCount = await _db.GradeRecords.CountAsync(g => g.SubjectId == id);
        if (gradeCount > 0)
        {
            throw ApiException.Conflict(
                $"Cannot delete '{subject.SubjectCode}' because {gradeCount} grade record(s) use it. " +
                "Delete those grade records first.");
        }

        _db.Subjects.Remove(subject);
        await _db.SaveChangesAsync();
    }

    private static string NormalizeCode(string code)
    {
        return code.Trim().ToUpperInvariant();
    }

    private static string? NullIfBlank(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static SubjectResponseDto ToDto(Subject subject)
    {
        return new SubjectResponseDto
        {
            SubjectId = subject.SubjectId,
            SubjectCode = subject.SubjectCode,
            SubjectName = subject.SubjectName,
            Units = subject.Units,
            Description = subject.Description,
            CreatedAt = subject.CreatedAt,
            UpdatedAt = subject.UpdatedAt
        };
    }
}