using Techodist.BuildingBlocks.Web.Extensions;
using Techodist.Catalog.Api.Contracts;
using Techodist.Catalog.Application.Features.Products.Commands.ArchiveProduct;
using Techodist.Catalog.Application.Features.Products.Commands.CreateProduct;
using Techodist.Catalog.Application.Features.Products.Commands.DeleteProduct;
using Techodist.Catalog.Application.Features.Products.Commands.PublishProduct;
using Techodist.Catalog.Application.Features.Products.Commands.UpdateProduct;
using Techodist.Catalog.Application.Features.Products.Queries.GetProductById;
using Techodist.Catalog.Application.Features.Products.Queries.GetProducts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Techodist.Catalog.Api.Controllers;

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

    /// <summary>Создание товара (для админ-панели). Требует access-токен администратора.</summary>
    [HttpPost]
    [Authorize(Roles = AuthenticationExtensions.AdminRoles)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : result.Error.ToProblemResult();
    }

    /// <summary>Изменение товара (админ-панель). Изменение уходит в поиск событием через outbox.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = AuthenticationExtensions.AdminRoles)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateProductCommand(
            id,
            request.Name,
            request.CategoryId,
            request.Price,
            request.ShortDescription,
            request.Description,
            request.SolventType,
            request.VolumeLiters,
            request.Slug);

        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess ? NoContent() : result.Error.ToProblemResult();
    }

    /// <summary>Публикация товара: карточка появляется на витрине и в поисковом индексе.</summary>
    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = AuthenticationExtensions.AdminRoles)]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new PublishProductCommand(id), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.Error.ToProblemResult();
    }

    /// <summary>Снятие товара с продажи (архив): карточка уходит с витрины и из поиска.</summary>
    [HttpPost("{id:guid}/archive")]
    [Authorize(Roles = AuthenticationExtensions.AdminRoles)]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ArchiveProductCommand(id), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.Error.ToProblemResult();
    }

    /// <summary>Удаление черновика. Опубликованный товар удалить нельзя — только снять с продажи.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AuthenticationExtensions.AdminRoles)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteProductCommand(id), cancellationToken);

        return result.IsSuccess ? NoContent() : result.Error.ToProblemResult();
    }
}
