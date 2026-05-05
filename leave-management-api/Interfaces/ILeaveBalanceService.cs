public interface ILeaveBalanceService
{
    Task AssignBalancesForYearAsync(int year);
    Task AssignBalancesForUserAsync(Guid userId, int year);
    Task<List<LeaveBalanceResponseDto>> GetBalancesForUserAsync(Guid userId, int year);
}