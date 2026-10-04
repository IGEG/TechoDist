using Techodist.BuildingBlocks.Core.Pagination;
using Techodist.Identity.Application.Abstractions;
using Techodist.Identity.Application.Dtos;
using MediatR;

namespace Techodist.Identity.Application.Features.Users.Queries.GetAdminUsers;

/// <summary>Постраничный список администраторов (для админ-панели).</summary>
public sealed record GetAdminUsersQuery(int Page = 1, int PageSize = 20, string? Search = null)
    : IRequest<PagedResult<AdminUserDto>>
{
    public int NormalizedPage => Page < 1 ? 1 : Page;

    public int NormalizedPageSize => PageSize switch
    {
        < 1 => 20,
        > 100 => 100,
        _ => PageSize,
    };
}

internal sealed class GetAdminUsersQueryHandler(IIdentityService identity)
    : IRequestHandler<GetAdminUsersQuery, PagedResult<AdminUserDto>>
{
    public Task<PagedResult<AdminUserDto>> Handle(GetAdminUsersQuery request, CancellationToken cancellationToken)
        => identity.ListAsync(
            request.NormalizedPage,
            request.NormalizedPageSize,
            request.Search,
            cancellationToken);
}
