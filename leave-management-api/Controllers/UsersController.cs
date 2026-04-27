using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;
    public UsersController(IUserService users)
    {
        _users = users;
    }

    [HttpGet]
    [Authorize(Roles = "ADMIN,MANAGER")]
    public async Task<IActionResult> GetAll()
    {
        var users = await _users.GetAllAsync();
        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        // Employees can only view their own profile
        if (!IsAdminOrManager() && GetCurrentUserId() != id)
            return Forbid();
        try
        {
            var user = await _users.GetByIdAsync(id);
            return Ok(user);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN,MANAGER")]
    public async Task<IActionResult> Create(CreateUserDto dto)
    {
        var creatorRole = GetCurrentUserRole();
        try
        {
            var result = await _users.CreateAsync(dto, creatorRole);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (ForbiddenException ex) { return Forbid(); }
        catch (ConflictException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "ADMIN,MANAGER")]
    public async Task<IActionResult> Update(Guid id, UpdateUserDto dto)
    {
        if (!IsAdminOrManager() && GetCurrentUserId() != id)
            return Forbid();

        try
        {
            var result = await _users.UpdateAsync(id, dto);
            return Ok(result);
        }
        catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _users.DeleteAsync(id);
            return NoContent();
        }
        catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }
    private Guid GetCurrentUserId() =>
      Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private UserRole GetCurrentUserRole() =>
        Enum.Parse<UserRole>(User.FindFirstValue(ClaimTypes.Role)!);

    private bool IsAdminOrManager() =>
        User.IsInRole("ADMIN") || User.IsInRole("MANAGER");
}
