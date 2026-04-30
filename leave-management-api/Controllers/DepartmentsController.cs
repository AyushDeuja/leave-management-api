using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departments;

    public DepartmentsController(IDepartmentService departments)
    {
        _departments = departments;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {

            var depts = await _departments.GetAllAsync();
            return Ok(depts);
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var dept = await _departments.GetByIdAsync(id);
            return Ok(dept);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Create(CreateDepartmentDto dto)
    {
        try
        {
            var dept = await _departments.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = dept.Id }, dept);
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Update(Guid id, UpdateDepartmentDto dto)
    {
        try
        {
            var dept = await _departments.UpdateAsync(id, dto);
            return Ok(dept);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var success = await _departments.DeleteAsync(id);
            if (!success)
                return NotFound(new { message = $"Department {id} not found." });
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}