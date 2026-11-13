using CruzDusty_WebSystemFinalProject.Api.Models;
using CruzDusty_WebSystemFinalProject.Api.Models.DTOs;
using CruzDusty_WebSystemFinalProject.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CruzDusty_WebSystemFinalProject.Api.Controllers;

[ApiController]
[Route("api/students")]
[Authorize(Roles = AppRoles.Faculty)]
[Produces("application/json")]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;

    public StudentsController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    // GET /api/students?search=   (Faculty: student list for the grade form)
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StudentResponseDto>>> GetAll([FromQuery] string? search)
    {
        return Ok(await _studentService.GetAllAsync(search));
    }
}