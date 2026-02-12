using ApiGenerica.Services;
using ApiGenerica.Contracts;
using Microsoft.AspNetCore.Http;

namespace ApiGenerica.Controllers;

public class JsonFileController
{
    private readonly IJsonFileService _service;

    public JsonFileController(IJsonFileService service)
    {
        _service = service;
    }

    public async Task<IResult> GetJsonFile(string carpeta, string id)
    {
        var (success, content, error) = await _service.GetJsonAsync(carpeta, id);
        
        if (!success)
        {
            if (error.Contains("no encontrado"))
                return Results.NotFound(new { error });
            return Results.BadRequest(new { error });
        }

        return Results.Content(content, "application/json");
    }

    public async Task<IResult> GetAllJsonFiles(string carpeta)
    {
        var (success, content, error) = await _service.GetAllAsync(carpeta);
        
        if (!success)
            return Results.BadRequest(new { error });

        return Results.Content(content, "application/json");
    }

    public async Task<IResult> CreateJsonFile(string carpeta, string id, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Results.BadRequest(new { error = "El cuerpo de la solicitud no puede estar vacío." });

        var (success, generatedId, error) = await _service.CreateJsonAsync(carpeta, id, content);

        if (!success)
            return Results.BadRequest(new { error });

        return Results.Created($"/json/{carpeta}/{generatedId}", new { id = generatedId, message = $"Archivo creado: {carpeta}/{generatedId}.json" });
    }

    public async Task<IResult> UpdateJsonFile(string carpeta, string id, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Results.BadRequest(new { error = "El cuerpo de la solicitud no puede estar vacío." });

        var (success, error) = await _service.UpdateJsonAsync(carpeta, id, content);

        if (!success)
        {
            if (error.Contains("no encontrado"))
                return Results.NotFound(new { error });
            return Results.BadRequest(new { error });
        }

        return Results.Ok(new { message = $"Archivo actualizado: {carpeta}/{id}.json" });
    }

    public async Task<IResult> DeleteJsonFile(string carpeta, string id)
    {
        var (success, error) = await _service.DeleteAsync(carpeta, id);

        if (!success)
        {
            if (error.Contains("no encontrado"))
                return Results.NotFound(new { error });
            return Results.BadRequest(new { error });
        }

        return Results.Ok(new { message = $"Archivo eliminado: {carpeta}/{id}.json" });
    }

    public async Task<IResult> SearchJson(string carpeta, string field, string value)
    {
        var (success, content, error) = await _service.SearchAsync(carpeta, field, value);

        if (!success)
            return Results.BadRequest(new { error });

        return Results.Content(content, "application/json");
    }

    public async Task<IResult> ComplexSearchJson(string carpeta, ComplexSearchRequest request)
    {
        if (request?.Filters is null || request.Filters.Count == 0)
            return Results.BadRequest(new { error = "Se requiere al menos un filtro." });

        var (success, content, error) = await _service.ComplexSearchAsync(carpeta, request.Filters);

        if (!success)
            return Results.BadRequest(new { error });

        return Results.Content(content, "application/json");
    }

    public async Task<IResult> GetImage(string id)
    {
        var (success, bytes, contentType, error) = await _service.GetImageAsync(id);

        if (!success)
        {
            if (error.Contains("no encontrado"))
                return Results.NotFound(new { error });
            return Results.BadRequest(new { error });
        }

        return Results.File(bytes, contentType, enableRangeProcessing: true);
    }

    public async Task<IResult> UploadImage(IFormFile file, string? id = null)
    {
        var (success, generatedId, error) = await _service.UploadImageAsync(id, file);

        if (!success)
            return Results.BadRequest(new { error });

        return Results.Created($"/images/{generatedId}", new { id = generatedId, message = $"Imagen guardada: resources/{generatedId}" });
    }
}
