using CruzDusty_WebSystemFinalProject.Api.Models;
using CruzDusty_WebSystemFinalProject.Api.Models.DTOs;
using CruzDusty_WebSystemFinalProject.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CruzDusty_WebSystemFinalProject.Api.Controllers;

[ApiController]
[Route("api/grades")]
[Authorize]
[Produces("application/json")]
public class GradesController : ControllerBase
{
    private readonly IGradeService _gradeService;

    public GradesController(IGradeService gradeService)
    {
        _gradeService = gradeService;
    }

    // GET /api/grades?studentId=&subjectId=&schoolYear=&semester=
    [HttpGet]
    [Authorize(Roles = "Faculty")]
    public async Task<ActionResult<IReadOnlyList<GradeResponseDto>>> GetAll(
        [FromQuery] int? studentId,
        [FromQuery] int? subjectId,
        [FromQuery] string? schoolYear,
        [FromQuery] string? semester)
    {
        var grades = await _gradeService.GetAllAsync(studentId, subjectId, schoolYear, semester);
        return Ok(grades);
    }

    // GET /api/grades/5
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Faculty")]
    public async Task<ActionResult<GradeResponseDto>> GetById(int id)
    {
        var grade = await _gradeService.GetByIdAsync(id);
        return Ok(grade);
    }

    // POST /api/grades  (server computes FinalGrade and Remarks)
    [HttpPost]
    [Authorize(Roles = "Faculty")]
    public async Task<ActionResult<GradeResponseDto>> Create([FromBody] GradeCreateDto dto)
    {
        var created = await _gradeService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.GradeRecordId }, created);
    }

    // PUT /api/grades/5  (server recomputes FinalGrade and Remarks)
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Faculty")]
    public async Task<ActionResult<GradeResponseDto>> Update(int id, [FromBody] GradeUpdateDto dto)
    {
        var updated = await _gradeService.UpdateAsync(id, dto);
        return Ok(updated);
    }

    // DELETE /api/grades/5
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Faculty")]
    public async Task<IActionResult> Delete(int id)
    {
        await _gradeService.DeleteAsync(id);
        return NoContent();
    }

    // POST /api/grades/preview  (computes without saving)
    [HttpPost("preview")]
    [Authorize(Roles = "Faculty")]
    public ActionResult<GradePreviewResponseDto> Preview([FromBody] GradePreviewRequestDto dto)
    {
        var preview = _gradeService.Preview(dto);
        return Ok(preview);
    }

    // GET /api/grades/me
    // The StudentId comes ONLY from the signed JWT claim.
    [HttpGet("me")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<MyGradesResponseDto>> GetMine()
    {
        var claimValue = User.FindFirst(AppClaimTypes.StudentId)?.Value;

        if (!int.TryParse(claimValue, out var studentId))
        {
            return Forbid();
        }

        var result = await _gradeService.GetMyGradesAsync(studentId);
        return Ok(result);
    }
}