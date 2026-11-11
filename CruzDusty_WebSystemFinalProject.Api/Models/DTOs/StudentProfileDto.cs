namespace CruzDusty_WebSystemFinalProject.Api.Models.DTOs;

public class StudentProfileDto
{
    public int StudentId { get; set; }

    public string StudentNumber { get; set; } = string.Empty;

    // "Juan S. Dela Cruz"
    public string FullName { get; set; } = string.Empty;

    public string Course { get; set; } = string.Empty;

    public int YearLevel { get; set; }
}