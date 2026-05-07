using FluentValidation;

public class ApproveLeaveRequestDtoValidator : AbstractValidator<ApproveLeaveDto>
{
    public ApproveLeaveRequestDtoValidator()
    {
        RuleFor(x => x.Action)
             .IsInEnum().WithMessage("Action must be APPROVED or REJECTED.");

        RuleFor(x => x.Remarks)
            .NotEmpty().WithMessage("Remarks are required when rejecting a request.")
            .When(x => x.Action == ApprovalAction.REJECTED);

        RuleFor(x => x.Remarks)
            .MaximumLength(500).WithMessage("Remarks cannot exceed 500 characters.")
            .When(x => x.Remarks is not null);
    }
}