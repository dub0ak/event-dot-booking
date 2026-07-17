namespace EBooking.Domain.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}

public class NoAvailableSeatsException : Exception
{
    public NoAvailableSeatsException(string message) : base(message)
    {
    }
}

public sealed class ActiveBookingLimitExceededException : Exception
{
    public ActiveBookingLimitExceededException(int limit)
        : base($"The active booking limit of {limit} has been reached.")
    {
        Limit = limit;
    }

    public int Limit { get; }
}

public sealed class EventAlreadyStartedException : Exception
{
    public EventAlreadyStartedException(Guid eventId)
        : base($"Event '{eventId}' has already started.")
    {
    }
}

public sealed class ForbiddenOperationException : Exception
{
    public ForbiddenOperationException()
        : base("You do not have permission to perform this operation.")
    {
    }

    public ForbiddenOperationException(string message)
        : base(message)
    {
    }
    
}

public sealed class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException()
        : base("Invalid login or password")
    {
    }
}