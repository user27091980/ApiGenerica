using System.Text.Json.Nodes;
using System.ComponentModel;

namespace ApiGenerica.Contracts;

// DTOs no necesarios - usando JsonNode directamente en los endpoints

/// <summary>
/// Solicitud de login de usuario
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Nombre de usuario o email
    /// </summary>
    public string Name { get; set; } = "";
    
    /// <summary>
    /// Contraseña del usuario
    /// </summary>
    public string Passwd { get; set; } = "";
}

/// <summary>
/// Solicitud de registro de nuevo usuario
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// Nombre de usuario
    /// </summary>
    public string Name { get; set; } = "";
    
    /// <summary>
    /// Contraseña del usuario
    /// </summary>
    public string Passwd { get; set; } = "";
    
    /// <summary>
    /// Email del usuario (opcional)
    /// </summary>
    public string? Email { get; set; }
}

/// <summary>
/// Respuesta de autenticación exitosa
/// </summary>
public class AuthResponse
{
    /// <summary>
    /// ID del usuario autenticado
    /// </summary>
    public string Id { get; set; } = "";
    
    /// <summary>
    /// Nombre del usuario
    /// </summary>
    public string Name { get; set; } = "";
    
    /// <summary>
    /// Email del usuario (si existe)
    /// </summary>
    public string? Email { get; set; }
    
    /// <summary>
    /// Mensaje de éxito
    /// </summary>
    public string Message { get; set; } = "";
}

/// <summary>
/// Solicitud de búsqueda compleja con múltiples filtros
/// </summary>
public class ComplexSearchRequest
{
    /// <summary>
    /// Lista de filtros a aplicar. Todos deben cumplirse (lógica AND)
    /// </summary>
    public List<SearchFilter> Filters { get; set; } = new();
}

/// <summary>
/// Filtro individual con campo, operador y valor
/// </summary>
public class SearchFilter
{
    /// <summary>
    /// Nombre del campo JSON a evaluar
    /// </summary>
    public string Field { get; set; } = "";
    
    /// <summary>
    /// Operador de comparación
    /// </summary>
    public SearchOperator Operator { get; set; }
    
    /// <summary>
    /// Valor de comparación (como string)
    /// </summary>
    public string Value { get; set; } = "";
}

/// <summary>
/// Operadores de búsqueda disponibles
/// </summary>
public enum SearchOperator
{
    /// <summary>
    /// Igualdad exacta (insensible a mayúsculas)
    /// </summary>
    [Description("Igualdad exacta")]
    Equals = 0,
    
    /// <summary>
    /// Distinto (insensible a mayúsculas)
    /// </summary>
    [Description("Distinto")]
    NotEquals = 1,
    
    /// <summary>
    /// Mayor que (comparación numérica si ambos valores son números)
    /// </summary>
    [Description("Mayor que")]
    GreaterThan = 2,
    
    /// <summary>
    /// Menor que (comparación numérica si ambos valores son números)
    /// </summary>
    [Description("Menor que")]
    LessThan = 3,
    
    /// <summary>
    /// Contiene (coincidencia parcial, insensible a mayúsculas)
    /// </summary>
    [Description("Contiene")]
    Contains = 4
}

