using Techodist.BuildingBlocks.Core.Results;
using Techodist.Identity.Application.Abstractions;
using Techodist.Identity.Domain.Security;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Techodist.Identity.Application.Features.Users.Commands.UpdateAdminUser;

/// <summary>Изменение профиля администратора: отображаемое имя и набор ролей.</summary>
public sealed record UpdateAdminUserCommand(
    Guid UserId,
    string DisplayName,
    IReadOnlyCollection<string> Roles) : IRequest<Result>;

internal sealed class UpdateAdminUserCommandHandler(
    IIdentityService identity,
    ICurrentUser currentUser,
    ILogger<UpdateAdminUserCommandHandler> logger)
    : IRequestHandler<UpdateAdminUserCommand, Result>
{
    public async Task<Result> Handle(UpdateAdminUserCommand request, CancellationToken cancellationToken)
    {
        var unknownRoles = request.Roles?.Where(role => !AdminRoles.IsKnown(role)).ToArray() ?? [];
        if (unknownRoles.Length != 0)
        {
            return Result.Failure(
                Error.Validation(
                    "identity.user.invalid_role",
                    $"Неизвестные роли: {string.Join(", ", unknownRoles)}. Допустимые роли: {string.Join(", ", AdminRoles.All)}."));
        }

        var roles = AdminRoles.Normalize(request.Roles);
        if (roles.Count == 0)
        {
            return Result.Failure(
                Error.Validation("identity.user.roles_required", "Нужно указать хотя бы одну роль."));
        }

        // Защита от «самоблокировки»: нельзя снять с себя роль Admin.
        if (currentUser.UserId == request.UserId && !roles.Contains(AdminRoles.Admin))
        {
            return Result.Failure(
                Error.Conflict(
                    "identity.user.cannot_remove_own_admin_role",
                    "Нельзя снять роль Admin с собственной учётной записи."));
        }

        var result = await identity.UpdateAsync(
            request.UserId,
            request.DisplayName.Trim(),
            roles,
            cancellationToken);

        if (result.IsSuccess)
        {
            logger.LogInformation(
                "Обновлён администратор {UserId}: роли {Roles}",
                request.UserId,
                string.Join(", ", roles));
        }

        return result;
    }
}
