using Techodist.BuildingBlocks.Core.Results;
using Techodist.Identity.Application.Abstractions;
using Techodist.Identity.Application.Dtos;
using MediatR;

namespace Techodist.Identity.Application.Features.Users.Queries.GetAdminProfile;

/// <summary>Профиль администратора по идентификатору (используется и для <c>/api/auth/me</c>).</summary>
public sealed record GetAdminProfileQuery(Guid UserId) : IRequest<Result<AdminUserDto>>;

internal sealed class GetAdminProfileQueryHandler(IIdentityService identity)
    : IRequestHandler<GetAdminProfileQuery, Result<AdminUserDto>>
{
    public async Task<Result<AdminUserDto>> Handle(GetAdminProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await identity.GetByIdAsync(request.UserId, cancellationToken);

        return user is null
            ? Result.Failure<AdminUserDto>(
                Error.NotFound("identity.user.not_found", $"Администратор '{request.UserId}' не найден."))
            : user;
    }
}
