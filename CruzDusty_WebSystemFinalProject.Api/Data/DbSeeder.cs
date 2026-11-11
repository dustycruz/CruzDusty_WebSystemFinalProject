using CruzDusty_WebSystemFinalProject.Api.Models;
using CruzDusty_WebSystemFinalProject.Api.Models.Entities;
using CruzDusty_WebSystemFinalProject.Api.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CruzDusty_WebSystemFinalProject.Api.Data;

public static class DbSeeder
{
    private const string FacultyUsername = "faculty1";
    private const string FacultyPassword = "Faculty@123";
    private const string FacultyFullName = "Prof. Teresa Villanueva";

    private const string StudentPassword = "Student@123";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var db = provider.GetRequiredService<AppDbContext>();
        var passwordHasher = provider.GetRequiredService<IPasswordHasher<User>>();
        var calculator = provider.GetRequiredService<IGradeCalculator>();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        if (!await db.Database.CanConnectAsync())
        {
            throw new InvalidOperationException(
                "Cannot connect to the database. Check ConnectionStrings:DefaultConnection in appsettings.json " +
                "and run Database/CruzDusty_WebSystemFinalProject.sql first.");
        }

        await EnsureFacultyAsync(db, passwordHasher);

        var studentOne = await EnsureStudentAccountAsync(
            db,
            passwordHasher,
            username: "student1",
            studentNumber: "2025-0001",
            firstName: "Juan",
            middleName: "Santos",
            lastName: "Dela Cruz",
            email: "juan.delacruz@example.com",
            course: "BS Information Technology",
            yearLevel: 2);

        var studentTwo = await EnsureStudentAccountAsync(
            db,
            passwordHasher,
            username: "student2",
            studentNumber: "2025-0002",
            firstName: "Maria",
            middleName: "Lopez",
            lastName: "Santos",
            email: "maria.santos@example.com",
            course: "BS Information Technology",
            yearLevel: 2);

        await EnsureGradeRecordsAsync(db, calculator, studentOne, studentTwo);

        logger.LogInformation("Database seeding completed.");
    }

    private static async Task EnsureFacultyAsync(AppDbContext db, IPasswordHasher<User> passwordHasher)
    {
        var exists = await db.Users.AnyAsync(u => u.Username == FacultyUsername);
        if (exists)
        {
            return;
        }

        var faculty = new User
        {
            Username = FacultyUsername,
            FullName = FacultyFullName,
            Role = AppRoles.Faculty,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        faculty.PasswordHash = passwordHasher.HashPassword(faculty, FacultyPassword);

        db.Users.Add(faculty);
        await db.SaveChangesAsync();
    }

    private static async Task<Student> EnsureStudentAccountAsync(
        AppDbContext db,
        IPasswordHasher<User> passwordHasher,
        string username,
        string studentNumber,
        string firstName,
        string? middleName,
        string lastName,
        string? email,
        string course,
        int yearLevel)
    {
        var student = await db.Students
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.StudentNumber == studentNumber);

        if (student is null)
        {
            student = new Student
            {
                StudentNumber = studentNumber,
                FirstName = firstName,
                MiddleName = middleName,
                LastName = lastName,
                Email = email,
                Course = course,
                YearLevel = yearLevel,
                CreatedAt = DateTime.UtcNow
            };
            db.Students.Add(student);
        }

        if (student.User is null)
        {
            var user = await db.Users
                .Include(u => u.Student)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user is null)
            {
                user = new User
                {
                    Username = username,
                    FullName = $"{student.FirstName} {student.LastName}",
                    Role = AppRoles.Student,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                user.PasswordHash = passwordHasher.HashPassword(user, StudentPassword);
                db.Users.Add(user);
            }

            if (user.Student is null)
            {
                student.User = user;
                student.UpdatedAt = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync();
        return student;
    }

    private static async Task EnsureGradeRecordsAsync(
        AppDbContext db,
        IGradeCalculator calculator,
        Student studentOne,
        Student studentTwo)
    {
        if (await db.GradeRecords.AnyAsync())
        {
            return;
        }

        var subjects = await db.Subjects.ToListAsync();
        if (subjects.Count == 0)
        {
            return;
        }

        void AddRecord(
            Student student,
            string subjectCode,
            string schoolYear,
            string semester,
            decimal? prelim,
            decimal? midterm,
            decimal? finals)
        {
            var subject = subjects.FirstOrDefault(s => s.SubjectCode == subjectCode);
            if (subject is null)
            {
                return;
            }

            // Remarks and FinalGrade always come from the calculator, never from literals.
            var computed = calculator.Compute(prelim, midterm, finals);

            db.GradeRecords.Add(new GradeRecord
            {
                StudentId = student.StudentId,
                SubjectId = subject.SubjectId,
                SchoolYear = schoolYear,
                Semester = semester,
                Prelim = prelim,
                Midterm = midterm,
                Finals = finals,
                FinalGrade = computed.FinalGrade,
                Remarks = computed.Remarks,
                CreatedAt = DateTime.UtcNow
            });
        }

        // student1 (Juan Dela Cruz)
        AddRecord(studentOne, "IT101", "2025-2026", Semesters.Second, 88m, 90m, 92m);
        AddRecord(studentOne, "GE102", "2025-2026", Semesters.Second, 84m, 86m, 81m);
        AddRecord(studentOne, "IT102", "2026-2027", Semesters.First, 70m, 68m, 72m);
        AddRecord(studentOne, "IT202", "2026-2027", Semesters.First, 91m, 89m, 94m);
        AddRecord(studentOne, "GE101", "2026-2027", Semesters.First, 85m, 80m, null);

        // student2 (Maria Santos)
        AddRecord(studentTwo, "IT101", "2025-2026", Semesters.Second, 78m, 82m, 80m);
        AddRecord(studentTwo, "IT102", "2026-2027", Semesters.First, 95m, 93m, 97m);
        AddRecord(studentTwo, "GE102", "2026-2027", Semesters.First, 60m, 72m, 74m);
        AddRecord(studentTwo, "PE101", "2026-2027", Semesters.First, 90m, null, null);

        await db.SaveChangesAsync();
    }
}