using System.ComponentModel.DataAnnotations;

namespace CruzDusty_WebSystemFinalProject.Api.Models.DTOs;

public class SubjectUpdateDto
{
    [Required(ErrorMessage = "Subject code is required.")]
    [RegularExpression(@"^[A-Za-z0-9 -]{2,20}$",
        ErrorMessage = "Subject code must be 2 to 20 characters: letters, numbers, spaces or hyphens.")]
    public string SubjectCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Subject name is required.")]
    [StringLength(150, ErrorMessage = "Subject name must be at most 150 characters.")]
    public string SubjectName { get; set; } = string.Empty;

    [Range(1, 10, ErrorMessage = "Units must be between 1 and 10.")]
    public int Units { get; set; }

    [StringLength(500, ErrorMessage = "Description must be at most 500 characters.")]
    public string? Description { get; set; }
}