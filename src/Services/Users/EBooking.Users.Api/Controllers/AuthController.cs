namespace EBooking.Users.Api;

using EBooking.Users.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Контроллер аутентификации пользователей
/// </summary>
[ApiController]
[Route("auth")]
[AllowAnonymous]
public sealed class AuthController(
    IAuthenticationService authenticationService)
    : ControllerBase
{
    private readonly IAuthenticationService _authenticationService = authenticationService;

    /// <summary>
    /// Зарегистрировать нового пользователя
    /// </summary>
    /// <param name="request">Данные для регистрации пользователя</param>
    /// <param name="cancellationToken">
    /// Токен отмены операции
    /// </param>
    /// <returns>Ответ без содержимого при успешной регистрации</returns>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        typeof(ErrorResponse),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ErrorResponse),
        StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        await _authenticationService.RegisterAsync(request, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Аутентифицировать пользователя
    /// </summary>
    /// <param name="request">Учётные данные пользователя</param>
    /// <param name="cancellationToken">
    /// Токен отмены операции
    /// </param>
    /// <returns>Пользователь и JWT-токен</returns>
    [HttpPost("login")]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(ApiResult<AuthenticationResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ErrorResponse),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ErrorResponse),
        StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResult<AuthenticationResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authenticationService.LoginAsync(request, cancellationToken);

        return Ok(
            new ApiResult<AuthenticationResponse>
            {
                Status = true,
                Message = "User authenticated successfully",
                Data = response
            });
    }
}