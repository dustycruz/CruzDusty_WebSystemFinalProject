namespace CruzDusty_WebSystemFinalProject.Api.Models.Entities;

public class GradeRecord
{
    public int GradeRecordId { get; set; }

    public int StudentId { get; set; }

    public int SubjectId { get; set; }

    public string SchoolYear { get; set; } = string.Empty;

    public string Semester { get; set; } = string.Empty;

    public decimal? Prelim { get; set; }

    public decimal? Midterm { get; set; }

    public decimal? Finals { get; set; }

    public decimal? FinalGrade { get; set; }

    public string Remarks { get; set; } = GradeRemarks.Incomplete;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public Student Student { get; set; } = null!;

    public Subject Subject { get; set; } = null!;
}