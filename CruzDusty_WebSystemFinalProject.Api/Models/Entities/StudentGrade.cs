namespace CruzDusty_WebSystemFinalProject.Api.Models.Entities;

// Maps to dbo.StudentGrades from the instructor's SQL script.
public class StudentGrade
{
    public int GradeId { get; set; }

    public int StudentId { get; set; }

    public string SubjectCode { get; set; } = string.Empty;

    public string SubjectName { get; set; } = string.Empty;

    public int FacultyId { get; set; }

    public int Grade { get; set; }

    public string Remarks { get; set; } = string.Empty;

    public string Semester { get; set; } = string.Empty;

    public string SchoolYear { get; set; } = string.Empty;

    public DateTime DatePosted { get; set; }

    public DateTime? DateModified { get; set; }

    public Student Student { get; set; } = null!;

    public Faculty Faculty { get; set; } = null!;
}