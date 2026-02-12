using ApiGenerica.Controllers;
using ApiGenerica.Services;
using ApiGenerica.Contracts;

namespace ApiGenerica.Extensions;

public static class AuthEndpointsExtensions
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/login", Login)
            .WithName("Login")
            .WithDescription("Autentica un usuario con nombre y contraseña")
            .WithSummary("Iniciar sesión")
            .Accepts<LoginRequest>("application/json")
            .Produces<AuthResponse>(200)
            .Produces(401);

        app.MapPost("/auth/register", Register)
            .WithName("Register")
            .WithDescription("Registra un nuevo usuario en el sistema")
            .WithSummary("Registrar nuevo usuario")
            .Accepts<RegisterRequest>("application/json")
            .Produces<AuthResponse>(201)
            .Produces(400);
    }

    private static IResult Login(LoginRequest request, IAuthService authService)
    {
        return new AuthController(authService).Login(request).Result;
    }

    private static IResult Register(RegisterRequest request, IAuthService authService)
    {
        return new AuthController(authService).Register(request).Result;
    }
}
