public class LeaveApproval
{
    public Guid Id { get; set; }
    public Guid LeaveRequestId { get; set; }
    public Guid ApproverId { get; set; }
    public ApprovalAction Action { get; set; }
    public string? Remarks { get; set; }
    public DateTime ActionDate { get; set; }
    public DateTime CreatedAt { get; set; }

    public LeaveRequest LeaveRequest { get; set; } = null!;
    public User Approver { get; set; } = null!;
}