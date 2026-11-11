namespace CruzDusty_WebSystemFinalProject.Api.Models.DTOs;

public class MyGradesResponseDto
{
    public StudentProfileDto Student { get; set; } = new();

    public List<GradeResponseDto> Grades { get; set; } = new();
}