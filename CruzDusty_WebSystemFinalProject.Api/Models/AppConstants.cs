namespace CruzDusty_WebSystemFinalProject.Api.Models;

public static class AppRoles
{
    public const string Faculty = "Faculty";
    public const string Student = "Student";
    public const string FacultyOrStudent = "Faculty,Student";
}

// The required grading scale. Only GradeCalculator assigns these values.
public static class GradeRemarks
{
    public const string Cms = "CMS";
    public const string Sp = "SP";
    public const string Sap = "SAP";
    public const string Gp = "GP";
    public const string Ep = "EP";
    public const string Failed = "BEP/FAILED";
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
    public const string FacultyId = "facultyId";
}