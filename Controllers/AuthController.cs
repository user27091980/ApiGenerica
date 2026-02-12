using ApiGenerica.Services;
using ApiGenerica.Contracts;

namespace ApiGenerica.Controllers;

public class AuthController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<IResult> Login(LoginRequest request)
    {
        if (request == null)
            return Results.BadRequest(new { error = "Solicitud inválida." });

        var (success, response, error) = await _authService.LoginAsync(request);

        if (!success)
            return Results.Unauthorized();

        return Results.Ok(response);
    }

    public async Task<IResult> Register(RegisterRequest request)
    {
        if (request == null)
            return Results.BadRequest(new { error = "Solicitud inválida." });

        var (success, response, error) = await _authService.RegisterAsync(request);

        if (!success)
            return Results.BadRequest(new { error });

        return Results.Created($"/json/user/{response!.Id}", response);
    }
}
