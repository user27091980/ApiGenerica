using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using ApiGenerica.Configuration;
using ApiGenerica.Contracts;

namespace ApiGenerica.Services;

public interface IJsonFileService
{
    Task<(bool Success, string Content, string Error)> GetJsonAsync(string carpeta, string id);
    Task<(bool Success, string Content, string Error)> GetAllAsync(string carpeta);
    Task<(bool Success, string Id, string Error)> CreateJsonAsync(string carpeta, string id, string content);
    Task<(bool Success, string Id, string Error)> CreateJsonAsync(string carpeta, string content);
    Task<(bool Success, string Error)> UpdateJsonAsync(string carpeta, string id, string content);
    Task<(bool Success, string Content, string Error)> SearchAsync(string carpeta, string field, string value);
    Task<(bool Success, string Content, string Error)> ComplexSearchAsync(string carpeta, List<SearchFilter> filters);
    Task<(bool Success, string Error)> DeleteAsync(string carpeta, string id);
    Task<(bool Success, byte[] Bytes, string ContentType, string Error)> GetImageAsync(string id);
    Task<(bool Success, string Id, string Error)> UploadImageAsync(string? id, IFormFile file);
}

public class JsonFileService : IJsonFileService
{
    private readonly string _baseDir;
    private readonly string _imagesDir;
    private readonly FileStorageOptions _options;
    private static readonly string[] _imageExtensions = [".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".svg"];

    public JsonFileService(IOptions<FileStorageOptions> options)
    {
        _options = options.Value ?? new FileStorageOptions();

        var baseDirName = string.IsNullOrWhiteSpace(_options.BaseDirectory)
            ? "data"
            : _options.BaseDirectory;

        _baseDir = Path.GetFullPath(Path.IsPathRooted(baseDirName)
            ? baseDirName
            : Path.Combine(Directory.GetCurrentDirectory(), baseDirName));

        Directory.CreateDirectory(_baseDir);

        var imagesFolderName = string.IsNullOrWhiteSpace(_options.ImagesFolder)
            ? "resources"
            : _options.ImagesFolder;

        _imagesDir = Path.GetFullPath(Path.Combine(_baseDir, imagesFolderName));
    }

    public async Task<(bool Success, string Content, string Error)> GetJsonAsync(string carpeta, string id)
    {
        var (valid, error) = ValidateCarpetaAndId(carpeta, id);
        if (!valid)
            return (false, "", error);

        var (pathOk, requestedPath, pathError) = BuildPath(carpeta, id);
        if (!pathOk)
            return (false, "", pathError);

        if (!File.Exists(requestedPath))
            return (false, "", $"Archivo no encontrado: {carpeta}/{id}.json");

        var content = await File.ReadAllTextAsync(requestedPath);
        return (true, content, "");
    }

    public async Task<(bool Success, string Content, string Error)> GetAllAsync(string carpeta)
    {
        if (string.IsNullOrWhiteSpace(carpeta))
            return (false, "", "Se requiere el parámetro 'carpeta'.");

        var dir = Path.GetFullPath(Path.Combine(_baseDir, carpeta));
        if (!IsPathValid(dir + Path.DirectorySeparatorChar))
            return (false, "", "Ruta inválida.");

        if (!Directory.Exists(dir))
            return (true, "[]", "");

        var results = new List<JsonElement>();

        foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
        {
            try
            {
                var text = await File.ReadAllTextAsync(file);
                using var doc = JsonDocument.Parse(text);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    results.Add(doc.RootElement.Clone());
                }
            }
            catch
            {
                // Ignorar archivos dañados
            }
        }

        var json = JsonSerializer.Serialize(results);
        return (true, json, "");
    }

    public async Task<(bool Success, string Id, string Error)> CreateJsonAsync(string carpeta, string id, string content)
    {
        if (string.IsNullOrWhiteSpace(carpeta))
            return (false, "", "Se requiere el parámetro 'carpeta'.");

        var (contentOk, contentError) = ValidateContent(content);
        if (!contentOk)
            return (false, "", contentError);

        var (pathOk, requestedPath, pathError) = BuildPath(carpeta, id);
        if (!pathOk)
            return (false, "", pathError);

        var carpetaPath = Path.GetDirectoryName(requestedPath);
        if (carpetaPath != null)
            Directory.CreateDirectory(carpetaPath);

        await File.WriteAllTextAsync(requestedPath, content);
        return (true, id, "");
    }

    public async Task<(bool Success, string Id, string Error)> CreateJsonAsync(string carpeta, string content)
    {
        if (string.IsNullOrWhiteSpace(carpeta))
            return (false, "", "Se requiere el parámetro 'carpeta'.");

        var id = Guid.NewGuid().ToString();
        return await CreateJsonAsync(carpeta, id, content);
    }

    public async Task<(bool Success, string Error)> UpdateJsonAsync(string carpeta, string id, string content)
    {
        var (valid, error) = ValidateCarpetaAndId(carpeta, id);
        if (!valid)
            return (false, error);

        var (pathOk, requestedPath, pathError) = BuildPath(carpeta, id);
        if (!pathOk)
            return (false, pathError);

        if (!File.Exists(requestedPath))
            return (false, $"Archivo no encontrado: {carpeta}/{id}.json");

        var (contentOk, contentError) = ValidateContent(content);
        if (!contentOk)
            return (false, contentError);

        await File.WriteAllTextAsync(requestedPath, content);
        return (true, "");
    }

    public async Task<(bool Success, string Content, string Error)> SearchAsync(string carpeta, string field, string value)
    {
        if (string.IsNullOrWhiteSpace(carpeta) || string.IsNullOrWhiteSpace(field) || string.IsNullOrWhiteSpace(value))
            return (false, "", "Se requieren 'carpeta', 'field' y 'value'.");

        var dir = Path.GetFullPath(Path.Combine(_baseDir, carpeta));
        if (!IsPathValid(dir + Path.DirectorySeparatorChar))
            return (false, "", "Ruta inválida.");

        if (!Directory.Exists(dir))
            return (true, "[]", "");

        var results = new List<JsonElement>();
        var comparer = StringComparer.OrdinalIgnoreCase;

        foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
        {
            try
            {
                var text = await File.ReadAllTextAsync(file);
                using var doc = JsonDocument.Parse(text);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    continue;

                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (!comparer.Equals(prop.Name, field))
                        continue;

                    if (prop.Value.ToString() == value)
                    {
                        results.Add(doc.RootElement.Clone());
                        break;
                    }
                }
            }
            catch
            {
                // Ignorar archivos dañados
            }
        }

        var json = JsonSerializer.Serialize(results);
        return (true, json, "");
    }

    public async Task<(bool Success, string Content, string Error)> ComplexSearchAsync(string carpeta, List<SearchFilter> filters)
    {
        if (string.IsNullOrWhiteSpace(carpeta))
            return (false, "", "Se requiere el parámetro 'carpeta'.");

        if (filters is null || filters.Count == 0)
            return (false, "", "Se requiere al menos un filtro de búsqueda.");

        var dir = Path.GetFullPath(Path.Combine(_baseDir, carpeta));
        if (!IsPathValid(dir + Path.DirectorySeparatorChar))
            return (false, "", "Ruta inválida.");

        if (!Directory.Exists(dir))
            return (true, "[]", "");

        var results = new List<JsonElement>();
        var comparer = StringComparer.OrdinalIgnoreCase;

        foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
        {
            try
            {
                var text = await File.ReadAllTextAsync(file);
                using var doc = JsonDocument.Parse(text);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    continue;

                // Evaluar todos los filtros - todos deben cumplirse (AND)
                bool matchesAll = true;
                foreach (var filter in filters)
                {
                    if (!EvaluateFilter(doc.RootElement, filter, comparer))
                    {
                        matchesAll = false;
                        break;
                    }
                }

                if (matchesAll)
                    results.Add(doc.RootElement.Clone());
            }
            catch
            {
                // Ignorar archivos dañados
            }
        }

        var json = JsonSerializer.Serialize(results);
        return (true, json, "");
    }

    private bool EvaluateFilter(JsonElement element, SearchFilter filter, StringComparer comparer)
    {
        // Buscar el campo
        if (!element.TryGetProperty(filter.Field, out var property))
            return false;

        var propValue = property.ToString();

        return filter.Operator switch
        {
            SearchOperator.Equals => comparer.Equals(propValue, filter.Value),
            SearchOperator.NotEquals => !comparer.Equals(propValue, filter.Value),
            SearchOperator.Contains => propValue.Contains(filter.Value, StringComparison.OrdinalIgnoreCase),
            SearchOperator.GreaterThan => CompareNumeric(propValue, filter.Value) > 0,
            SearchOperator.LessThan => CompareNumeric(propValue, filter.Value) < 0,
            _ => false
        };
    }

    private int CompareNumeric(string value1, string value2)
    {
        if (double.TryParse(value1, out var num1) && double.TryParse(value2, out var num2))
            return num1.CompareTo(num2);
        
        return string.Compare(value1, value2, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<(bool Success, string Error)> DeleteAsync(string carpeta, string id)
    {
        var (valid, error) = ValidateCarpetaAndId(carpeta, id);
        if (!valid)
            return (false, error);

        var (pathOk, requestedPath, pathError) = BuildPath(carpeta, id);
        if (!pathOk)
            return (false, pathError);

        if (!File.Exists(requestedPath))
            return (false, $"Archivo no encontrado: {carpeta}/{id}.json");

        File.Delete(requestedPath);
        return (true, "");
    }

    public async Task<(bool Success, byte[] Bytes, string ContentType, string Error)> GetImageAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return (false, Array.Empty<byte>(), "", "Se requiere el parámetro 'id'.");

        var dir = _imagesDir;
        if (!IsPathValid(dir + Path.DirectorySeparatorChar))
            return (false, Array.Empty<byte>(), "", "Ruta inválida.");

        if (!Directory.Exists(dir))
            return (false, Array.Empty<byte>(), "", $"Archivo no encontrado: resources/{id}");

        string? imagePath = null;
        foreach (var ext in _imageExtensions)
        {
            var candidate = Path.GetFullPath(Path.Combine(dir, $"{id}{ext}"));
            if (!IsPathValid(candidate))
                continue;

            if (File.Exists(candidate))
            {
                imagePath = candidate;
                break;
            }
        }

        if (imagePath is null)
            return (false, Array.Empty<byte>(), "", $"Archivo no encontrado: resources/{id}");

        var bytes = await File.ReadAllBytesAsync(imagePath);
        var contentType = GetContentTypeFromExtension(Path.GetExtension(imagePath));
        return (true, bytes, contentType, "");
    }

    private bool IsPathValid(string requestedPath)
    {
        var baseDirFull = Path.GetFullPath(_baseDir) + Path.DirectorySeparatorChar;
        return requestedPath.StartsWith(baseDirFull, StringComparison.OrdinalIgnoreCase);
    }

    private (bool Success, string Error) ValidateContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return (false, "El cuerpo de la solicitud no puede estar vacío.");

        if (!IsValidJson(content))
            return (false, "El contenido enviado no es un JSON válido.");

        return (true, "");
    }

    private (bool Success, string Error) ValidateCarpetaAndId(string carpeta, string id)
    {
        if (string.IsNullOrWhiteSpace(carpeta) || string.IsNullOrWhiteSpace(id))
            return (false, "Se requieren los parámetros 'carpeta' e 'id'.");

        return (true, "");
    }

    private (bool Success, string RequestedPath, string Error) BuildPath(string carpeta, string id)
    {
        var requestedPath = Path.GetFullPath(Path.Combine(_baseDir, carpeta, $"{id}.json"));

        if (!IsPathValid(requestedPath))
            return (false, "", "Ruta inválida.");

        return (true, requestedPath, "");
    }

    private bool IsValidJson(string content)
    {
        try
        {
            JsonDocument.Parse(content);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<(bool Success, string Id, string Error)> UploadImageAsync(string? id, IFormFile file)
    {
        if (file is null || file.Length == 0)
            return (false, "", "El archivo está vacío o no se envió.");

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext))
            return (false, "", "No se pudo determinar la extensión de la imagen.");

        if (!_imageExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
            return (false, "", $"Extensión no permitida: {ext}");

        var normalizedExt = ext.ToLowerInvariant();
        var imageId = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString() : id;
        var dir = _imagesDir;

        if (!IsPathValid(dir + Path.DirectorySeparatorChar))
            return (false, "", "Ruta inválida.");

        Directory.CreateDirectory(dir);

        var destination = Path.GetFullPath(Path.Combine(dir, $"{imageId}{normalizedExt}"));
        if (!IsPathValid(destination))
            return (false, "", "Ruta inválida.");

        await using var stream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(stream);

        return (true, imageId, "");
    }

    private string GetContentTypeFromExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };
    }
}
