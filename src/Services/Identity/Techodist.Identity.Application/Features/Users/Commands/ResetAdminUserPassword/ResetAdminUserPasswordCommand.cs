using Techodist.BuildingBlocks.Core.Results;
using Techodist.Identity.Application.Abstractions;
using Techodist.Identity.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Techodist.Identity.Application.Features.Users.Commands.ResetAdminUserPassword;

/// <summary>Сброс пароля администратора другим администратором (текущий пароль не запрашивается).</summary>
public sealed record ResetAdminUserPasswordCommand(Guid UserId, string NewPassword) : IRequest<Result>;

internal sealed class ResetAdminUserPasswordCommandHandler(
    IIdentityService identity,
    ILogger<ResetAdminUserPasswordCommandHandler> logger)
    : IRequestHandler<ResetAdminUserPasswordCommand, Result>
{
    public async Task<Result> Handle(ResetAdminUserPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await identity.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(
                Error.NotFound("identity.user.not_found", $"Администратор '{request.UserId}' не найден."));
        }

        var violations = PasswordPolicy.Default.Validate(request.NewPassword, user.Email);
        if (violations.Count > 0)
        {
            return Result.Failure(Error.Validation("identity.password.weak", string.Join(" ", violations)));
        }

        var result = await identity.ResetPasswordAsync(request.UserId, request.NewPassword, cancellationToken);

        if (result.IsSuccess)
        {
            logger.LogWarning("Пароль администратора {Email} сброшен другим администратором", user.Email);
        }

        return result;
    }
}
