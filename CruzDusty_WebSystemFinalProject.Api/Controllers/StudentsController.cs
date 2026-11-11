using CruzDusty_WebSystemFinalProject.Api.Models.DTOs;
using CruzDusty_WebSystemFinalProject.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CruzDusty_WebSystemFinalProject.Api.Controllers;

[ApiController]
[Route("api/students")]
[Authorize(Roles = "Faculty")]
[Produces("application/json")]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;

    public StudentsController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    // GET /api/students?search=
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StudentResponseDto>>> GetAll([FromQuery] string? search)
    {
        var students = await _studentService.GetAllAsync(search);
        return Ok(students);
    }

    // GET /api/students/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<StudentResponseDto>> GetById(int id)
    {
        var student = await _studentService.GetByIdAsync(id);
        return Ok(student);
    }

    // POST /api/students  (creates the Student row and its login account)
    [HttpPost]
    public async Task<ActionResult<StudentResponseDto>> Create([FromBody] StudentCreateDto dto)
    {
        var created = await _studentService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.StudentId }, created);
    }

    // PUT /api/students/5
    [HttpPut("{id:int}")]
    public async Task<ActionResult<StudentResponseDto>> Update(int id, [FromBody] StudentUpdateDto dto)
    {
        var updated = await _studentService.UpdateAsync(id, dto);
        return Ok(updated);
    }

    // DELETE /api/students/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _studentService.DeleteAsync(id);
        return NoContent();
    }
}