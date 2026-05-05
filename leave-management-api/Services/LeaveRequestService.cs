using Microsoft.EntityFrameworkCore;

public class LeaveRequestService : ILeaveRequestService
{
    private readonly AppDbContext _db;

    public LeaveRequestService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<LeaveRequestResponseDto> SubmitAsync(
          CreateLeaveRequestDto dto, Guid userId)
    {
        if (dto.StartDate < DateOnly.FromDateTime(DateTime.UtcNow))
            throw new InvalidLeaveDateException("Start date cannot be in the past.");

        if (dto.EndDate < dto.StartDate)
            throw new InvalidLeaveDateException("End date must be after start date.");

        var totalDays = (dto.EndDate.DayNumber - dto.StartDate.DayNumber) + 1;

        // check balance
        var balance = await _db.LeaveBalances.FirstOrDefaultAsync(b =>
            b.UserId == userId &&
            b.LeaveTypeId == dto.LeaveTypeId &&
            b.Year == DateTime.UtcNow.Year)
            ?? throw new NotFoundException("No leave balance found for this leave type.");

        var remaining = balance.TotalDays - balance.UsedDays;
        if (remaining < totalDays)
            throw new InsufficientLeaveBalanceException(totalDays, remaining);

        // check for overlapping requests
        var overlap = await _db.LeaveRequests.AnyAsync(r =>
            r.UserId == userId &&
            r.Status != LeaveStatus.REJECTED &&
            r.Status != LeaveStatus.CANCELLED &&
            r.StartDate <= dto.EndDate &&
            r.EndDate >= dto.StartDate);

        if (overlap)
            throw new LeaveDatesOverlapException();

        //save request
        var request = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LeaveTypeId = dto.LeaveTypeId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            TotalDays = totalDays,
            Reason = dto.Reason.Trim(),
            Status = LeaveStatus.PENDING,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.LeaveRequests.Add(request);
        await _db.SaveChangesAsync();

        return await MapToDtoAsync(request);
    }

    public async Task<LeaveRequestResponseDto> ApproveAsync(
    Guid id, Guid approverId, ApproveLeaveDto dto)
    {
        var request = await _db.LeaveRequests
           .Include(r => r.User)
           .Include(r => r.LeaveType)
           .FirstOrDefaultAsync(r => r.Id == id)
           ?? throw new NotFoundException($"Leave request {id} not found.");

        //can only action a pending request
        if (request.Status != LeaveStatus.PENDING)
            throw new LeaveAlreadyProcessedException();

        //update status and deduct balance in one transaction
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            request.Status = dto.Action == ApprovalAction.APPROVED
               ? LeaveStatus.APPROVED
               : LeaveStatus.REJECTED;
            request.UpdatedAt = DateTime.UtcNow;

            //only deduct balance if approved
            if (dto.Action == ApprovalAction.APPROVED)
            {
                var balance = await _db.LeaveBalances.FirstOrDefaultAsync(b =>
                    b.UserId == request.UserId &&
                    b.LeaveTypeId == request.LeaveTypeId &&
                    b.Year == request.StartDate.Year)
                    ?? throw new NotFoundException("Leave balance record not found.");

                var remaining = balance.TotalDays - balance.UsedDays;
                if (remaining < request.TotalDays)
                    throw new InsufficientLeaveBalanceException(
                        request.TotalDays, remaining);

                balance.UsedDays += request.TotalDays;
                balance.UpdatedAt = DateTime.UtcNow;
            }

            // Record the approval action for audit 
            _db.LeaveApprovals.Add(new LeaveApproval
            {
                LeaveRequestId = request.Id,
                ApproverId = approverId,
                Action = dto.Action,
                Remarks = dto.Remarks?.Trim(),
                ActionDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        return MapToDto(request);
    }

    public async Task CancelAsync(Guid id, Guid callerId, UserRole callerRole)
    {
        var request = await _db.LeaveRequests
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new NotFoundException($"Leave request {id} not found.");

        // Employee can only cancel their own
        if (callerRole == UserRole.EMPLOYEE && request.UserId != callerId)
            throw new ForbiddenException("You can only cancel your own leave requests.");

        if (request.Status == LeaveStatus.CANCELLED)
            throw new ConflictException("Request is already cancelled.");

        var wasApproved = request.Status == LeaveStatus.APPROVED;

        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            request.Status = LeaveStatus.CANCELLED;
            request.UpdatedAt = DateTime.UtcNow;

            // Restore days only if the leave was previously approved
            if (wasApproved)
            {
                var balance = await _db.LeaveBalances.FirstOrDefaultAsync(b =>
                    b.UserId == request.UserId &&
                    b.LeaveTypeId == request.LeaveTypeId &&
                    b.Year == request.StartDate.Year);

                if (balance is not null)
                {
                    balance.UsedDays = Math.Max(0, balance.UsedDays - request.TotalDays);
                    balance.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<LeaveRequestResponseDto>> GetAllAsync() =>
        await _db.LeaveRequests
            .Include(r => r.User)
            .Include(r => r.LeaveType)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => MapToDto(r))
            .ToListAsync();

    public async Task<List<LeaveRequestResponseDto>> GetByUserAsync(Guid userId) =>
        await _db.LeaveRequests
            .Include(r => r.User)
            .Include(r => r.LeaveType)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => MapToDto(r))
            .ToListAsync();

    public async Task<LeaveRequestResponseDto> GetByIdAsync(
        Guid id, Guid callerId, UserRole callerRole)
    {
        var request = await _db.LeaveRequests
            .Include(r => r.User)
            .Include(r => r.LeaveType)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new NotFoundException($"Leave request {id} not found.");

        if (callerRole == UserRole.EMPLOYEE && request.UserId != callerId)
            throw new ForbiddenException("You cannot view other users' leave requests.");

        return MapToDto(request);
    }


    private async Task<LeaveRequestResponseDto> MapToDtoAsync(LeaveRequest r)
    {
        await _db.Entry(r).Reference(x => x.User).LoadAsync();
        await _db.Entry(r).Reference(x => x.LeaveType).LoadAsync();
        return MapToDto(r);
    }

    private static LeaveRequestResponseDto MapToDto(LeaveRequest r) => new()
    {
        Id = r.Id,
        UserFullName = r.User?.FullName ?? string.Empty,
        LeaveTypeName = r.LeaveType?.Name ?? string.Empty,
        StartDate = r.StartDate,
        EndDate = r.EndDate,
        TotalDays = r.TotalDays,
        Reason = r.Reason,
        Status = r.Status,
        CreatedAt = r.CreatedAt
    };
}