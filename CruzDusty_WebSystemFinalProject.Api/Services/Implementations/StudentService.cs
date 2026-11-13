using CruzDusty_WebSystemFinalProject.Api.Data;
using CruzDusty_WebSystemFinalProject.Api.Models.DTOs;
using CruzDusty_WebSystemFinalProject.Api.Models.Entities;
using CruzDusty_WebSystemFinalProject.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CruzDusty_WebSystemFinalProject.Api.Services.Implementations;

// Read-only: the faculty grade form needs the list of students to choose from.
public class StudentService : IStudentService
{
    private readonly AppDbContext _db;

    public StudentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<StudentResponseDto>> GetAllAsync(string? search)
    {
        IQueryable<Student> query = _db.Students.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(s =>
                s.StudentNumber.Contains(term) ||
                s.FirstName.Contains(term) ||
                s.LastName.Contains(term) ||
                (s.FirstName + " " + s.LastName).Contains(term));
        }

        var students = await query
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ToListAsync();

        return students.Select(s => new StudentResponseDto
        {
            StudentId = s.StudentId,
            StudentNumber = s.StudentNumber,
            FirstName = s.FirstName,
            LastName = s.LastName,
            FullName = s.FullName,
            Email = s.Email,
            IsActive = s.IsActive
        }).ToList();
    }
}