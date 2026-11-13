using System.ComponentModel.DataAnnotations;

namespace CruzDusty_WebSystemFinalProject.Api.Models.DTOs;

public class GradePreviewRequestDto
{
    [Required(ErrorMessage = "Grade is required.")]
    [Range(0, 100, ErrorMessage = "Grade must be a whole number from 0 to 100.")]
    public int? Grade { get; set; }
}