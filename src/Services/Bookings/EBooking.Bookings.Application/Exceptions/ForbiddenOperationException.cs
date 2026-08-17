namespace EBooking.Bookings.Application;

/// <summary>
/// Возникает при попытке выполнить операцию без необходимых прав.
/// </summary>
public sealed class ForbiddenOperationException : Exception
{
    public ForbiddenOperationException() : base("You do not have permission to perform this operation.") {}

    public ForbiddenOperationException(string message) : base(message) {}
}