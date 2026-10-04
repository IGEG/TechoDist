namespace Techodist.Search.Application.Index;

/// <summary>Страница выдачи поискового индекса: документы и общее число совпадений.</summary>
public sealed record ProductIndexPage(IReadOnlyList<ProductDocument> Items, int TotalCount);
