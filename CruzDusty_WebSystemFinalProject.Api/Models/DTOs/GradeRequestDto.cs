using System.ComponentModel.DataAnnotations;

namespace CruzDusty_WebSystemFinalProject.Api.Models.DTOs;

// Used by POST /api/grades and PUT /api/grades/{id}.
// There is intentionally NO Remarks, FacultyId, DatePosted or DateModified property:
// the server sets all of them, so anything the client sends for them is ignored.
public class GradeRequestDto
{
    [Required(ErrorMessage = "Student is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Student is required.")]
    public int? StudentId { get; set; }

    [Required(ErrorMessage = "Subject code is required.")]
    [RegularExpression(@"^\s*[A-Za-z0-9 -]{2,30}\s*$",
        ErrorMessage = "Subject code must be 2 to 30 characters: letters, numbers, spaces or hyphens.")]
    public string SubjectCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Subject name is required.")]
    [StringLength(200, ErrorMessage = "Subject name must be at most 200 characters.")]
    public string SubjectName { get; set; } = string.Empty;

    // int? so that a missing grade is reported as "required" instead of silently becoming 0.
    // A non-integer such as 85.5 cannot be bound to int and is rejected with 400.
    [Required(ErrorMessage = "Grade is required.")]
    [Range(0, 100, ErrorMessage = "Grade must be a whole number from 0 to 100.")]
    public int? Grade { get; set; }

    [Required(ErrorMessage = "Semester is required.")]
    [RegularExpression(@"^(1st Semester|2nd Semester|Summer)$",
        ErrorMessage = "Semester must be '1st Semester', '2nd Semester' or 'Summer'.")]
    public string Semester { get; set; } = string.Empty;

    [Required(ErrorMessage = "School year is required.")]
    [RegularExpression(@"^\d{4}-\d{4}$", ErrorMessage = "School year must look like 2026-2027.")]
    public string SchoolYear { get; set; } = string.Empty;
}