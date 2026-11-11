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
        "A grade record already exists for this student, subject, school year and semester.";

    private readonly AppDbContext _db;
    private readonly IGradeCalculator _calculator;

    public GradeService(AppDbContext db, IGradeCalculator calculator)
    {
        _db = db;
        _calculator = calculator;
    }

    public async Task<IReadOnlyList<GradeResponseDto>> GetAllAsync(
        int? studentId,
        int? subjectId,
        string? schoolYear,
        string? semester)
    {
        var query = BaseQuery();

        if (studentId.HasValue)
        {
            var id = studentId.Value;
            query = query.Where(g => g.StudentId == id);
        }

        if (subjectId.HasValue)
        {
            var id = subjectId.Value;
            query = query.Where(g => g.SubjectId == id);
        }

        if (!string.IsNullOrWhiteSpace(schoolYear))
        {
            var year = schoolYear.Trim();
            query = query.Where(g => g.SchoolYear == year);
        }

        if (!string.IsNullOrWhiteSpace(semester))
        {
            var term = semester.Trim();
            query = query.Where(g => g.Semester == term);
        }

        var records = await query
            .OrderByDescending(g => g.SchoolYear)
            .ThenBy(g => g.Semester)
            .ThenBy(g => g.Student.LastName)
            .ThenBy(g => g.Student.FirstName)
            .ThenBy(g => g.Subject.SubjectCode)
            .ToListAsync();

        return records.Select(ToDto).ToList();
    }

    public async Task<GradeResponseDto> GetByIdAsync(int id)
    {
        var record = await BaseQuery().FirstOrDefaultAsync(g => g.GradeRecordId == id);

        if (record is null)
        {
            throw ApiException.NotFound($"Grade record with id {id} was not found.");
        }

        return ToDto(record);
    }

    public async Task<GradeResponseDto> CreateAsync(GradeCreateDto dto)
    {
        var schoolYear = dto.SchoolYear.Trim();
        var semester = dto.Semester.Trim();

        ValidateTerm(schoolYear, semester);

        // SERVER-SIDE COMPUTATION. Throws 400 if any score is outside 0-100.
        var computed = _calculator.Compute(dto.Prelim, dto.Midterm, dto.Finals);

        await EnsureStudentAndSubjectExistAsync(dto.StudentId, dto.SubjectId);

        var duplicate = await _db.GradeRecords.AnyAsync(g =>
            g.StudentId == dto.StudentId &&
            g.SubjectId == dto.SubjectId &&
            g.SchoolYear == schoolYear &&
            g.Semester == semester);

        if (duplicate)
        {
            throw ApiException.Conflict(DuplicateMessage);
        }

        var record = new GradeRecord
        {
            StudentId = dto.StudentId,
            SubjectId = dto.SubjectId,
            SchoolYear = schoolYear,
            Semester = semester,
            Prelim = dto.Prelim,
            Midterm = dto.Midterm,
            Finals = dto.Finals,
            FinalGrade = computed.FinalGrade,
            Remarks = computed.Remarks,
            CreatedAt = DateTime.UtcNow
        };

        _db.GradeRecords.Add(record);

        // If two requests race past the check above, the unique constraint
        // UQ_GradeRecords_Student_Subject_Term rejects the second one and
        // ExceptionHandlingMiddleware turns that into 409 as well.
        await _db.SaveChangesAsync();

        return await GetByIdAsync(record.GradeRecordId);
    }

    public async Task<GradeResponseDto> UpdateAsync(int id, GradeUpdateDto dto)
    {
        var record = await _db.GradeRecords.FirstOrDefaultAsync(g => g.GradeRecordId == id);

        if (record is null)
        {
            throw ApiException.NotFound($"Grade record with id {id} was not found.");
        }

        var schoolYear = dto.SchoolYear.Trim();
        var semester = dto.Semester.Trim();

        ValidateTerm(schoolYear, semester);

        // SERVER-SIDE RECOMPUTATION on every update.
        var computed = _calculator.Compute(dto.Prelim, dto.Midterm, dto.Finals);

        await EnsureStudentAndSubjectExistAsync(dto.StudentId, dto.SubjectId);

        var duplicate = await _db.GradeRecords.AnyAsync(g =>
            g.GradeRecordId != id &&
            g.StudentId == dto.StudentId &&
            g.SubjectId == dto.SubjectId &&
            g.SchoolYear == schoolYear &&
            g.Semester == semester);

        if (duplicate)
        {
            throw ApiException.Conflict(DuplicateMessage);
        }

        record.StudentId = dto.StudentId;
        record.SubjectId = dto.SubjectId;
        record.SchoolYear = schoolYear;
        record.Semester = semester;
        record.Prelim = dto.Prelim;
        record.Midterm = dto.Midterm;
        record.Finals = dto.Finals;
        record.FinalGrade = computed.FinalGrade;
        record.Remarks = computed.Remarks;
        record.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(int id)
    {
        var record = await _db.GradeRecords.FirstOrDefaultAsync(g => g.GradeRecordId == id);

        if (record is null)
        {
            throw ApiException.NotFound($"Grade record with id {id} was not found.");
        }

        _db.GradeRecords.Remove(record);
        await _db.SaveChangesAsync();
    }

    public async Task<MyGradesResponseDto> GetMyGradesAsync(int studentId)
    {
        var student = await _db.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.StudentId == studentId);

        if (student is null)
        {
            throw ApiException.NotFound("Your student record could not be found.");
        }

        var records = await BaseQuery()
            .Where(g => g.StudentId == studentId)
            .OrderByDescending(g => g.SchoolYear)
            .ThenBy(g => g.Semester)
            .ThenBy(g => g.Subject.SubjectCode)
            .ToListAsync();

        return new MyGradesResponseDto
        {
            Student = new StudentProfileDto
            {
                StudentId = student.StudentId,
                StudentNumber = student.StudentNumber,
                FullName = student.FullName,
                Course = student.Course,
                YearLevel = student.YearLevel
            },
            Grades = records.Select(ToDto).ToList()
        };
    }

    public GradePreviewResponseDto Preview(GradePreviewRequestDto dto)
    {
        var computed = _calculator.Compute(dto.Prelim, dto.Midterm, dto.Finals);

        return new GradePreviewResponseDto
        {
            FinalGrade = computed.FinalGrade,
            Remarks = computed.Remarks
        };
    }

    private IQueryable<GradeRecord> BaseQuery()
    {
        return _db.GradeRecords
            .AsNoTracking()
            .Include(g => g.Student)
            .Include(g => g.Subject);
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

    private async Task EnsureStudentAndSubjectExistAsync(int studentId, int subjectId)
    {
        if (!await _db.Students.AnyAsync(s => s.StudentId == studentId))
        {
            throw ApiException.BadRequest("The selected student does not exist.");
        }

        if (!await _db.Subjects.AnyAsync(s => s.SubjectId == subjectId))
        {
            throw ApiException.BadRequest("The selected subject does not exist.");
        }
    }

    private static GradeResponseDto ToDto(GradeRecord record)
    {
        return new GradeResponseDto
        {
            GradeRecordId = record.GradeRecordId,
            StudentId = record.StudentId,
            StudentNumber = record.Student.StudentNumber,
            StudentName = record.Student.ListName,
            SubjectId = record.SubjectId,
            SubjectCode = record.Subject.SubjectCode,
            SubjectName = record.Subject.SubjectName,
            Units = record.Subject.Units,
            SchoolYear = record.SchoolYear,
            Semester = record.Semester,
            Prelim = record.Prelim,
            Midterm = record.Midterm,
            Finals = record.Finals,
            FinalGrade = record.FinalGrade,
            Remarks = record.Remarks,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt
        };
    }
}