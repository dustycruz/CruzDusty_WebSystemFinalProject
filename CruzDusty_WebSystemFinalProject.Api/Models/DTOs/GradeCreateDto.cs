using System.ComponentModel.DataAnnotations;

namespace CruzDusty_WebSystemFinalProject.Api.Models.DTOs;

// There is intentionally NO FinalGrade and NO Remarks property here.
// Both values are computed on the server by IGradeCalculator.
public class GradeCreateDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Student is required.")]
    public int StudentId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Subject is required.")]
    public int SubjectId { get; set; }

    [Required(ErrorMessage = "School year is required.")]
    [RegularExpression(@"^\d{4}-\d{4}$", ErrorMessage = "School year must look like 2026-2027.")]
    public string SchoolYear { get; set; } = string.Empty;

    [Required(ErrorMessage = "Semester is required.")]
    [RegularExpression(@"^(1st Semester|2nd Semester|Summer)$",
        ErrorMessage = "Semester must be '1st Semester', '2nd Semester' or 'Summer'.")]
    public string Semester { get; set; } = string.Empty;

    [Range(0.0, 100.0, ErrorMessage = "Prelim must be between 0 and 100.")]
    public decimal? Prelim { get; set; }

    [Range(0.0, 100.0, ErrorMessage = "Midterm must be between 0 and 100.")]
    public decimal? Midterm { get; set; }

    [Range(0.0, 100.0, ErrorMessage = "Finals must be between 0 and 100.")]
    public decimal? Finals { get; set; }
}