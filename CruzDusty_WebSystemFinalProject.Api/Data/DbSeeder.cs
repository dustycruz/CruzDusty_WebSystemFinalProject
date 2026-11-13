using CruzDusty_WebSystemFinalProject.Api.Models;
using CruzDusty_WebSystemFinalProject.Api.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CruzDusty_WebSystemFinalProject.Api.Data;

public static class DbSeeder
{
    // Demonstration credentials only. They are created in the Development environment only.
    private const string DemoFacultyPassword = "Faculty@123";
    private const string DemoStudentPassword = "Student@123";

    private static readonly DemoAccount[] DemoAccounts =
    {
        new("faculty1", AppRoles.Faculty, "FAC-001", DemoFacultyPassword),
        new("faculty2", AppRoles.Faculty, "FAC-002", DemoFacultyPassword),
        new("student1", AppRoles.Student, "2026-0001", DemoStudentPassword),
        new("student2", AppRoles.Student, "2026-0002", DemoStudentPassword),
        new("student3", AppRoles.Student, "2026-0003", DemoStudentPassword)
    };

    public static async Task InitializeAsync(IServiceProvider services, bool seedDemoAccounts)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var db = provider.GetRequiredService<AppDbContext>();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        await EnsureAcademicTablesExistAsync(db);

        // Creates or updates the Identity tables and the StudentId / FacultyId mapping.
        await db.Database.MigrateAsync();
        logger.LogInformation("Identity migrations are up to date.");

        if (!seedDemoAccounts)
        {
            logger.LogInformation("Demo accounts are only created in the Development environment.");
            return;
        }

        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in new[] { AppRoles.Faculty, AppRoles.Student })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                EnsureSucceeded(await roleManager.CreateAsync(new IdentityRole(role)), $"create the role '{role}'");
            }
        }

        foreach (var account in DemoAccounts)
        {
            await EnsureAccountAsync(db, userManager, logger, account);
        }

        logger.LogInformation("Database seeding completed.");
    }

    private static async Task EnsureAcademicTablesExistAsync(AppDbContext db)
    {
        try
        {
            _ = await db.Students.AnyAsync();
            _ = await db.FacultyMembers.AnyAsync();
            _ = await db.StudentGrades.AnyAsync();
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException(
                "The academic tables (Students, Faculty, StudentGrades) could not be read. " +
                "Run Database/StudentGradeManagementDB.sql first and check ConnectionStrings:DefaultConnection.",
                ex);
        }
    }

    private static async Task EnsureAccountAsync(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        DemoAccount account)
    {
        int? studentId = null;
        int? facultyId = null;
        string email;

        if (account.Role == AppRoles.Faculty)
        {
            var faculty = await db.FacultyMembers.FirstOrDefaultAsync(f => f.FacultyNumber == account.RecordNumber);
            if (faculty is null)
            {
                logger.LogWarning("Faculty {Number} not found; skipping login {Username}.", account.RecordNumber, account.Username);
                return;
            }

            facultyId = faculty.FacultyId;
            email = faculty.Email;
        }
        else
        {
            var student = await db.Students.FirstOrDefaultAsync(s => s.StudentNumber == account.RecordNumber);
            if (student is null)
            {
                logger.LogWarning("Student {Number} not found; skipping login {Username}.", account.RecordNumber, account.Username);
                return;
            }

            studentId = student.StudentId;
            email = student.Email;
        }

        var user = await userManager.FindByNameAsync(account.Username);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = account.Username,
                Email = email,
                EmailConfirmed = true,
                StudentId = studentId,
                FacultyId = facultyId
            };

            // UserManager hashes the password with PasswordHasher<T>; no plaintext is stored.
            EnsureSucceeded(await userManager.CreateAsync(user, account.Password), $"create the user '{account.Username}'");
        }
        else if (user.StudentId != studentId || user.FacultyId != facultyId)
        {
            user.StudentId = studentId;
            user.FacultyId = facultyId;
            EnsureSucceeded(await userManager.UpdateAsync(user), $"update the mapping for '{account.Username}'");
        }

        if (!await userManager.IsInRoleAsync(user, account.Role))
        {
            EnsureSucceeded(await userManager.AddToRoleAsync(user, account.Role), $"add '{account.Username}' to '{account.Role}'");
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string action)
    {
        if (!result.Succeeded)
        {
            var reasons = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Could not {action}: {reasons}");
        }
    }

    private sealed record DemoAccount(string Username, string Role, string RecordNumber, string Password);
}