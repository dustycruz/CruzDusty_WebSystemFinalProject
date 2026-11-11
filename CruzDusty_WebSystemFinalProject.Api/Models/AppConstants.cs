namespace CruzDusty_WebSystemFinalProject.Api.Models;

public static class AppRoles
{
    public const string Faculty = "Faculty";
    public const string Student = "Student";
}

public static class GradeRemarks
{
    public const string Passed = "Passed";
    public const string Failed = "Failed";
    public const string Incomplete = "Incomplete";
}

public static class Semesters
{
    public const string First = "1st Semester";
    public const string Second = "2nd Semester";
    public const string Summer = "Summer";

    public static readonly string[] All = { First, Second, Summer };

    public static bool IsValid(string? value)
    {
        return value is not null && Array.IndexOf(All, value) >= 0;
    }
}

public static class AppClaimTypes
{
    public const string UserId = "sub";
    public const string Username = "unique_name";
    public const string FullName = "fullName";
    public const string Role = "role";
    public const string StudentId = "studentId";
}