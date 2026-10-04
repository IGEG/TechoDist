using Techodist.Order.Domain.Enums;

namespace Techodist.Order.Application.Models;

/// <summary>Фильтр списка заявок для админки: статус + поиск по номеру, имени или e-mail.</summary>
public sealed record OrderListFilter(
    OrderStatus? Status = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20)
{
    public const int DefaultPageSize = 20;

    public const int MaxPageSize = 100;

    public int NormalizedPage => Page < 1 ? 1 : Page;

    public int NormalizedPageSize => PageSize switch
    {
        < 1 => DefaultPageSize,
        > MaxPageSize => MaxPageSize,
        _ => PageSize,
    };
}
