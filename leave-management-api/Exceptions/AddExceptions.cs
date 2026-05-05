using System;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}

public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message) { }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}

public class InsufficientLeaveBalanceException : Exception
{
    public InsufficientLeaveBalanceException(int requested, int remaining)
        : base($"Requested {requested} days but only {remaining} remaining.") { }
}

public class LeaveDatesOverlapException : Exception
{
    public LeaveDatesOverlapException()
        : base("The requested dates overlap an existing leave request.") { }
}

public class InvalidLeaveDateException : Exception
{
    public InvalidLeaveDateException(string msg) : base(msg) { }
}

public class LeaveAlreadyProcessedException : Exception
{
    public LeaveAlreadyProcessedException()
        : base("This leave request has already been approved or rejected.") { }
}