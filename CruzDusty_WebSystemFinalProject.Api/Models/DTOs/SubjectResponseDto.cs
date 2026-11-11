namespace CruzDusty_WebSystemFinalProject.Api.Models.DTOs;

public class SubjectResponseDto
{
    public int SubjectId { get; set; }

    public string SubjectCode { get; set; } = string.Empty;

    public string SubjectName { get; set; } = string.Empty;

    public int Units { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}