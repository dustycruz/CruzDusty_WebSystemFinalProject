using CruzDusty_WebSystemFinalProject.Api.Models.DTOs;
using CruzDusty_WebSystemFinalProject.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CruzDusty_WebSystemFinalProject.Api.Controllers;

[ApiController]
[Route("api/subjects")]
[Authorize(Roles = "Faculty")]
[Produces("application/json")]
public class SubjectsController : ControllerBase
{
    private readonly ISubjectService _subjectService;

    public SubjectsController(ISubjectService subjectService)
    {
        _subjectService = subjectService;
    }

    // GET /api/subjects
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SubjectResponseDto>>> GetAll()
    {
        var subjects = await _subjectService.GetAllAsync();
        return Ok(subjects);
    }

    // GET /api/subjects/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<SubjectResponseDto>> GetById(int id)
    {
        var subject = await _subjectService.GetByIdAsync(id);
        return Ok(subject);
    }

    // POST /api/subjects
    [HttpPost]
    public async Task<ActionResult<SubjectResponseDto>> Create([FromBody] SubjectCreateDto dto)
    {
        var created = await _subjectService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.SubjectId }, created);
    }

    // PUT /api/subjects/5
    [HttpPut("{id:int}")]
    public async Task<ActionResult<SubjectResponseDto>> Update(int id, [FromBody] SubjectUpdateDto dto)
    {
        var updated = await _subjectService.UpdateAsync(id, dto);
        return Ok(updated);
    }

    // DELETE /api/subjects/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _subjectService.DeleteAsync(id);
        return NoContent();
    }
}