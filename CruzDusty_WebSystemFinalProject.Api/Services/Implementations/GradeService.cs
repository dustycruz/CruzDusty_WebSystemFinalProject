using System.Text.RegularExpressions;
using CruzDusty_WebSystemFinalProject.Api.Data;
using CruzDusty_WebSystemFinalProject.Api.Exceptions;
using CruzDusty_WebSystemFinalProject.Api.Models;
using CruzDusty_WebSystemFinalProject.Api.Models.DTOs;
using CruzDusty_WebSystemFinalProject.Api.Models.Entities;
using CruzDusty_WebSystemFinalProject.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CruzDusty_WebSystemFinalProject.Api.Services.Implementations;

public class GradeService : IGradeService
{
    private const string DuplicateMessage =
        "A grade already exists for this student, subject, semester and school year.";

    private readonly AppDbContext _db;
    private readonly IGradeCalculator _calculator;

    public GradeService(AppDbContext db, IGradeCalculator calculator)
    {
        _db = db;
        _calculator = calculator;
    }

    public async Task<IReadOnlyList<GradeResponseDto>> GetAllAsync(
        int? studentId,
        string? subjectCode,
        string? semester,
        string? schoolYear)
    {
        var query = BaseQuery();

        if (studentId.HasValue)
        {
            var id = studentId.Value;
            query = query.Where(g => g.StudentId == id);
        }

        if (!string.IsNullOrWhiteSpace(subjectCode))
        {
            var code = NormalizeCode(subjectCode);
            query = query.Where(g => g.SubjectCode == code);
        }

        if (!string.IsNullOrWhiteSpace(semester))
        {
            var term = semester.Trim();
            query = query.Where(g => g.Semester == term);
        }

        if (!string.IsNullOrWhiteSpace(schoolYear))
        {
            var year = schoolYear.Trim();
            query = query.Where(g => g.SchoolYear == year);
        }

        var records = await query
            .OrderByDescending(g => g.SchoolYear)
            .ThenBy(g => g.Semester)
            .ThenBy(g => g.Student.StudentNumber)
            .ThenBy(g => g.SubjectCode)
            .ToListAsync();

        return records.Select(ToDto).ToList();
    }

    public async Task<GradeResponseDto> GetByIdAsync(int id, int? ownerStudentId)
    {
        var record = await BaseQuery().FirstOrDefaultAsync(g => g.GradeId == id);

        // Ownership check. A student asking for someone else's record gets exactly the same
        // 404 as for a record that does not exist, so other students' records stay hidden.
        if (record is null || (ownerStudentId.HasValue && record.StudentId != ownerStudentId.Value))
        {
            throw ApiException.NotFound($"Grade record {id} was not found.");
        }

        return ToDto(record);
    }

    public async Task<GradeResponseDto> CreateAsync(GradeRequestDto dto, int facultyId)
    {
        var input = Validate(dto);

        await EnsureFacultyCanPostAsync(facultyId);
        await EnsureStudentAsync(input.StudentId, requireActive: true);
        await EnsureNoDuplicateAsync(input, excludeGradeId: null);

        var record = new StudentGrade
        {
            StudentId = input.StudentId,
            SubjectCode = input.SubjectCode,
            SubjectName = input.SubjectName,
            FacultyId = facultyId,
            Grade = input.Grade,
            Remarks = input.Remarks,
            Semester = input.Semester,
            SchoolYear = input.SchoolYear,
            DatePosted = DateTime.UtcNow
        };

        _db.StudentGrades.Add(record);

        // If two requests race past the duplicate check, UQ_StudentGrades_StudentSubjectTerm
        // rejects the second one and ExceptionHandlingMiddleware returns 409.
        await _db.SaveChangesAsync();

        return await GetByIdAsync(record.GradeId, null);
    }

    public async Task<GradeResponseDto> UpdateAsync(int id, GradeRequestDto dto)
    {
        var record = await _db.StudentGrades.FirstOrDefaultAsync(g => g.GradeId == id)
            ?? throw ApiException.NotFound($"Grade record {id} was not found.");

        var input = Validate(dto);

        await EnsureStudentAsync(input.StudentId, requireActive: input.StudentId != record.StudentId);
        await EnsureNoDuplicateAsync(input, excludeGradeId: id);

        record.StudentId = input.StudentId;
        record.SubjectCode = input.SubjectCode;
        record.SubjectName = input.SubjectName;
        record.Grade = input.Grade;
        record.Remarks = input.Remarks; // always recomputed from the NEW grade
        record.Semester = input.Semester;
        record.SchoolYear = input.SchoolYear;
        record.DateModified = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return await GetByIdAsync(id, null);
    }

    public async Task DeleteAsync(int id)
    {
        var record = await _db.StudentGrades.FirstOrDefaultAsync(g => g.GradeId == id)
            ?? throw ApiException.NotFound($"Grade record {id} was not found.");

        _db.StudentGrades.Remove(record);
        await _db.SaveChangesAsync();
    }

    public async Task<MyGradesResponseDto> GetStudentGradesAsync(int studentId)
    {
        var student = await _db.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.StudentId == studentId)
            ?? throw ApiException.NotFound("Your student record could not be found.");

        var records = await BaseQuery()
            .Where(g => g.StudentId == studentId)
            .OrderByDescending(g => g.SchoolYear)
            .ThenBy(g => g.Semester)
            .ThenBy(g => g.SubjectCode)
            .ToListAsync();

        return new MyGradesResponseDto
        {
            Student = new StudentProfileDto
            {
                StudentId = student.StudentId,
                StudentNumber = student.StudentNumber,
                FullName = student.FullName,
                Email = student.Email
            },
            Grades = records.Select(ToDto).ToList()
        };
    }

    public GradePreviewResponseDto Preview(GradePreviewRequestDto dto)
    {
        if (dto.Grade is not int grade)
        {
            throw ApiException.BadRequest("Grade is required.");
        }

        return new GradePreviewResponseDto
        {
            Grade = grade,
            Remarks = _calculator.ComputeRemarks(grade)
        };
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------
    private ValidatedGrade Validate(GradeRequestDto dto)
    {
        if (dto.StudentId is not int studentId || studentId < 1)
        {
            throw ApiException.BadRequest("Student is required.");
        }

        if (dto.Grade is not int grade)
        {
            throw ApiException.BadRequest("Grade is required.");
        }

        var subjectCode = NormalizeCode(dto.SubjectCode ?? string.Empty);
        var subjectName = (dto.SubjectName ?? string.Empty).Trim();

        if (subjectCode.Length == 0 || subjectName.Length == 0)
        {
            throw ApiException.BadRequest("Subject code and subject name are required.");
        }

        var semester = (dto.Semester ?? string.Empty).Trim();
        var schoolYear = (dto.SchoolYear ?? string.Empty).Trim();
        ValidateTerm(schoolYear, semester);

        // SERVER-SIDE REMARKS: computed from the validated grade. The request has no
        // Remarks property, so a client-supplied remark can never reach the database.
        var remarks = _calculator.ComputeRemarks(grade);

        return new ValidatedGrade(studentId, subjectCode, subjectName, grade, remarks, semester, schoolYear);
    }

    private static void ValidateTerm(string schoolYear, string semester)
    {
        var match = Regex.Match(schoolYear, @"^(\d{4})-(\d{4})$");
        if (!match.Success)
        {
            throw ApiException.BadRequest("School year must look like 2026-2027.");
        }

        var startYear = int.Parse(match.Groups[1].Value);
        var endYear = int.Parse(match.Groups[2].Value);

        if (endYear != startYear + 1)
        {
            throw ApiException.BadRequest("School year must be two consecutive years, for example 2026-2027.");
        }

        if (!Semesters.IsValid(semester))
        {
            throw ApiException.BadRequest("Semester must be '1st Semester', '2nd Semester' or 'Summer'.");
        }
    }

    private async Task EnsureFacultyCanPostAsync(int facultyId)
    {
        var faculty = await _db.FacultyMembers.AsNoTracking().FirstOrDefaultAsync(f => f.FacultyId == facultyId);

        if (faculty is null || !faculty.IsActive)
        {
            throw ApiException.Forbidden("Your faculty record is missing or inactive, so you cannot post grades.");
        }
    }

    private async Task EnsureStudentAsync(int studentId, bool requireActive)
    {
        var student = await _db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.StudentId == studentId);

        if (student is null)
        {
            throw ApiException.BadRequest("The selected student does not exist.");
        }

        if (requireActive && !student.IsActive)
        {
            throw ApiException.BadRequest("The selected student is inactive.");
        }
    }

    private async Task EnsureNoDuplicateAsync(ValidatedGrade input, int? excludeGradeId)
    {
        var duplicate = await _db.StudentGrades.AnyAsync(g =>
            (excludeGradeId == null || g.GradeId != excludeGradeId) &&
            g.StudentId == input.StudentId &&
            g.SubjectCode == input.SubjectCode &&
            g.Semester == input.Semester &&
            g.SchoolYear == input.SchoolYear);

        if (duplicate)
        {
            throw ApiException.Conflict(DuplicateMessage);
        }
    }

    private IQueryable<StudentGrade> BaseQuery()
    {
        return _db.StudentGrades
            .AsNoTracking()
            .Include(g => g.Student)
            .Include(g => g.Faculty);
    }

    private static string NormalizeCode(string code)
    {
        return code.Trim().ToUpperInvariant();
    }

    private static GradeResponseDto ToDto(StudentGrade record)
    {
        return new GradeResponseDto
        {
            GradeId = record.GradeId,
            StudentId = record.StudentId,
            StudentNumber = record.Student.StudentNumber,
            StudentName = record.Student.FullName,
            SubjectCode = record.SubjectCode,
            SubjectName = record.SubjectName,
            FacultyId = record.FacultyId,
            FacultyName = record.Faculty.FullName,
            Grade = record.Grade,
            Remarks = record.Remarks,
            Semester = record.Semester,
            SchoolYear = record.SchoolYear,
            DatePosted = record.DatePosted,
            DateModified = record.DateModified
        };
    }

    private sealed record ValidatedGrade(
        int StudentId,
        string SubjectCode,
        string SubjectName,
        int Grade,
        string Remarks,
        string Semester,
        string SchoolYear);
}