using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/leave-types")]
[Authorize]
public class LeaveTypesController : ControllerBase
{
    private readonly AppDbContext _db;

    public LeaveTypesController(AppDbContext db) => _db = db;


    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var types = await _db.LeaveTypes
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new { t.Id, t.Name, t.Description, t.DefaultDaysPerYear })
            .ToListAsync();
        return Ok(types);
    }


    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Create([FromBody] CreateLeaveTypeDto dto)
    {
        var exists = await _db.LeaveTypes.AnyAsync(t =>
            t.Name.ToLower() == dto.Name.ToLower());
        if (exists) return Conflict(new { error = $"Leave type '{dto.Name}' already exists." });

        var type = new LeaveType
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            DefaultDaysPerYear = dto.DefaultDaysPerYear,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.LeaveTypes.Add(type);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { id = type.Id }, type);
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateLeaveTypeDto dto)
    {
        var type = await _db.LeaveTypes.FindAsync(id);
        if (type is null) return NotFound(new { error = $"Leave type {id} not found." });

        if (dto.Name is not null) type.Name = dto.Name.Trim();
        if (dto.Description is not null) type.Description = dto.Description.Trim();
        if (dto.DefaultDaysPerYear.HasValue) type.DefaultDaysPerYear = dto.DefaultDaysPerYear.Value;
        type.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(type);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Delete(long id)
    {
        var type = await _db.LeaveTypes.FindAsync(id);
        if (type is null) return NotFound(new { error = $"Leave type {id} not found." });

        type.IsActive = false;   // soft delete — existing balances reference this
        type.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
