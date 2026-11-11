using System.ComponentModel.DataAnnotations;

namespace CruzDusty_WebSystemFinalProject.Api.Models.DTOs;

public class StudentUpdateDto
{
    [Required(ErrorMessage = "Student number is required.")]
    [StringLength(20, MinimumLength = 3, ErrorMessage = "Student number must be 3 to 20 characters.")]
    public string StudentNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, ErrorMessage = "First name must be at most 50 characters.")]
    public string FirstName { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Middle name must be at most 50 characters.")]
    public string? MiddleName { get; set; }

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, ErrorMessage = "Last name must be at most 50 characters.")]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(150, ErrorMessage = "Email must be at most 150 characters.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Course is required.")]
    [StringLength(100, ErrorMessage = "Course must be at most 100 characters.")]
    public string Course { get; set; } = string.Empty;

    [Range(1, 6, ErrorMessage = "Year level must be between 1 and 6.")]
    public int YearLevel { get; set; }

    // Optional. When supplied, the student's login password is reset to this value.
    [StringLength(100, MinimumLength = 8, ErrorMessage = "New password must be 8 to 100 characters.")]
    public string? NewPassword { get; set; }
}