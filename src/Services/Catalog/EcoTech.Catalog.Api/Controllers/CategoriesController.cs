using EcoTech.BuildingBlocks.Web.Extensions;
using EcoTech.Catalog.Application.Features.Categories.Commands.CreateCategory;
using EcoTech.Catalog.Application.Features.Categories.Queries.GetCategories;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EcoTech.Catalog.Api.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(ISender sender) : ControllerBase
{
    /// <summary>Список категорий каталога.</summary>
    [HttpGet]
    public async Task<IActionResult> GetCategories(
        [FromQuery] bool onlyPublished = true,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetCategoriesQuery(onlyPublished), cancellationToken);
        return Ok(result);
    }

    /// <summary>Создание категории (для админ-панели).</summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Created($"/api/categories/{result.Value}", result.Value)
            : result.Error.ToProblemResult();
    }
}
