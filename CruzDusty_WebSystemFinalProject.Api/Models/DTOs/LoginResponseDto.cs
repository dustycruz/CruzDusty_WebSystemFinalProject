namespace CruzDusty_WebSystemFinalProject.Api.Models.DTOs;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public int? StudentId { get; set; }

    public int? FacultyId { get; set; }

    public DateTime ExpiresAt { get; set; }
}