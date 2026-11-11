namespace CruzDusty_WebSystemFinalProject.Api.Models.DTOs;

public class GradeResponseDto
{
    public int GradeRecordId { get; set; }

    public int StudentId { get; set; }

    public string StudentNumber { get; set; } = string.Empty;

    // "Dela Cruz, Juan S."
    public string StudentName { get; set; } = string.Empty;

    public int SubjectId { get; set; }

    public string SubjectCode { get; set; } = string.Empty;

    public string SubjectName { get; set; } = string.Empty;

    public int Units { get; set; }

    public string SchoolYear { get; set; } = string.Empty;

    public string Semester { get; set; } = string.Empty;

    public decimal? Prelim { get; set; }

    public decimal? Midterm { get; set; }

    public decimal? Finals { get; set; }

    public decimal? FinalGrade { get; set; }

    public string Remarks { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}