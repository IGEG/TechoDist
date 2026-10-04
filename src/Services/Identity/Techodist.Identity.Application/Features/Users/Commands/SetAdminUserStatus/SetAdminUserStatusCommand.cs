using Techodist.BuildingBlocks.Core.Results;
using Techodist.Identity.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Techodist.Identity.Application.Features.Users.Commands.SetAdminUserStatus;

/// <summary>Включение/отключение учётной записи администратора (мягкая блокировка вместо удаления).</summary>
public sealed record SetAdminUserStatusCommand(Guid UserId, bool IsActive) : IRequest<Result>;

internal sealed class SetAdminUserStatusCommandHandler(
    IIdentityService identity,
    ICurrentUser currentUser,
    ILogger<SetAdminUserStatusCommandHandler> logger)
    : IRequestHandler<SetAdminUserStatusCommand, Result>
{
    public async Task<Result> Handle(SetAdminUserStatusCommand request, CancellationToken cancellationToken)
    {
        if (!request.IsActive && currentUser.UserId == request.UserId)
        {
            return Result.Failure(
                Error.Conflict("identity.user.cannot_disable_self", "Нельзя отключить собственную учётную запись."));
        }

        var result = await identity.SetActiveAsync(request.UserId, request.IsActive, cancellationToken);

        if (result.IsSuccess)
        {
            logger.LogInformation(
                "Учётная запись {UserId} переведена в состояние {Status}",
                request.UserId,
                request.IsActive ? "активна" : "отключена");
        }

        return result;
    }
}
