using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/leave-balances")]
[Authorize]
public class LeaveBalancesController : ControllerBase
{
    private readonly ILeaveBalanceService _balances;

    public LeaveBalancesController(ILeaveBalanceService balances)
    {
        _balances = balances;
    }

    // GET api/leave-balances?year=2025
    // Employee sees their own, admin/manager can pass ?userId=
    [HttpGet]
    public async Task<IActionResult> GetMyBalances([FromQuery] int? year)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var currentYear = year ?? DateTime.UtcNow.Year;
        var result = await _balances.GetBalancesForUserAsync(userId, currentYear);
        return Ok(result);
    }

    // GET api/leave-balances/users/{userId}?year=2025  (admin/manager only)
    [HttpGet("users/{userId:guid}")]
    [Authorize(Roles = "ADMIN,MANAGER")]
    public async Task<IActionResult> GetUserBalances(Guid userId, [FromQuery] int? year)
    {
        var currentYear = year ?? DateTime.UtcNow.Year;
        var result = await _balances.GetBalancesForUserAsync(userId, currentYear);
        return Ok(result);
    }

    // POST api/leave-balances/assign?year=2025  (admin only, manual trigger)
    [HttpPost("assign")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> AssignForYear([FromQuery] int? year)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        await _balances.AssignBalancesForYearAsync(targetYear);
        return Ok(new { message = $"Balances assigned for {targetYear}." });
    }
}