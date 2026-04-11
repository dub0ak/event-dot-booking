namespace EBooking.Handlers;
using System.Net;

/// <summary>
/// Базовый класс с основными возвращаемыми параметрами
/// </summary>
public class ApiBaseResult
{
    /// <summary>
    /// Флаг, указывающий на успешность выполненного запроса
    /// </summary>
    public required bool Status { get; set; }

    /// <summary>
    /// Кастомное сообщение с дополнительной информацией
    /// Здесь может быть информация об ошибке в случае неуспеха
    /// </summary>
    public required string Message { get; set; }
}

/// <summary>
/// Основная структура ответа EBooking-API
/// </summary>
public class ApiResult : ApiBaseResult { }

/// <summary>
/// Шаблонная структура ответа EBooking-API
/// </summary>
public class ApiResult<T> : ApiBaseResult
{
    /// <summary>
    /// Возвращаемые данные
    /// </summary>
    public required T Data { get; set; }
}

public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
}