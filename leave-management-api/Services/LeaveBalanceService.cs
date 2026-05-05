using Microsoft.EntityFrameworkCore;

public class LeaveBalanceService : ILeaveBalanceService
{
    private readonly AppDbContext _db;

    public LeaveBalanceService(AppDbContext db)
    {
        _db = db;
    }

    //called by background job at the beginning of each year to assign leave balances to all users
    public async Task AssignBalancesForYearAsync(int year)
    {
        var users = await _db.Users.Where(u => u.IsActive).ToListAsync();
        var leaveTypes = await _db.LeaveTypes.Where(t => t.IsActive).ToListAsync();

        foreach (var user in users)
            foreach (var type in leaveTypes)
                await UpsertBalanceAsync(user.Id, type.Id, type.DefaultDaysPerYear, year);

        await _db.SaveChangesAsync();
    }

    // Called when a new employee is created mid-year
    public async Task AssignBalancesForUserAsync(Guid userId, int year)
    {
        var leaveTypes = await _db.LeaveTypes.Where(t => t.IsActive).ToListAsync();

        foreach (var type in leaveTypes)
            await UpsertBalanceAsync(userId, type.Id, type.DefaultDaysPerYear, year);

        await _db.SaveChangesAsync();
    }

    public async Task<List<LeaveBalanceResponseDto>> GetBalancesForUserAsync(Guid userId, int year)
    {
        return await _db.LeaveBalances
        .Include(b => b.LeaveType)
        .Where(b => b.UserId == userId && b.Year == year)
        .Select(b => new LeaveBalanceResponseDto
        {
            Id = b.Id,
            LeaveTypeName = b.LeaveType.Name,
            Year = b.Year,
            TotalDays = b.TotalDays,
            UsedDays = b.UsedDays,
            RemainingDays = b.TotalDays - b.UsedDays
        })
        .ToListAsync();
    }

    private async Task UpsertBalanceAsync(
       Guid userId, Guid leaveTypeId, int defaultDays, int year)
    {
        var exists = await _db.LeaveBalances.AnyAsync(b =>
            b.UserId == userId &&
            b.LeaveTypeId == leaveTypeId &&
            b.Year == year);

        if (exists) return;

        _db.LeaveBalances.Add(new LeaveBalance
        {
            UserId = userId,
            LeaveTypeId = leaveTypeId,
            Year = year,
            TotalDays = defaultDays,
            UsedDays = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
    }
}