namespace CruzDusty_WebSystemFinalProject.Api.Models.Entities;

// Maps to dbo.Faculty from the instructor's SQL script.
public class Faculty
{
    public int FacultyId { get; set; }

    public string FacultyNumber { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<StudentGrade> Grades { get; set; } = new List<StudentGrade>();

    // Not mapped: "Andrea Garcia"
    public string FullName => $"{FirstName} {LastName}";
}