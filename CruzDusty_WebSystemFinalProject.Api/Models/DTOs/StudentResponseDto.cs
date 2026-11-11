namespace CruzDusty_WebSystemFinalProject.Api.Models.DTOs;

public class StudentResponseDto
{
    public int StudentId { get; set; }

    public string StudentNumber { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string? MiddleName { get; set; }

    public string LastName { get; set; } = string.Empty;

    // "Dela Cruz, Juan S."
    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string Course { get; set; } = string.Empty;

    public int YearLevel { get; set; }

    public string? Username { get; set; }

    public bool HasAccount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}