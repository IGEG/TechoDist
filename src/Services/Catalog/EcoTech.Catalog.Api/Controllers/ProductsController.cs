using EcoTech.BuildingBlocks.Web.Extensions;
using EcoTech.Catalog.Application.Features.Products.Commands.CreateProduct;
using EcoTech.Catalog.Application.Features.Products.Queries.GetProductById;
using EcoTech.Catalog.Application.Features.Products.Queries.GetProducts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EcoTech.Catalog.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(ISender sender) : ControllerBase
{
    /// <summary>Постраничный список товаров с фильтрами (категория, растворитель, поиск).</summary>
    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] GetProductsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Детальная карточка товара по идентификатору.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetProductByIdQuery(id), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.Error.ToProblemResult();
    }

    /// <summary>Создание товара (для админ-панели).</summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : result.Error.ToProblemResult();
    }
}
