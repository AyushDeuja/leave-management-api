using Quartz;

[DisallowConcurrentExecution]
public class YearlyLeaveBalanceJob : IJob
{
    private readonly ILeaveBalanceService _balanceService;
    private readonly ILogger<YearlyLeaveBalanceJob> _logger;

    public YearlyLeaveBalanceJob(ILeaveBalanceService balanceService, ILogger<YearlyLeaveBalanceJob> logger)
    {
        _balanceService = balanceService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var year = DateTime.UtcNow.Year;
        _logger.LogInformation("Starting YearlyLeaveBalanceJob for year {Year}", year);

        try
        {
            await _balanceService.AssignBalancesForYearAsync(year);
            _logger.LogInformation("Successfully completed YearlyLeaveBalanceJob for year {Year}", year);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while executing YearlyLeaveBalanceJob for year {Year}", year);
            throw;
        }
    }
}