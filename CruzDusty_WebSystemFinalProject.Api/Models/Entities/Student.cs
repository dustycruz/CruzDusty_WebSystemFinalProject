namespace CruzDusty_WebSystemFinalProject.Api.Models.Entities;

public class Student
{
    public int StudentId { get; set; }

    public string StudentNumber { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string? MiddleName { get; set; }

    public string LastName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string Course { get; set; } = string.Empty;

    public int YearLevel { get; set; }

    public int? UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public User? User { get; set; }

    public ICollection<GradeRecord> GradeRecords { get; set; } = new List<GradeRecord>();

    // Not mapped: "Juan S. Dela Cruz"
    public string FullName =>
        string.IsNullOrWhiteSpace(MiddleName)
            ? $"{FirstName} {LastName}"
            : $"{FirstName} {MiddleName.Trim()[0]}. {LastName}";

    // Not mapped: "Dela Cruz, Juan S."
    public string ListName =>
        string.IsNullOrWhiteSpace(MiddleName)
            ? $"{LastName}, {FirstName}"
            : $"{LastName}, {FirstName} {MiddleName.Trim()[0]}.";
}