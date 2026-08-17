namespace EBooking.Events.Application;

/// <summary>
/// Страница результатов запроса.
/// </summary>
public sealed record PaginatedResult<T>(
    int TotalCount,
    int Page,
    int PageSize,
    IReadOnlyCollection<T> Items
);