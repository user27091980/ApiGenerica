using System.Text.Json;
using Microsoft.Extensions.Options;
using ApiGenerica.Configuration;
using ApiGenerica.Contracts;

namespace ApiGenerica.Services;

public interface IAuthService
{
    Task<(bool Success, AuthResponse? Response, string Error)> LoginAsync(LoginRequest request);
    Task<(bool Success, AuthResponse? Response, string Error)> RegisterAsync(RegisterRequest request);
}

public class AuthService : IAuthService
{
    private readonly string _baseDir;
    private readonly string _userFolder = "user";

    public AuthService(IOptions<FileStorageOptions> options)
    {
        var opts = options.Value ?? new FileStorageOptions();
        var baseDirName = string.IsNullOrWhiteSpace(opts.BaseDirectory) ? "data" : opts.BaseDirectory;
        
        _baseDir = Path.GetFullPath(Path.IsPathRooted(baseDirName)
            ? baseDirName
            : Path.Combine(Directory.GetCurrentDirectory(), baseDirName));
        
        Directory.CreateDirectory(Path.Combine(_baseDir, _userFolder));
    }

    public async Task<(bool Success, AuthResponse? Response, string Error)> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Passwd))
            return (false, null, "Se requieren 'name' y 'passwd'.");

        var userDir = Path.Combine(_baseDir, _userFolder);
        
        if (!Directory.Exists(userDir))
            return (false, null, "Credenciales inválidas.");

        // Buscar usuario por nombre
        foreach (var file in Directory.EnumerateFiles(userDir, "*.json"))
        {
            try
            {
                var text = await File.ReadAllTextAsync(file);
                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                    continue;

                // Verificar nombre de usuario
                if (!root.TryGetProperty("name", out var nameProperty))
                    continue;

                if (!string.Equals(nameProperty.GetString(), request.Name, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Verificar contraseña
                if (!root.TryGetProperty("passwd", out var passwdProperty))
                    return (false, null, "Credenciales inválidas.");

                if (passwdProperty.GetString() != request.Passwd)
                    return (false, null, "Credenciales inválidas.");

                // Login exitoso
                var userId = root.TryGetProperty("id", out var idProperty) 
                    ? idProperty.GetString() ?? "" 
                    : "";

                var email = root.TryGetProperty("email", out var emailProperty)
                    ? emailProperty.GetString()
                    : null;

                var response = new AuthResponse
                {
                    Id = userId,
                    Name = nameProperty.GetString() ?? "",
                    Email = email,
                    Message = "Login exitoso"
                };

                return (true, response, "");
            }
            catch
            {
                // Ignorar archivos dañados y continuar
            }
        }

        return (false, null, "Credenciales inválidas.");
    }

    public async Task<(bool Success, AuthResponse? Response, string Error)> RegisterAsync(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Passwd))
            return (false, null, "Se requieren 'name' y 'passwd'.");

        var userDir = Path.Combine(_baseDir, _userFolder);
        Directory.CreateDirectory(userDir);

        // Verificar si el usuario ya existe
        foreach (var file in Directory.EnumerateFiles(userDir, "*.json"))
        {
            try
            {
                var text = await File.ReadAllTextAsync(file);
                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                    continue;

                if (root.TryGetProperty("name", out var nameProperty))
                {
                    if (string.Equals(nameProperty.GetString(), request.Name, StringComparison.OrdinalIgnoreCase))
                        return (false, null, "El usuario ya existe.");
                }
            }
            catch
            {
                // Ignorar archivos dañados
            }
        }

        // Crear nuevo usuario
        var userId = Guid.NewGuid().ToString();
        var user = new
        {
            id = userId,
            name = request.Name,
            passwd = request.Passwd,
            email = request.Email
        };

        var jsonContent = JsonSerializer.Serialize(user);
        var filePath = Path.Combine(userDir, $"{userId}.json");
        
        await File.WriteAllTextAsync(filePath, jsonContent);

        var response = new AuthResponse
        {
            Id = userId,
            Name = request.Name,
            Email = request.Email,
            Message = "Usuario registrado exitosamente"
        };

        return (true, response, "");
    }
}
