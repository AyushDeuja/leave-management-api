using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/leave-requests")]
[Authorize]
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveRequestService _requests;

    public LeaveRequestsController(ILeaveRequestService requests)
    {
        _requests = requests;
    }

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] CreateLeaveRequestDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        try
        {
            var result = await _requests.SubmitAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidLeaveDateException ex) { return BadRequest(new { error = ex.Message }); }
        catch (InsufficientLeaveBalanceException ex) { return UnprocessableEntity(new { error = ex.Message }); }
        catch (LeaveDatesOverlapException ex) { return Conflict(new { error = ex.Message }); }
        catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        var result = role == UserRole.EMPLOYEE.ToString()
            ? await _requests.GetByUserAsync(userId)
            : await _requests.GetAllAsync();

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var role = Enum.Parse<UserRole>(User.FindFirst(ClaimTypes.Role)?.Value!);
        try
        {
            var result = await _requests.GetByIdAsync(id, userId, role);
            return Ok(result);
        }
        catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (ForbiddenException ex) { return Forbid(); }
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "ADMIN,MANAGER")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveLeaveDto dto)
    {
        var approverId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        try
        {
            var result = await _requests.ApproveAsync(id, approverId, dto);
            return Ok(result);
        }
        catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (LeaveAlreadyProcessedException ex) { return Conflict(new { error = ex.Message }); }
        catch (InsufficientLeaveBalanceException ex) { return UnprocessableEntity(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var role = Enum.Parse<UserRole>(User.FindFirst(ClaimTypes.Role)?.Value!);

        try
        {
            await _requests.CancelAsync(id, userId, role);
            return NoContent();
        }
        catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (ConflictException ex) { return Conflict(new { error = ex.Message }); }
        catch (ForbiddenException ex) { return Forbid(); }
    }
}