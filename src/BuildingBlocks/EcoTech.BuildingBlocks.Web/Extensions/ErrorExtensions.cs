using EcoTech.BuildingBlocks.Core.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EcoTech.BuildingBlocks.Web.Extensions;

/// <summary>Преобразование доменной ошибки (<see cref="Error"/>) в HTTP-ответ ProblemDetails.</summary>
public static class ErrorExtensions
{
    public static ObjectResult ToProblemResult(this Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError,
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = "Ошибка обработки запроса",
            Detail = error.Message,
        };

        problemDetails.Extensions["code"] = error.Code;

        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }
}
