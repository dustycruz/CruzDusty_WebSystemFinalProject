using CruzDusty_WebSystemFinalProject.Api.Extensions;
using CruzDusty_WebSystemFinalProject.Api.Models;
using CruzDusty_WebSystemFinalProject.Api.Models.DTOs;
using CruzDusty_WebSystemFinalProject.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CruzDusty_WebSystemFinalProject.Api.Controllers;

[ApiController]
[Route("api/student")]
[Authorize(Roles = AppRoles.Student)]
[Produces("application/json")]
public class StudentController : ControllerBase
{
    private readonly IGradeService _gradeService;

    public StudentController(IGradeService gradeService)
    {
        _gradeService = gradeService;
    }

    // GET /api/student/grades
    // The StudentId comes ONLY from the signed token claim. This action has no parameters,
    // so nothing in the URL, query string or body can select another student's grades.
    [HttpGet("grades")]
    public async Task<ActionResult<MyGradesResponseDto>> GetGrades()
    {
        var studentId = User.GetIntClaim(AppClaimTypes.StudentId);
        if (studentId is null)
        {
            return Forbid();
        }

        return Ok(await _gradeService.GetStudentGradesAsync(studentId.Value));
    }
}