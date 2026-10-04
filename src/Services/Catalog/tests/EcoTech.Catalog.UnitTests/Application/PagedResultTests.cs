using EcoTech.Catalog.Application.Models;
using Xunit;

namespace EcoTech.Catalog.UnitTests.Application;

public sealed class PagedResultTests
{
    [Fact]
    public void Create_ComputesPagesAndFlags()
    {
        var items = Enumerable.Range(1, 10).ToArray();

        var page = PagedResult<int>.Create(items, totalCount: 25, page: 2, pageSize: 10);

        Assert.Equal(3, page.TotalPages);
        Assert.True(page.HasNext);
        Assert.True(page.HasPrevious);
        Assert.Equal(10, page.Items.Count);
    }

    [Fact]
    public void Create_LastPage_HasNoNext()
    {
        var page = PagedResult<int>.Create([42], totalCount: 21, page: 3, pageSize: 10);

        Assert.Equal(3, page.TotalPages);
        Assert.False(page.HasNext);
        Assert.True(page.HasPrevious);
    }

    [Fact]
    public void Empty_HasZeroItemsAndPages()
    {
        var page = PagedResult<string>.Empty(page: 1, pageSize: 12);

        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalPages);
        Assert.False(page.HasNext);
        Assert.False(page.HasPrevious);
    }
}
