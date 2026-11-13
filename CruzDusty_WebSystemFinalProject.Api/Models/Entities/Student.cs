namespace CruzDusty_WebSystemFinalProject.Api.Models.Entities;

// Maps to dbo.Students from the instructor's SQL script.
public class Student
{
    public int StudentId { get; set; }

    public string StudentNumber { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<StudentGrade> Grades { get; set; } = new List<StudentGrade>();

    // Not mapped: "Juan Dela Cruz"
    public string FullName => $"{FirstName} {LastName}";
}