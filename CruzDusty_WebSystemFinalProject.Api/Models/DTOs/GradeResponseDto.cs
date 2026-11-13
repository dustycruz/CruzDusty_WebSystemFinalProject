namespace CruzDusty_WebSystemFinalProject.Api.Models.DTOs;

public class GradeResponseDto
{
    public int GradeId { get; set; }

    public int StudentId { get; set; }

    public string StudentNumber { get; set; } = string.Empty;

    public string StudentName { get; set; } = string.Empty;

    public string SubjectCode { get; set; } = string.Empty;

    public string SubjectName { get; set; } = string.Empty;

    public int FacultyId { get; set; }

    public string FacultyName { get; set; } = string.Empty;

    public int Grade { get; set; }

    public string Remarks { get; set; } = string.Empty;

    public string Semester { get; set; } = string.Empty;

    public string SchoolYear { get; set; } = string.Empty;

    public DateTime DatePosted { get; set; }

    public DateTime? DateModified { get; set; }
}