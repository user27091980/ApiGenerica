using ApiGenerica.Controllers;
using ApiGenerica.Services;
using ApiGenerica.Contracts;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;

namespace ApiGenerica.Extensions;

public static class JsonFileEndpointsExtensions
{
    public static void MapJsonFileEndpoints(this WebApplication app)
    {
        app.MapGet("/json/{carpeta}", GetAllJsonFiles)
            .WithName("GetAllJsonFiles");

        app.MapGet("/json/{carpeta}/{id}", GetJsonFile)
            .WithName("GetJsonFile");

        app.MapPut("/json/{carpeta}", CreateJsonFile)
            .WithName("CreateJsonFile")
            .Accepts<JsonNode>("application/json");

        app.MapPost("/json/{carpeta}/{id}", UpdateJsonFile)
            .WithName("UpdateJsonFile")
            .Accepts<JsonNode>("application/json");

        app.MapGet("/json/{carpeta}/search", SearchJson)
            .WithName("SearchJson");

        app.MapPost("/json/{carpeta}/complex-search", ComplexSearchJson)
            .WithName("ComplexSearchJson")
            .WithDescription("Realiza una búsqueda compleja con múltiples filtros. Todos los filtros se aplican con lógica AND.\n\n" +
                "**Operadores disponibles:**\n" +
                "- Equals (0): Igualdad exacta (insensible a mayúsculas)\n" +
                "- NotEquals (1): Distinto\n" +
                "- GreaterThan (2): Mayor que (comparación numérica)\n" +
                "- LessThan (3): Menor que (comparación numérica)\n" +
                "- Contains (4): Coincidencia parcial en texto\n\n" +
                "**Ejemplo:** Buscar usuarios con edad > 18 Y nombre contiene 'Juan' Y activo = true")
            .WithSummary("Búsqueda compleja con múltiples criterios")
            .Accepts<ComplexSearchRequest>("application/json");

        app.MapDelete("/json/{carpeta}/{id}", DeleteJsonFile)
            .WithName("DeleteJsonFile");

        app.MapGet("/images/{id}", GetImageFile)
            .WithName("GetImageFile");

        app.MapPost("/images", UploadImageFile)
            .WithName("UploadImageFile")
            .Accepts<IFormFile>("multipart/form-data")
            .DisableAntiforgery();
    }

    private static IResult GetAllJsonFiles(string carpeta, IJsonFileService service) =>
        new JsonFileController(service).GetAllJsonFiles(carpeta).Result;

    private static IResult GetJsonFile(string carpeta, string id, IJsonFileService service) =>
        new JsonFileController(service).GetJsonFile(carpeta, id).Result;

    private static IResult CreateJsonFile(string carpeta, JsonNode? payload, IJsonFileService service)
    {
        if (payload == null)
            return Results.BadRequest(new { error = "El cuerpo de la solicitud no puede estar vacío." });

        // Generar ID y añadirlo al payload
        var id = Guid.NewGuid().ToString();
        var jsonObject = payload.AsObject();
        jsonObject["id"] = id;

        var content = jsonObject.ToJsonString();
        return new JsonFileController(service).CreateJsonFile(carpeta, id, content).Result;
    }

    private static IResult UpdateJsonFile(string carpeta, string id, JsonNode? payload, IJsonFileService service)
    {
        if (payload == null)
            return Results.BadRequest(new { error = "El cuerpo de la solicitud no puede estar vacío." });

        var content = payload.ToJsonString();
        return new JsonFileController(service).UpdateJsonFile(carpeta, id, content).Result;
    }

    private static IResult SearchJson(string carpeta, string field, string value, IJsonFileService service)
    {
        return new JsonFileController(service).SearchJson(carpeta, field, value).Result;
    }

    private static IResult ComplexSearchJson(string carpeta, ComplexSearchRequest request, IJsonFileService service)
    {
        return new JsonFileController(service).ComplexSearchJson(carpeta, request).Result;
    }

    private static IResult DeleteJsonFile(string carpeta, string id, IJsonFileService service)
    {
        return new JsonFileController(service).DeleteJsonFile(carpeta, id).Result;
    }

    private static IResult GetImageFile(string id, IJsonFileService service)
    {
        return new JsonFileController(service).GetImage(id).Result;
    }

    private static IResult UploadImageFile(IFormFile file, string? id, IJsonFileService service)
    {
        return new JsonFileController(service).UploadImage(file, id).Result;
    }
}
