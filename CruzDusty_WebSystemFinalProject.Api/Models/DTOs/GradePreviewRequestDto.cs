using System.ComponentModel.DataAnnotations;

namespace CruzDusty_WebSystemFinalProject.Api.Models.DTOs;

public class GradePreviewRequestDto
{
    [Range(0.0, 100.0, ErrorMessage = "Prelim must be between 0 and 100.")]
    public decimal? Prelim { get; set; }

    [Range(0.0, 100.0, ErrorMessage = "Midterm must be between 0 and 100.")]
    public decimal? Midterm { get; set; }

    [Range(0.0, 100.0, ErrorMessage = "Finals must be between 0 and 100.")]
    public decimal? Finals { get; set; }
}