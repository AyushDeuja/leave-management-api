public interface ILeaveRequestService
{
    Task<LeaveRequestResponseDto> SubmitAsync(CreateLeaveRequestDto dto, Guid userId);
    Task<List<LeaveRequestResponseDto>> GetAllAsync();
    Task<List<LeaveRequestResponseDto>> GetByUserAsync(Guid userId);
    Task<LeaveRequestResponseDto> GetByIdAsync(Guid id, Guid callerId, UserRole callerRole);
    Task<LeaveRequestResponseDto> ApproveAsync(Guid id, Guid approverId, ApproveLeaveDto dto);
    Task CancelAsync(Guid id, Guid callerId, UserRole callerRole);
}