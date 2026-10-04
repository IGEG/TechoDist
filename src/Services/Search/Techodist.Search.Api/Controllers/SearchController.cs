using Techodist.Search.Application.Features.SearchProducts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Techodist.Search.Api.Controllers;

[ApiController]
[Route("api/search")]
public sealed class SearchController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Поиск товаров витрины: полнотекстовый запрос, категория, диапазон цен, сортировка,
    /// пагинация. Публичный эндпоинт — покупатели не аутентифицируются (ADR 0004).
    /// </summary>
    [HttpGet("products")]
    public async Task<IActionResult> SearchProducts(
        [FromQuery] SearchProductsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(query, cancellationToken);

        return Ok(result);
    }
}
