using CruzDusty_WebSystemFinalProject.Api.Extensions;
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

    // GET /api/grades?studentId=&subjectCode=&semester=&schoolYear=   (Faculty)
    [HttpGet]
    [Authorize(Roles = AppRoles.Faculty)]
    public async Task<ActionResult<IReadOnlyList<GradeResponseDto>>> GetAll(
        [FromQuery] int? studentId,
        [FromQuery] string? subjectCode,
        [FromQuery] string? semester,
        [FromQuery] string? schoolYear)
    {
        return Ok(await _gradeService.GetAllAsync(studentId, subjectCode, semester, schoolYear));
    }

    // GET /api/grades/5   (Faculty: any record. Student: own record only, otherwise 404.)
    [HttpGet("{id:int}")]
    [Authorize(Roles = AppRoles.FacultyOrStudent)]
    public async Task<ActionResult<GradeResponseDto>> GetById(int id)
    {
        if (User.IsInRole(AppRoles.Faculty))
        {
            return Ok(await _gradeService.GetByIdAsync(id, ownerStudentId: null));
        }

        var studentId = User.GetIntClaim(AppClaimTypes.StudentId);
        if (studentId is null)
        {
            return Forbid();
        }

        return Ok(await _gradeService.GetByIdAsync(id, ownerStudentId: studentId));
    }

    // POST /api/grades   (Faculty. FacultyId comes from the token; Remarks from the server.)
    [HttpPost]
    [Authorize(Roles = AppRoles.Faculty)]
    public async Task<ActionResult<GradeResponseDto>> Create([FromBody] GradeRequestDto dto)
    {
        var facultyId = User.GetIntClaim(AppClaimTypes.FacultyId);
        if (facultyId is null)
        {
            return Forbid();
        }

        var created = await _gradeService.CreateAsync(dto, facultyId.Value);
        return CreatedAtAction(nameof(GetById), new { id = created.GradeId }, created);
    }

    // PUT /api/grades/5   (Faculty. Remarks are recomputed from the new grade.)
    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Faculty)]
    public async Task<ActionResult<GradeResponseDto>> Update(int id, [FromBody] GradeRequestDto dto)
    {
        return Ok(await _gradeService.UpdateAsync(id, dto));
    }

    // DELETE /api/grades/5   (Faculty)
    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Faculty)]
    public async Task<IActionResult> Delete(int id)
    {
        await _gradeService.DeleteAsync(id);
        return NoContent();
    }

    // POST /api/grades/preview   (Faculty. Computes the remark without saving.)
    [HttpPost("preview")]
    [Authorize(Roles = AppRoles.Faculty)]
    public ActionResult<GradePreviewResponseDto> Preview([FromBody] GradePreviewRequestDto dto)
    {
        return Ok(_gradeService.Preview(dto));
    }
}