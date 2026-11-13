namespace CruzDusty_WebSystemFinalProject.Api.Models.DTOs;

public class StudentProfileDto
{
    public int StudentId { get; set; }

    public string StudentNumber { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}