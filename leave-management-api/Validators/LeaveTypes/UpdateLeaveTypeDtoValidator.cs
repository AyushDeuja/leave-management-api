using FluentValidation;

class UpdateLeaveTypeDtoValidator : AbstractValidator<UpdateLeaveTypeDto>
{
    public UpdateLeaveTypeDtoValidator()
    {
        RuleFor(x => x.Name)
           .NotEmpty().WithMessage("Leave type name is required.")
           .MaximumLength(255);

        RuleFor(x => x.DefaultDaysPerYear)
            .GreaterThan(0).WithMessage("Default days must be at least 1.")
            .LessThanOrEqualTo(365).WithMessage("Default days cannot exceed 365.");
    }
}