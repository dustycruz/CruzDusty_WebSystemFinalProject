using CruzDusty_WebSystemFinalProject.Api.Data;
using CruzDusty_WebSystemFinalProject.Api.Exceptions;
using CruzDusty_WebSystemFinalProject.Api.Models;
using CruzDusty_WebSystemFinalProject.Api.Models.DTOs;
using CruzDusty_WebSystemFinalProject.Api.Models.Entities;
using CruzDusty_WebSystemFinalProject.Api.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CruzDusty_WebSystemFinalProject.Api.Services.Implementations;

public class StudentService : IStudentService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;

    public StudentService(AppDbContext db, IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<IReadOnlyList<StudentResponseDto>> GetAllAsync(string? search)
    {
        IQueryable<Student> query = _db.Students
            .AsNoTracking()
            .Include(s => s.User);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(s =>
                s.StudentNumber.Contains(term) ||
                s.FirstName.Contains(term) ||
                s.LastName.Contains(term) ||
                (s.MiddleName != null && s.MiddleName.Contains(term)) ||
                (s.FirstName + " " + s.LastName).Contains(term) ||
                (s.LastName + ", " + s.FirstName).Contains(term));
        }

        var students = await query
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ThenBy(s => s.StudentNumber)
            .ToListAsync();

        return students.Select(ToDto).ToList();
    }

    public async Task<StudentResponseDto> GetByIdAsync(int id)
    {
        var student = await _db.Students
            .AsNoTracking()
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.StudentId == id);

        if (student is null)
        {
            throw ApiException.NotFound($"Student with id {id} was not found.");
        }

        return ToDto(student);
    }

    public async Task<StudentResponseDto> CreateAsync(StudentCreateDto dto)
    {
        var studentNumber = dto.StudentNumber.Trim();
        var username = dto.Username.Trim();
        var firstName = dto.FirstName.Trim();
        var lastName = dto.LastName.Trim();
        var course = dto.Course.Trim();

        if (firstName.Length == 0 || lastName.Length == 0 || course.Length == 0 || studentNumber.Length == 0)
        {
            throw ApiException.BadRequest("Student number, first name, last name and course cannot be blank.");
        }

        if (await _db.Students.AnyAsync(s => s.StudentNumber == studentNumber))
        {
            throw ApiException.Conflict($"Student number '{studentNumber}' is already in use.");
        }

        if (await _db.Users.AnyAsync(u => u.Username == username))
        {
            throw ApiException.Conflict($"Username '{username}' is already taken.");
        }

        var now = DateTime.UtcNow;

        var user = new User
        {
            Username = username,
            FullName = $"{firstName} {lastName}",
            Role = AppRoles.Student,
            IsActive = true,
            CreatedAt = now
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        var student = new Student
        {
            StudentNumber = studentNumber,
            FirstName = firstName,
            MiddleName = NullIfBlank(dto.MiddleName),
            LastName = lastName,
            Email = NullIfBlank(dto.Email),
            Course = course,
            YearLevel = dto.YearLevel,
            User = user,
            CreatedAt = now
        };

        // One SaveChanges = one transaction: the Student row and the User row
        // are both written or neither is.
        _db.Students.Add(student);
        await _db.SaveChangesAsync();

        return ToDto(student);
    }

    public async Task<StudentResponseDto> UpdateAsync(int id, StudentUpdateDto dto)
    {
        var student = await _db.Students
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.StudentId == id);

        if (student is null)
        {
            throw ApiException.NotFound($"Student with id {id} was not found.");
        }

        var studentNumber = dto.StudentNumber.Trim();
        var firstName = dto.FirstName.Trim();
        var lastName = dto.LastName.Trim();
        var course = dto.Course.Trim();

        if (firstName.Length == 0 || lastName.Length == 0 || course.Length == 0 || studentNumber.Length == 0)
        {
            throw ApiException.BadRequest("Student number, first name, last name and course cannot be blank.");
        }

        var numberTaken = await _db.Students
            .AnyAsync(s => s.StudentNumber == studentNumber && s.StudentId != id);

        if (numberTaken)
        {
            throw ApiException.Conflict($"Student number '{studentNumber}' is already in use.");
        }

        var now = DateTime.UtcNow;

        student.StudentNumber = studentNumber;
        student.FirstName = firstName;
        student.MiddleName = NullIfBlank(dto.MiddleName);
        student.LastName = lastName;
        student.Email = NullIfBlank(dto.Email);
        student.Course = course;
        student.YearLevel = dto.YearLevel;
        student.UpdatedAt = now;

        var newPassword = string.IsNullOrWhiteSpace(dto.NewPassword) ? null : dto.NewPassword;

        if (student.User is not null)
        {
            student.User.FullName = $"{firstName} {lastName}";
            student.User.UpdatedAt = now;

            if (newPassword is not null)
            {
                student.User.PasswordHash = _passwordHasher.HashPassword(student.User, newPassword);
            }
        }
        else if (newPassword is not null)
        {
            // The student has no login yet: create one, using the student number as the username.
            var username = studentNumber;

            if (await _db.Users.AnyAsync(u => u.Username == username))
            {
                throw ApiException.Conflict(
                    $"Cannot create a login: the username '{username}' is already taken.");
            }

            var user = new User
            {
                Username = username,
                FullName = $"{firstName} {lastName}",
                Role = AppRoles.Student,
                IsActive = true,
                CreatedAt = now
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);

            student.User = user;
        }

        await _db.SaveChangesAsync();

        return ToDto(student);
    }

    public async Task DeleteAsync(int id)
    {
        var student = await _db.Students
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.StudentId == id);

        if (student is null)
        {
            throw ApiException.NotFound($"Student with id {id} was not found.");
        }

        var user = student.User;

        // Grade records are removed by ON DELETE CASCADE (FK_GradeRecords_Students).
        _db.Students.Remove(student);

        if (user is not null)
        {
            _db.Users.Remove(user);
        }

        await _db.SaveChangesAsync();
    }

    private static string? NullIfBlank(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static StudentResponseDto ToDto(Student student)
    {
        return new StudentResponseDto
        {
            StudentId = student.StudentId,
            StudentNumber = student.StudentNumber,
            FirstName = student.FirstName,
            MiddleName = student.MiddleName,
            LastName = student.LastName,
            FullName = student.ListName,
            Email = student.Email,
            Course = student.Course,
            YearLevel = student.YearLevel,
            Username = student.User?.Username,
            HasAccount = student.User is not null,
            CreatedAt = student.CreatedAt,
            UpdatedAt = student.UpdatedAt
        };
    }
}